using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace BlueBillywig.Sapi.Util;

/// <summary>
/// HOTP token generation as used by SAPI RPC tokens: a full HMAC-SHA1 digest (40-char lowercase
/// hex) over an 8-byte big-endian counter, keyed with the UTF-8 bytes of the shared secret.
/// </summary>
public static class Hotp
{
    /// <summary>
    /// Generates a full HMAC-SHA1 HOTP token (40-char hex) for a given counter.
    /// Unlike standard HOTP which truncates to 6-8 digits, this returns the full digest.
    /// </summary>
    public static string GenerateByCounter(string key, long counter)
    {
        if (key is null) throw new ArgumentNullException(nameof(key));

        var counterBytes = new byte[8];
        BinaryPrimitives.WriteUInt64BigEndian(counterBytes, unchecked((ulong)counter));

        // The secret is HMAC'd over its UTF-8 bytes (not ASCII): a non-ASCII secret must
        // produce the same digest SAPI computes.
        using var hmac = new HMACSHA1(Encoding.UTF8.GetBytes(key));
        return Convert.ToHexString(hmac.ComputeHash(counterBytes)).ToLowerInvariant();
    }

    /// <summary>
    /// Generates an HOTP token based on a timestamp and window size. The counter is derived as
    /// <c>floor(timestamp / window)</c>. Client and server clocks must be within the window for
    /// tokens to match.
    /// </summary>
    /// <param name="key">The shared secret key.</param>
    /// <param name="window">Time window in seconds (e.g. 120 for 2-minute windows).</param>
    /// <param name="timestamp">Unix timestamp in seconds. Defaults to the current time.</param>
    public static string GenerateByTime(string key, int window, long? timestamp = null)
    {
        var ts = timestamp ?? DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        return GenerateByCounter(key, FloorDiv(ts, window));
    }

    /// <summary>
    /// Generates HOTP tokens for a range of time windows around the current counter. Useful for
    /// server-side validation to account for minor clock drift.
    /// </summary>
    /// <returns>A map from shift offset to hex HMAC string, in ascending shift order.</returns>
    public static IReadOnlyDictionary<int, string> GenerateByTimeWindow(string key, int window, int min = -1, int max = 1, long? timestamp = null)
    {
        var ts = timestamp ?? DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var counter = FloorDiv(ts, window);
        var result = new Dictionary<int, string>();
        for (var shift = min; shift <= max; shift++)
        {
            result[shift] = GenerateByCounter(key, counter + shift);
        }
        return result;
    }

    private static long FloorDiv(long value, long divisor)
    {
        if (divisor <= 0) throw new ArgumentOutOfRangeException(nameof(divisor), divisor, "Window must be positive.");
        var q = value / divisor;
        if ((value % divisor != 0) && ((value < 0) != (divisor < 0))) q--;
        return q;
    }
}
