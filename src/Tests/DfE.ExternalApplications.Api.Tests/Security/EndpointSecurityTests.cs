using DfE.CoreLibs.Testing.Authorization;
using DfE.CoreLibs.Testing.Authorization.Helpers;
using Xunit;

namespace DfE.ExternalApplications.Api.Tests.Security
{
    public class EndpointSecurityTests
    {
        [Theory]
        [MemberData(nameof(GetEndpointTestData))]
        public void ValidateEndpointSecurity(string controllerName, string actionName, string expectedSecurity)
        {
            var securityTests = new AuthorizationTester();

            var results = securityTests.ValidateEndpoint(typeof(Program).Assembly, controllerName, actionName, expectedSecurity);

            Assert.Null(results.Message);
        }

        public static IEnumerable<object[]> GetEndpointTestData()
        {
            var configFilePath = "Security/ExpectedSecurity.json";
            return EndpointTestDataProvider.GetEndpointTestDataFromFile(typeof(Program).Assembly, configFilePath);
        }
    }
}
