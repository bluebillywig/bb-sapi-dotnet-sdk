using System.Globalization;

namespace BlueBillywig.Sapi.Entities;

/// <summary>Thumbnail and poster URL helpers.</summary>
public sealed class Thumbnail : Entity
{
    /// <summary>Creates the entity.</summary>
    public Thumbnail(Sdk sdk) : base(sdk) { }

    /// <summary>Constructs an absolute thumbnail URL with optional dimensions.</summary>
    /// <param name="relativeImagePath">The relative path to the image (with or without leading slash).</param>
    /// <param name="width">Desired width in pixels (0 for original). Must be non-negative.</param>
    /// <param name="height">Desired height in pixels (0 for original). Must be non-negative.</param>
    /// <exception cref="ArgumentOutOfRangeException">if width or height is negative.</exception>
    public string GetAbsoluteImagePath(string relativeImagePath, int width = 0, int height = 0)
    {
        if (relativeImagePath is null) throw new ArgumentNullException(nameof(relativeImagePath));
        if (width < 0) throw new ArgumentOutOfRangeException(nameof(width), width, "Given width is lower than 0.");
        if (height < 0) throw new ArgumentOutOfRangeException(nameof(height), height, "Given height is lower than 0.");

        relativeImagePath = relativeImagePath.TrimStart('/');
        return $"{Sdk.BaseUri}/image/{width}/{height}/{relativeImagePath}";
    }

    /// <summary>
    /// Absolute URL of a media clip's poster image.
    /// <para>
    /// Use this rather than building a URL from the clip payload. A clip's <c>src</c> is its SOURCE
    /// MEDIA file, so <c>defaultMediaAssetPath + clip.src</c> yields a link to a .mov — the service
    /// says as much, replying "Invalid src mime type: video/quicktime". That mistake shows up as a
    /// grid full of broken images.
    /// </para>
    /// <para>
    /// A <c>null</c> dimension (or one outside 0..99999) lets the service choose (<c>default</c>).
    /// </para>
    /// <para>
    /// A draft (unpublished) clip's poster is not public. Pass an RPC token minted from the
    /// READ-ONLY key — never the write key, because this URL ends up in page source — to see
    /// those. Note the token is time-based (HOTP, 120 s window by default), so the URL is
    /// short-lived.
    /// </para>
    /// </summary>
    public string GetMediaClipPosterPath(string mediaClipId, int? width = null, int? height = null, string? rpcToken = null)
    {
        if (mediaClipId is null) throw new ArgumentNullException(nameof(mediaClipId));

        static string Dimension(int? value) =>
            value is int i && i >= 0 && i < 100000 ? i.ToString(CultureInfo.InvariantCulture) : "default";

        var url = $"{Sdk.BaseUri}/mediaclip/{Uri.EscapeDataString(mediaClipId)}/spthumbnail/{Dimension(width)}/{Dimension(height)}.webp";

        return string.IsNullOrEmpty(rpcToken) ? url : $"{url}?useSession=true&rpctoken={Uri.EscapeDataString(rpcToken)}";
    }

    /// <inheritdoc cref="GetMediaClipPosterPath(string, int?, int?, string?)" />
    public string GetMediaClipPosterPath(long mediaClipId, int? width = null, int? height = null, string? rpcToken = null)
        => GetMediaClipPosterPath(mediaClipId.ToString(CultureInfo.InvariantCulture), width, height, rpcToken);
}
