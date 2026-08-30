namespace Qc.AccessPlugin.ControlPlans;

public enum ControlPlanStatus
{
    Draft = 0,
    UnderReview = 1,
    Approved = 2,
}

/// <summary>
/// Sample QC business entity. <see cref="ScopeUnitId"/> is the OrganizationalUnit
/// the Access Engine uses for subtree matching (typically a Company or Workstation).
/// </summary>
public sealed class ControlPlan
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public Guid ScopeUnitId { get; set; }
    public ControlPlanStatus Status { get; private set; } = ControlPlanStatus.Draft;

    public static ControlPlan Create(
        Guid id,
        string code,
        string title,
        Guid scopeUnitId,
        ControlPlanStatus status = ControlPlanStatus.Draft) =>
        new()
        {
            Id = id,
            Code = code,
            Title = title,
            ScopeUnitId = scopeUnitId,
            Status = status,
        };

    public void SubmitForReview()
    {
        if (Status != ControlPlanStatus.Draft)
        {
            throw new InvalidOperationException("Only draft control plans can be submitted for review.");
        }

        Status = ControlPlanStatus.UnderReview;
    }

    public void Approve()
    {
        if (Status != ControlPlanStatus.UnderReview)
        {
            throw new InvalidOperationException(
                "Business invariant: a control plan must be UnderReview to be approved.");
        }

        Status = ControlPlanStatus.Approved;
    }
}

public interface IControlPlanStore
{
    Task<ControlPlan?> FindByIdAsync(Guid id, CancellationToken ct = default);

    Task SaveAsync(ControlPlan plan, CancellationToken ct = default);
}

public sealed class InMemoryControlPlanStore : IControlPlanStore
{
    private readonly Dictionary<Guid, ControlPlan> _items = new();

    public Task<ControlPlan?> FindByIdAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult(_items.GetValueOrDefault(id));

    public Task SaveAsync(ControlPlan plan, CancellationToken ct = default)
    {
        _items[plan.Id] = plan;
        return Task.CompletedTask;
    }
}
