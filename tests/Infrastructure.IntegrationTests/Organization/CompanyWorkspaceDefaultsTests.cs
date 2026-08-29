using qc_authorization.Application.Organization.Queries.GetPersonnelWorkspaces;
using NUnit.Framework;
using Shouldly;

namespace qc_authorization.Infrastructure.IntegrationTests.Organization;

using qc_authorization.Tests.TestSupport;

[TestFixture]
public class CompanyWorkspaceDefaultsTests
{
    [Test]
    public void ResolveDefaultCompanyId_Uses_Primary_Assignment_Company()
    {
        var companies = new List<CompanyWorkspaceDto>
        {
            new(TestGuids.CompanyA, [new WorkspacePositionDto(TestGuids.Assignment101, TestGuids.PosA1, "A", "A", false, true)]),
            new(TestGuids.CompanyB, [new WorkspacePositionDto(TestGuids.Assignment102, TestGuids.PosB1, "B", "B", true, true)]),
        };

        CompanyWorkspaceDefaults.ResolveDefaultCompanyId(companies).ShouldBe(TestGuids.CompanyB);
    }

    [Test]
    public void ResolveDefaultCompanyId_Falls_Back_To_Lowest_CompanyId()
    {
        var companies = new List<CompanyWorkspaceDto>
        {
            new(TestGuids.CompanyB, [new WorkspacePositionDto(TestGuids.Assignment103, TestGuids.PosA2, "C", "C", false, true)]),
            new(TestGuids.CompanyA, [new WorkspacePositionDto(TestGuids.Assignment102, TestGuids.PosA1, "A", "A", false, true)]),
        };

        CompanyWorkspaceDefaults.ResolveDefaultCompanyId(companies).ShouldBe(TestGuids.CompanyA);
    }
}
