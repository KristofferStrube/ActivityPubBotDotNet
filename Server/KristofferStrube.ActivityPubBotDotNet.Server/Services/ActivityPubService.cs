using KristofferStrube.ActivityStreams;
using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using static System.Text.Json.JsonSerializer;

namespace KristofferStrube.ActivityPubBotDotNet.Server;

public class ActivityPubService
{
    private readonly HttpClient httpClient;
    private readonly IConfiguration configuration;

    public ActivityPubService(HttpClient httpClient, IConfiguration configuration)
    {
        this.httpClient = httpClient;
        this.configuration = configuration;
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/activity+json"));
    }

    public async Task<HttpResponseMessage> PostAsync(IObjectOrLink objectOrLink, Uri requestUri)
    {
        string serializedBody = Serialize(objectOrLink);

        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, requestUri)
        {
            Content = new StringContent(serializedBody, mediaType: new MediaTypeHeaderValue("application/activity+json"))
        };

        // Specify the 'x-ms-date' header as the current UTC timestamp according to the RFC1123 standard
        string date = DateTimeOffset.UtcNow.ToString("r", CultureInfo.InvariantCulture);
        // Compute a content hash for the 'x-ms-content-sha256' header.
        string contentHash = $"SHA-256={ComputeContentHash(serializedBody)}";

        // Prepare a string to sign.
        string stringToSign = $"date: {date}\ndigest: {contentHash}";
        Console.WriteLine(stringToSign);
        // Compute the signature.
        string signature = ComputeSignature(stringToSign);
        // Concatenate the string, which will be used in the authorization header.
        string authorizationHeader = $"keyId=\"https://kristoffer-strube.dk/API/ActivityPub/Users/bot#main-key\",headers=\"date digest\",signature=\"{signature}\"";

        // Add a Date header.
        request.Headers.Add("Date", date);
        // Add a Digest header.
        request.Headers.Add("Digest", contentHash);
        // Add a Signature header.
        request.Headers.Add("Signature", authorizationHeader);
        // ActivityStreams requires this.
        request.Headers.Accept.Add(new("application/ld+json"));

        return await httpClient.SendAsync(request);
    }

    public async Task<Uri?> GetInboxUriAsync(IObjectOrLink actor)
    {
        if (actor is ILink { Href: Uri href })
        {
            Console.WriteLine(await httpClient.GetStringAsync(href));
            IObjectOrLink? obj = await httpClient.GetFromJsonAsync<IObjectOrLink>(href);
            if (obj is Person { Inbox.Href: Uri inboxHref })
            {
                return inboxHref;
            }
        }
        else if (actor is Actor actorObject)
        {
            return actorObject.Inbox?.Href;
        }
        return null;
    }

    private string ComputeContentHash(string content)
    {
        using SHA256 sha256 = SHA256.Create();
        byte[] hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(content));
        return Convert.ToBase64String(hashedBytes);
    }

    private string ComputeSignature(string stringToSign)
    {
        string privateKey = configuration["PEM:Private"];

        RSA rsa = RSA.Create();
        rsa.ImportFromPem(privateKey.ToCharArray());
        byte[] bytes = Encoding.ASCII.GetBytes(stringToSign);
        byte[] hash = rsa.SignData(bytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return Convert.ToBase64String(hash);
    }
}
