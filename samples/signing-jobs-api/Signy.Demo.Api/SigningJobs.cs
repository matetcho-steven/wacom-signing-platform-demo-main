using System.Collections.Concurrent;

namespace Signy.Demo.Api;

public enum SigningJobStatus
{
    Pending,
    InProgress,
    Completed,
    Failed
}

public sealed record SigningJob(
    Guid Id,
    string TenantId,
    string ExternalReference,
    string Title,
    string CustomerName,
    SigningJobStatus Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? CompletedAtUtc = null);

public sealed record CreateSigningJobRequest(
    string ExternalReference,
    string Title,
    string CustomerName);

public interface ISigningJobStore
{
    (SigningJob Job, bool Created) GetOrCreate(
        string tenantId,
        CreateSigningJobRequest request,
        DateTimeOffset nowUtc);

    SigningJob? GetForTenant(Guid id, string tenantId);

    IReadOnlyList<SigningJob> ListForTenant(string tenantId);

    SigningJob? MarkCompleted(Guid id, string tenantId, DateTimeOffset completedAtUtc);
}

public sealed class InMemorySigningJobStore : ISigningJobStore
{
    private readonly object _gate = new();
    private readonly Dictionary<Guid, SigningJob> _jobs = new();
    private readonly Dictionary<string, Guid> _idByTenantAndExternalReference =
        new(StringComparer.OrdinalIgnoreCase);

    public (SigningJob Job, bool Created) GetOrCreate(
        string tenantId,
        CreateSigningJobRequest request,
        DateTimeOffset nowUtc)
    {
        var normalizedTenant = NormalizeRequired(tenantId, nameof(tenantId));
        var externalReference = NormalizeRequired(request.ExternalReference, nameof(request.ExternalReference));
        var title = NormalizeRequired(request.Title, nameof(request.Title));
        var customerName = NormalizeRequired(request.CustomerName, nameof(request.CustomerName));
        var idempotencyKey = $"{normalizedTenant}\n{externalReference}";

        lock (_gate)
        {
            if (_idByTenantAndExternalReference.TryGetValue(idempotencyKey, out var existingId))
            {
                return (_jobs[existingId], false);
            }

            var job = new SigningJob(
                Guid.NewGuid(),
                normalizedTenant,
                externalReference,
                title,
                customerName,
                SigningJobStatus.Pending,
                nowUtc);

            _jobs.Add(job.Id, job);
            _idByTenantAndExternalReference.Add(idempotencyKey, job.Id);
            return (job, true);
        }
    }

    public SigningJob? GetForTenant(Guid id, string tenantId)
    {
        var normalizedTenant = NormalizeRequired(tenantId, nameof(tenantId));

        lock (_gate)
        {
            return _jobs.TryGetValue(id, out var job) &&
                   string.Equals(job.TenantId, normalizedTenant, StringComparison.OrdinalIgnoreCase)
                ? job
                : null;
        }
    }

    public IReadOnlyList<SigningJob> ListForTenant(string tenantId)
    {
        var normalizedTenant = NormalizeRequired(tenantId, nameof(tenantId));

        lock (_gate)
        {
            return _jobs.Values
                .Where(job => string.Equals(job.TenantId, normalizedTenant, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(job => job.CreatedAtUtc)
                .ToArray();
        }
    }

    public SigningJob? MarkCompleted(Guid id, string tenantId, DateTimeOffset completedAtUtc)
    {
        var normalizedTenant = NormalizeRequired(tenantId, nameof(tenantId));

        lock (_gate)
        {
            if (!_jobs.TryGetValue(id, out var existing) ||
                !string.Equals(existing.TenantId, normalizedTenant, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var completed = existing with
            {
                Status = SigningJobStatus.Completed,
                CompletedAtUtc = completedAtUtc
            };

            _jobs[id] = completed;
            return completed;
        }
    }

    private static string NormalizeRequired(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value is required.", paramName);
        }

        return value.Trim();
    }
}
