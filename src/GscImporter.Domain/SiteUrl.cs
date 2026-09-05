namespace GscImporter.Domain;

public sealed record SiteUrl
{
    public SiteUrl(string value)
    {
        string tmp = value;
        if (!tmp.StartsWith("http://") & !tmp.StartsWith("https://"))
        {
            tmp = $"https://{tmp.Replace(":", "").Replace("/", "")}";
        }

        if (!Uri.TryCreate(tmp, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new ArgumentException("The site URL must be an absolute HTTP or HTTPS URL.", nameof(value));
        }

        Domaine = uri.IdnHost.Replace("www.", "");
        Value = $"{uri.Scheme}://{Domaine}{GetPortSuffix(uri)}";
    }

    public string Value { get; }
    public string Domaine { get; }

    public bool Contains(Uri pageUri) =>
        string.Equals(new SiteUrl(pageUri.GetLeftPart(UriPartial.Authority)).Domaine, Domaine, StringComparison.OrdinalIgnoreCase);

    public override string ToString() => Value;

    private static string GetPortSuffix(Uri uri) => uri.IsDefaultPort ? string.Empty : $":{uri.Port}";
}
