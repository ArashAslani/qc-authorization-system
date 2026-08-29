using qc_authorization.Application.Common.Mappings;
using NUnit.Framework;

namespace qc_authorization.Infrastructure.IntegrationTests;

using qc_authorization.Tests.TestSupport;

[SetUpFixture]
public class IntegrationTestSetup
{
    [OneTimeSetUp]
    public void RegisterMappings() => MappingConfig.RegisterMappings();
}
