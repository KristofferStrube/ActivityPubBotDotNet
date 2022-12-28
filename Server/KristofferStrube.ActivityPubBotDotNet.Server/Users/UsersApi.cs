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
        group.MapGet("/{userId}/following", Following);

        return group;
    }

    public static Results<BadRequest<string>, Ok<IObject>> Index(string userId, IConfiguration configuration, ActivityPubDbContext dbContext)
    {
        if (userId is not "bot")
        {
            return TypedResults.BadRequest("User was not 'bot'.");
        }

        UserInfo? user = dbContext.Users.Find($"{configuration["HostUrls:Server"]}/Users/{userId}");
        if (user is null)
        {
            return TypedResults.BadRequest("User could not be found.");
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
            return TypedResults.BadRequest("User was not 'bot'.");
        }

        switch (obj)
        {
            case Follow follow:
                if (follow.Actor is null)
                {
                    return TypedResults.BadRequest("Follow request had no actor.");
                }
                if (activityPub.GetPersonId(follow.Object?.First()) is not string objectPersonId)
                {
                    return TypedResults.BadRequest("The Object was not a Link or did not have a id.");
                }
                if (objectPersonId != $"{configuration["HostUrls:Server"]}/Users/{userId}")
                {
                    return TypedResults.BadRequest("The Object Id did not match the address of this inbox.");
                }
                Uri? inbox = await activityPub.GetInboxUriAsync(follow.Actor.First());
                if (inbox is null)
                {
                    return TypedResults.BadRequest("The User had no inbox specified.");
                }

                Accept accept = new Accept()
                {
                    JsonLDContext = new List<ReferenceTermDefinition>() { new(new("https://www.w3.org/ns/activitystreams")) },
                    Actor = new List<Link>() { new() { Href = new($"{configuration["HostUrls:Server"]}/Users/{userId}") } },
                    Id = $"{configuration["HostUrls:Server"]}/Activity/{Guid.NewGuid()}",
                    Object = new List<IObject>() { follow }
                };
                HttpResponseMessage response = await activityPub.PostAsync(accept, inbox);

                if (!response.IsSuccessStatusCode)
                {
                    return TypedResults.BadRequest("Could not send Accept message.");
                }
                if (activityPub.GetPersonId(follow.Actor.First()) is not string followerId)
                {
                    return TypedResults.BadRequest("The Actor was not a Link or did not have a id.");
                }

                if (dbContext.FollowRelations.Find(followerId, userId) is not null)
                {
                    return TypedResults.Accepted("Accepted as the Actor already followed the Object.");
                }
                UserInfo dbUser = dbContext.Users.Find($"{configuration["HostUrls:Server"]}/Users/{userId}")!;
                UserInfo? dbFollower = dbContext.Users.Find(followerId);
                if (dbFollower is null)
                {
                    dbFollower = new("Some Follower", followerId);
                    dbContext.Add(dbFollower);
                }
                dbContext.Add(new FollowRelation(dbFollower.Id, dbUser.Id));
                dbContext.SaveChanges();

                return TypedResults.Accepted("Accepted");
            case Undo undo:
                switch (undo.Object?.First())
                {
                    case Follow follow:
                        if (activityPub.GetPersonId(follow.Actor?.First()) is not string actorId || follow.Object?.First() is not ILink { Href: Uri objectUri })
                        {
                            return TypedResults.BadRequest($"Could not Undo Follow either because the actor was not a Link or did not have an id or because the Object was not a Link.");
                        }
                        FollowRelation? followRelation = dbContext.FollowRelations.Find(actorId, objectUri.ToString());
                        if (followRelation is null)
                        {
                            return TypedResults.BadRequest($"Could not Undo Follow because the Actor was not following the Object.");
                        }
                        dbContext.FollowRelations.Remove(followRelation);
                        dbContext.SaveChanges();
                        return TypedResults.Accepted("Accepted");
                    default:
                        return TypedResults.BadRequest(Serialize(undo.Object));
                }
            default:
                return TypedResults.BadRequest("The Object type was not supported.");
        }
    }

    public static Results<BadRequest<string>, Ok<IObjectOrLink>> Followers(string userId, IConfiguration configuration, ActivityPubDbContext dbContext)
    {
        if (userId is not "bot")
        {
            return TypedResults.BadRequest("User was not 'bot'.");
        }

        var relations = dbContext.FollowRelations.Where(f => f.FollowedId == $"{configuration["HostUrls:Server"]}/Users/{userId}").ToList();

        IObjectOrLink collection = new Collection()
        {
            JsonLDContext = new List<ReferenceTermDefinition>() { new(new("https://www.w3.org/ns/activitystreams")) },
            Id = $"{configuration["HostUrls:Server"]}/Users/{userId}/followers",
            Type = new List<string>() { "Collection" },
            Items = relations.Select(f => new Link() { Href = new(f.FollowerId) }).ToList(),
            TotalItems = (uint)relations.Count()
        };
        return TypedResults.Ok(collection);
    }

    public static Results<BadRequest<string>, Ok<IObjectOrLink>> Following(string userId, IConfiguration configuration, ActivityPubDbContext dbContext)
    {
        if (userId is not "bot")
        {
            return TypedResults.BadRequest("User was not 'bot'.");
        }

        var relations = dbContext.FollowRelations.Where(f => f.FollowerId == $"{configuration["HostUrls:Server"]}/Users/{userId}").ToList();

        IObjectOrLink collection = new Collection()
        {
            JsonLDContext = new List<ReferenceTermDefinition>() { new(new("https://www.w3.org/ns/activitystreams")) },
            Id = $"{configuration["HostUrls:Server"]}/Users/{userId}/following",
            Type = new List<string>() { "Collection" },
            Items = relations.Select(f => new Link() { Href = new(f.FollowedId) }).ToList(),
            TotalItems = (uint)relations.Count()
        };
        return TypedResults.Ok(collection);
    }
}
