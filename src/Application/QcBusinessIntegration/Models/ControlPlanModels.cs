namespace qc_authorization.Application.QcBusinessIntegration.Models;

public enum ControlPlanStatus
{
    Draft = 0,
    UnderReview = 1,
    Approved = 2,
}

public class ControlPlan
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public Guid CompanyId { get; set; }
    public Guid LaboratoryId { get; set; }
    public ControlPlanStatus Status { get; private set; } = ControlPlanStatus.Draft;

    public static ControlPlan Create(Guid id, string code, string title, Guid companyId, Guid laboratoryId, ControlPlanStatus status = ControlPlanStatus.Draft) =>
        new()
        {
            Id = id,
            Code = code,
            Title = title,
            CompanyId = companyId,
            LaboratoryId = laboratoryId,
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
            throw new InvalidOperationException("Business Invariant Violation: Control plan must be in 'UnderReview' state to be approved.");
        }

        Status = ControlPlanStatus.Approved;
    }

    public void UpdateTitle(string newTitle)
    {
        if (Status == ControlPlanStatus.Approved)
        {
            throw new InvalidOperationException("Cannot update an approved control plan.");
        }

        Title = newTitle;
    }
}

public class BOM
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public Guid CompanyId { get; set; }
    public string Revision { get; set; } = "1.0";
    public string Description { get; set; } = string.Empty;

    public static BOM Create(Guid id, string code, Guid companyId, string revision, string description) =>
        new()
        {
            Id = id,
            Code = code,
            CompanyId = companyId,
            Revision = revision,
            Description = description,
        };

    public void Update(string description, string revision)
    {
        Description = description;
        Revision = revision;
    }
}

public class Laboratory
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Guid CompanyId { get; set; }

    public static Laboratory Create(Guid id, string code, string name, Guid companyId) =>
        new()
        {
            Id = id,
            Code = code,
            Name = name,
            CompanyId = companyId,
        };
}

public class Workstation
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Guid LaboratoryId { get; set; }

    public static Workstation Create(Guid id, string code, string name, Guid laboratoryId) =>
        new()
        {
            Id = id,
            Code = code,
            Name = name,
            LaboratoryId = laboratoryId,
        };
}
