namespace DfE.ExternalApplications.Application.Common.Attributes
{
    /// <summary>
    /// Attribute to define rate limits on requests.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class RateLimitAttribute(int max, int seconds) : Attribute
    {
        public int Max { get; } = max;
        public int Seconds { get; } = seconds;
    }
}
