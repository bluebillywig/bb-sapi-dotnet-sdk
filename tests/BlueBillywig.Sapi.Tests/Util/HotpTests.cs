using BlueBillywig.Sapi.Util;

namespace BlueBillywig.Sapi.Tests.Util;

public class HotpTests
{
    [Fact]
    public void ConsistentForSameInputs() => Assert.Equal(Hotp.GenerateByCounter("test-key", 42), Hotp.GenerateByCounter("test-key", 42));

    [Fact]
    public void DifferentForDifferentCounters() => Assert.NotEqual(Hotp.GenerateByCounter("test-key", 1), Hotp.GenerateByCounter("test-key", 2));

    [Fact]
    public void Generates40CharHex() => Assert.Matches("^[0-9a-f]{40}$", Hotp.GenerateByCounter("my-secret", 100));

    [Fact]
    public void EncodesNonAsciiSecretsAsUtf8()
    {
        // Golden vector shared with the Node SDK (regression guard for its PR #2 ascii->utf8 fix).
        var token = Hotp.GenerateByCounter("sëcrét-café-🔑", 42);

        Assert.Equal("dc631268d43d625047ec064f8579148d3d7da25e", token);
        Assert.NotEqual("c4e7e1a05ab993c2ed5f21bfbe535e634092ef50", token);
    }

    [Fact]
    public void GeneratesByTimeWithExplicitTimestamp()
    {
        // counter = floor(1000 / 120) = 8
        Assert.Equal(Hotp.GenerateByCounter("my-secret", 8), Hotp.GenerateByTime("my-secret", 120, 1000));
    }

    [Fact]
    public void TimeWindowDefaultsToThreeShifts()
    {
        var tokens = Hotp.GenerateByTimeWindow("my-secret", 120);
        Assert.Equal(3, tokens.Count);
        Assert.True(tokens.ContainsKey(-1));
        Assert.True(tokens.ContainsKey(0));
        Assert.True(tokens.ContainsKey(1));
    }

    [Fact]
    public void TimeWindowTokensMatchCounters()
    {
        var tokens = Hotp.GenerateByTimeWindow("my-secret", 120, -1, 1, 1000);
        const int counter = 8;

        Assert.Equal(3, tokens.Count);
        Assert.Equal(Hotp.GenerateByCounter("my-secret", counter - 1), tokens[-1]);
        Assert.Equal(Hotp.GenerateByCounter("my-secret", counter), tokens[0]);
        Assert.Equal(Hotp.GenerateByCounter("my-secret", counter + 1), tokens[1]);
    }
}
