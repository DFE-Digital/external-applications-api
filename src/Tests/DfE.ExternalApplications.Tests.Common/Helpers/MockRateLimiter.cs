using GovUK.Dfe.CoreLibs.Utilities.RateLimiting;

namespace DfE.ExternalApplications.Tests.Common.Helpers;

/// <summary>
/// Mock rate limiter that always allows requests for testing purposes
/// </summary>
public class MockRateLimiter : IRateLimiter<string>
{
    public bool IsAllowed(string key) => true;
}

/// <summary>
/// Mock rate limiter factory that creates MockRateLimiter instances
/// </summary>
public class MockRateLimiterFactory : IRateLimiterFactory<string>
{
    public IRateLimiter<string> Create(int maxRequests, TimeSpan timeWindow) => new MockRateLimiter();
}