using Signy.Demo.Api;
using Xunit;

namespace Signy.Demo.Api.Tests;

public sealed class InMemorySigningJobStoreTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void GetOrCreate_ReusesJob_ForSameTenantAndExternalReference()
    {
        var store = new InMemorySigningJobStore();
        var request = new CreateSigningJobRequest("ERP-1001", "Loan approval", "Ama Mensah");

        var first = store.GetOrCreate("bank-a", request, Now);
        var second = store.GetOrCreate("bank-a", request, Now.AddMinutes(1));

        Assert.True(first.Created);
        Assert.False(second.Created);
        Assert.Equal(first.Job.Id, second.Job.Id);
    }

    [Fact]
    public void GetOrCreate_AllowsSameExternalReference_AcrossDifferentTenants()
    {
        var store = new InMemorySigningJobStore();
        var request = new CreateSigningJobRequest("ERP-1001", "Loan approval", "Ama Mensah");

        var tenantA = store.GetOrCreate("bank-a", request, Now);
        var tenantB = store.GetOrCreate("bank-b", request, Now);

        Assert.NotEqual(tenantA.Job.Id, tenantB.Job.Id);
        Assert.Equal("bank-a", tenantA.Job.TenantId);
        Assert.Equal("bank-b", tenantB.Job.TenantId);
    }

    [Fact]
    public void GetForTenant_DoesNotLeakAnotherTenantsJob()
    {
        var store = new InMemorySigningJobStore();
        var created = store.GetOrCreate(
            "hospital-a",
            new CreateSigningJobRequest("FORM-77", "Consent form", "Kojo Asare"),
            Now);

        var visibleToOwner = store.GetForTenant(created.Job.Id, "hospital-a");
        var visibleToOtherTenant = store.GetForTenant(created.Job.Id, "hospital-b");

        Assert.NotNull(visibleToOwner);
        Assert.Null(visibleToOtherTenant);
    }

    [Fact]
    public void MarkCompleted_RequiresMatchingTenant()
    {
        var store = new InMemorySigningJobStore();
        var created = store.GetOrCreate(
            "bank-a",
            new CreateSigningJobRequest("ERP-42", "Account mandate", "Esi Owusu"),
            Now);

        var rejected = store.MarkCompleted(created.Job.Id, "bank-b", Now.AddMinutes(3));
        var completed = store.MarkCompleted(created.Job.Id, "bank-a", Now.AddMinutes(4));

        Assert.Null(rejected);
        Assert.NotNull(completed);
        Assert.Equal(SigningJobStatus.Completed, completed!.Status);
        Assert.Equal(Now.AddMinutes(4), completed.CompletedAtUtc);
    }
}
