using GovUK.Dfe.FlexForms.Domain.Tenancy;
using Xunit;

namespace GovUK.Dfe.FlexForms.Domain.Tests.Tenancy;

public class TenantApiKeyHasherTests
{
    [Fact]
    public void Hash_ProducesDeterministicLowercaseHex()
    {
        var a = TenantApiKeyHasher.Hash("the-key");
        var b = TenantApiKeyHasher.Hash("the-key");

        Assert.Equal(a, b);
        Assert.Equal(64, a.Length); // SHA-256 = 32 bytes = 64 hex chars
        Assert.Equal(a.ToLowerInvariant(), a);
    }

    [Fact]
    public void Hash_ProducesDifferentDigests_ForDifferentInputs()
    {
        Assert.NotEqual(TenantApiKeyHasher.Hash("a"), TenantApiKeyHasher.Hash("b"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Hash_ReturnsEmpty_ForNullOrWhitespace(string? raw)
    {
        Assert.Equal(string.Empty, TenantApiKeyHasher.Hash(raw));
    }
}
