using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using AccessManagement.Infrastructure.Identity;
using NUnit.Framework;
using Shouldly;

namespace AccessManagement.Infrastructure.IntegrationTests.Identity;

using AccessManagement.Tests.TestSupport;

[TestFixture]
public class JwtTokenServiceTests
{
    [Test]
    public void GenerateToken_Includes_NameIdentifier_And_PersonnelId_Claims()
    {
        var service = new JwtTokenService(Options.Create(new JwtOptions
        {
            Key = "qc-authorization-dev-signing-key-min-32-chars",
            Issuer = "qc-authorization",
            Audience = "qc-authorization",
            ExpiryMinutes = 60,
        }));

        var user = new ApplicationUser
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Email = "user@test.local",
            UserName = "user@test.local",
            PersonnelId = TestGuids.Personnel1,
        };

        var token = service.GenerateToken(user, activeCompanyId: TestGuids.CompanyA, nationalId: "0012345678");
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        jwt.Claims.Single(c => c.Type == ClaimTypes.NameIdentifier).Value
            .ShouldBe(user.Id.ToString());
        jwt.Claims.Single(c => c.Type == "personnel_id").Value.ShouldBe(TestGuids.Personnel1.ToString());
        jwt.Claims.Single(c => c.Type == "active_company_id").Value.ShouldBe(TestGuids.CompanyA.ToString());
        jwt.Claims.Single(c => c.Type == "national_id").Value.ShouldBe("0012345678");
    }
}
