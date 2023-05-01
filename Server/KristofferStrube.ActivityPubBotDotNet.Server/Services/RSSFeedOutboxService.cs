using KristofferStrube.ActivityStreams;
using KristofferStrube.ActivityStreams.JsonLD;
using System.ServiceModel.Syndication;
using System.Xml;

namespace KristofferStrube.ActivityPubBotDotNet.Server;

public class RSSFeedOutboxService : IOutboxService
{
    private readonly string rssFeedUrl;
    private readonly string userId;
    private readonly IConfiguration configuration;

    public RSSFeedOutboxService(string rssFeedUrl, string userId, IConfiguration configuration)
    {
        this.rssFeedUrl = rssFeedUrl;
        this.userId = userId;
        this.configuration = configuration;
    }

    public bool HasOutboxFor(string userId) => this.userId == userId;

    public IEnumerable<IObjectOrLink> GetOutboxItems(string userId)
    {
        XmlReader reader = XmlReader.Create(rssFeedUrl);
        SyndicationFeed feed = SyndicationFeed.Load(reader);
        reader.Close();
        return feed.Items.Select(item => new Create()
        {
            Id = $"{configuration["HostUrls:Server"]}/Users/{userId}/outbox/{item.Id}/activity",
            To = new List<Link> { new() { Href = new("https://www.w3.org/ns/activitystreams#Public") } },
            Object = new List<IObject>
            {
                new Article()
                {
                    Id = $"{configuration["HostUrls:Server"]}/Users/{userId}/outbox/{item.Id}",
                    Content = new List<string>() {
                        $"""
                           <h1>{item.Title.Text}</h1>
                           {item.Summary.Text}
                           {(item.Content is TextSyndicationContent { } content ? content.Text : string.Empty)}
                        """
                    },
                    Published = item.PublishDate.DateTime,
                    To = new List<Link> { new() { Href = new("https://www.w3.org/ns/activitystreams#Public") } },
                    AttributedTo = new List<Link>() { new Link() { Href = new($"{configuration["HostUrls:Server"]}/Users/{userId}") } }
                }
            }
        });
    }
}
