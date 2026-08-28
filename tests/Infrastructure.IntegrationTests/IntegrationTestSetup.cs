using qc_authorization.Application.Common.Mappings;
using NUnit.Framework;

namespace qc_authorization.Infrastructure.IntegrationTests;

[SetUpFixture]
public class IntegrationTestSetup
{
    [OneTimeSetUp]
    public void RegisterMappings() => MappingConfig.RegisterMappings();
}
