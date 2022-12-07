using KristofferStrube.ActivityStreams;
using KristofferStrube.ActivityStreams.JsonLD;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using static System.Text.Json.JsonSerializer;

namespace KristofferStrube.ActivityPubBotDotNet.Server;

public static class UsersApi
{
    public static RouteGroupBuilder MapUsers(this IEndpointRouteBuilder routes)
    {
        RouteGroupBuilder group = routes.MapGroup("/users");
        group.WithTags("Users");

        group.MapGet("/{userId}", Index);
        group.MapPost("/{userId}/inbox", Inbox);
        group.MapGet("/{userId}/followers", Followers);

        return group;
    }

    public static Results<BadRequest<string>, Ok<IObject>> Index(string userId, IConfiguration configuration, ActivityPubDbContext dbContext)
    {
        if (userId is not "bot")
        {
            return TypedResults.BadRequest("User was not 'bot'");
        }

        var user = dbContext.Users.Find(userId);
        if (user is null)
        {
            return TypedResults.BadRequest("User could not be found");
        }

        return TypedResults.Ok((IObject)new Person()
        {
            JsonLDContext = new List<ReferenceTermDefinition>() { new(new("https://www.w3.org/ns/activitystreams")) },
            Id = $"{configuration["HostUrls:Server"]}/Users/{userId}",
            Type = new List<string>() { "Person" },
            PreferredUsername = user.Name,
            Inbox = new Link() { Href = new Uri($"{configuration["HostUrls:Server"]}/Users/{userId}/inbox") },
            Outbox = new Link() { Href = new Uri($"{configuration["HostUrls:Server"]}/Users/{userId}/outbox") },
            Followers = new Link() { Href = new Uri($"{configuration["HostUrls:Server"]}/Users/{userId}/followers") },
            Following = new Link() { Href = new Uri($"{configuration["HostUrls:Server"]}/Users/{userId}/following") },
            Published = new DateTime(2022, 11, 27),
            Icon = new List<Image> {
                    new() {
                        Url = new Link[] { new() { Href = new("https://kristoffer-strube.dk/bot.png") } },
                        MediaType = "image/png"
                    }
                },
            Image = new List<Image> {
                    new() {
                        Url = new Link[] { new() { Href = new("https://kristoffer-strube.dk/bot_header.PNG") } },
                        MediaType = "image/png"
                    }
                },
            Summary = new string[] { "This is a ActivityPub bot written in .NET." },
            ExtensionData = new()
                {
                    { "manuallyApprovesFollowers", SerializeToElement(true) },
                    { "discoverable", SerializeToElement(true) },
                    {
                        "publicKey",
                        SerializeToElement(new
                        {
                            id = $"{configuration["HostUrls:Server"]}/Users/{userId}#main-key",
                            owner = $"{configuration["HostUrls:Server"]}/Users/{userId}",
                            publicKeyPem = configuration["PEM:Public"]
                        })
                    }
                }
        });
    }

    public static async Task<Results<BadRequest<string>, Accepted>> Inbox(string userId, [FromBody] IObject obj, IConfiguration configuration, ActivityPubDbContext dbContext, ActivityPubService activityPub)
    {
        if (userId is not "bot")
        {
            return TypedResults.BadRequest("User was not 'bot'");
        }

        switch (obj)
        {
            case Follow follow:
                Accept accept = new Accept()
                {
                    JsonLDContext = new List<ReferenceTermDefinition>() { new(new("https://www.w3.org/ns/activitystreams")) },
                    Actor = new List<Link>() { new() { Href = new($"{configuration["HostUrls:Server"]}/Users/{userId}") } },
                    Id = $"{configuration["HostUrls:Server"]}/Activity/{Guid.NewGuid()}",
                    Object = new List<IObject>() { follow }
                };
                if (follow.Actor is null)
                {
                    return TypedResults.BadRequest("Follow request had no actor.");
                }
                Uri? inbox = await activityPub.GetInboxUriAsync(follow.Actor.First());
                if (inbox is null)
                {
                    return TypedResults.BadRequest("The User had no inbox specified.");
                }
                HttpResponseMessage response = await activityPub.PostAsync(accept, inbox);
                if (!response.IsSuccessStatusCode)
                {
                    return TypedResults.BadRequest("Could not send Accept message");
                }
                if (follow.Actor.First() is not ILink { Href: Uri actorUri })
                {
                    return TypedResults.BadRequest("The Actor was not a link or URI.");
                }
                UserInfo dbUser = dbContext.Users.Find($"{configuration["HostUrls:Server"]}/Users/{userId}")!;
                UserInfo? dbFollower = dbContext.Users.Find(follow.Id);
                if (dbFollower is null)
                {
                    dbFollower = new("Some Follower", actorUri.ToString());
                    dbContext.Add(dbFollower);
                }
                dbContext.Add(new FollowRelation(dbFollower.Id, dbUser.Id));
                dbContext.SaveChanges();
                return TypedResults.Accepted("Accepted");
        }
        return TypedResults.BadRequest("The Object type was not supported");
    }

    public static Results<BadRequest<string>, Ok<IObjectOrLink>> Followers(string userId, IConfiguration configuration, ActivityPubDbContext dbContext)
    {
        if (userId is not "bot")
        {
            return TypedResults.BadRequest("User was not 'bot'");
        }

        IObjectOrLink collection = new Collection()
        {
            JsonLDContext = new List<ReferenceTermDefinition>() { new(new("https://www.w3.org/ns/activitystreams")) },
            Id = $"{configuration["HostUrls:Server"]}/Users/{userId}/followers",
            Type = new List<string>() { "Collection" },
            Items = dbContext.FollowRelations.Where(f => f.FollowedId == $"{configuration["HostUrls:Server"]}/Users/{userId}").Select(f => new Link() { Href = new(f.FollowerId) }).ToList()
        };
        return TypedResults.Ok(collection);
    }

    public static Results<BadRequest<string>, Ok<IObjectOrLink>> Following(string userId, IConfiguration configuration, ActivityPubDbContext dbContext)
    {
        if (userId is not "bot")
        {
            return TypedResults.BadRequest("User was not 'bot'");
        }

        IObjectOrLink collection = new Collection()
        {
            JsonLDContext = new List<ReferenceTermDefinition>() { new(new("https://www.w3.org/ns/activitystreams")) },
            Id = $"{configuration["HostUrls:Server"]}/Users/{userId}/following",
            Type = new List<string>() { "Collection" },
            Items = dbContext.FollowRelations.Where(f => f.FollowerId == $"{configuration["HostUrls:Server"]}/Users/{userId}").Select(f => new Link() { Href = new(f.FollowedId) }).ToList()
        };
        return TypedResults.Ok(collection);
    }
}
