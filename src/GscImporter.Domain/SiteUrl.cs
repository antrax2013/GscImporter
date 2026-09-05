namespace GscImporter.Domain;

public sealed record SiteUrl
{
    public SiteUrl(string value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new ArgumentException("The site URL must be an absolute HTTP or HTTPS URL.", nameof(value));
        }

        Value = $"{uri.Scheme}://{uri.IdnHost}{GetPortSuffix(uri)}";
    }

    public string Value { get; }

    public bool Contains(Uri pageUri) =>
        string.Equals(new SiteUrl(pageUri.GetLeftPart(UriPartial.Authority)).Value, Value, StringComparison.OrdinalIgnoreCase);

    public override string ToString() => Value;

    private static string GetPortSuffix(Uri uri) => uri.IsDefaultPort ? string.Empty : $":{uri.Port}";
}
