using Signy.Demo.Api;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<ISigningJobStore, InMemorySigningJobStore>();

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapGet("/api/signing-jobs", (HttpRequest httpRequest, ISigningJobStore store) =>
{
    if (!TryGetTenantId(httpRequest, out var tenantId, out var error))
    {
        return error!;
    }

    return Results.Ok(store.ListForTenant(tenantId!));
});

app.MapGet("/api/signing-jobs/{id:guid}", (Guid id, HttpRequest httpRequest, ISigningJobStore store) =>
{
    if (!TryGetTenantId(httpRequest, out var tenantId, out var error))
    {
        return error!;
    }

    var job = store.GetForTenant(id, tenantId!);
    return job is null ? Results.NotFound() : Results.Ok(job);
});

app.MapPost("/api/signing-jobs", (CreateSigningJobRequest request, HttpRequest httpRequest, ISigningJobStore store) =>
{
    if (!TryGetTenantId(httpRequest, out var tenantId, out var error))
    {
        return error!;
    }

    try
    {
        var result = store.GetOrCreate(tenantId!, request, DateTimeOffset.UtcNow);
        return result.Created
            ? Results.Created($"/api/signing-jobs/{result.Job.Id}", result.Job)
            : Results.Ok(result.Job);
    }
    catch (ArgumentException exception)
    {
        return Results.BadRequest(new { error = exception.Message });
    }
});

app.MapPost("/api/signing-jobs/{id:guid}/complete", (Guid id, HttpRequest httpRequest, ISigningJobStore store) =>
{
    if (!TryGetTenantId(httpRequest, out var tenantId, out var error))
    {
        return error!;
    }

    var completed = store.MarkCompleted(id, tenantId!, DateTimeOffset.UtcNow);
    return completed is null ? Results.NotFound() : Results.Ok(completed);
});

app.Run();

static bool TryGetTenantId(HttpRequest request, out string? tenantId, out IResult? error)
{
    tenantId = request.Headers["X-Tenant-Id"].FirstOrDefault()?.Trim();
    if (string.IsNullOrWhiteSpace(tenantId))
    {
        error = Results.BadRequest(new { error = "X-Tenant-Id header is required." });
        return false;
    }

    error = null;
    return true;
}

public partial class Program
{
}
