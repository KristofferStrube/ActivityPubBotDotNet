using System.Text.Json.Serialization;

namespace KristofferStrube.ActivityPubBotDotNet.Server.WebFinger;

public class ResourceLink
{
    [JsonPropertyName("rel")]
    public string Rel { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("href")]
    public Uri Href { get; set; }

    public ResourceLink(string rel, string type, Uri href)
    {
        Rel = rel;
        Type = type;
        Href = href;
    }
}