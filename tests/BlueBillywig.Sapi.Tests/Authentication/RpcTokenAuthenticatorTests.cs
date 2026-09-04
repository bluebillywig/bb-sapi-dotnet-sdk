using BlueBillywig.Sapi.Authentication;
using BlueBillywig.Sapi.Util;

namespace BlueBillywig.Sapi.Tests.Authentication;

public class RpcTokenAuthenticatorTests
{
    [Fact]
    public void ReturnsRpctokenHeaderInCorrectFormat()
    {
        var authenticator = new RpcTokenAuthenticator(1, "some-secret");

        var headers = authenticator.Authenticate();

        Assert.True(headers.ContainsKey("rpctoken"));
        var parts = headers["rpctoken"].Split('-');
        Assert.Equal("1", parts[0]);
        Assert.NotEmpty(parts[1]);

        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var validTokens = Hotp.GenerateByTimeWindow("some-secret", 120, -1, 1, now);
        Assert.Contains(parts[1], validTokens.Values);
    }

    [Fact]
    public void OnlyReturnsTheRpctokenHeader()
    {
        var headers = new RpcTokenAuthenticator(1, "some-secret").Authenticate();

        Assert.Equal(new[] { "rpctoken" }, headers.Keys);
    }
}
