using AccessManagement.Application.Common.Mappings;
using NUnit.Framework;

namespace AccessManagement.Infrastructure.IntegrationTests;

using AccessManagement.Tests.TestSupport;

[SetUpFixture]
public class IntegrationTestSetup
{
    [OneTimeSetUp]
    public void RegisterMappings() => MappingConfig.RegisterMappings();
}
