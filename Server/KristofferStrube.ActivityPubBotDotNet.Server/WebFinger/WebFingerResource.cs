using System.Text.Json.Serialization;

namespace KristofferStrube.ActivityPubBotDotNet.Server.WebFinger;

public class WebFingerResource
{
    [JsonPropertyName("subject")]
    public string Subject { get; set; }

    [JsonPropertyName("links")]
    public List<ResourceLink> Links { get; set; } = new();

    public WebFingerResource(string subject)
    {
        Subject = subject;
    }
}
