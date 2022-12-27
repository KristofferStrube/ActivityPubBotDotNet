using KristofferStrube.ActivityPubBotDotNet.Server.WebFinger;
using KristofferStrube.ActivityStreams;
using KristofferStrube.ActivityStreams.JsonLD;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using static System.Text.Json.JsonSerializer;

namespace KristofferStrube.ActivityPubBotDotNet.Server;

public static class WebFingerApi
{
    public static RouteGroupBuilder MapWebFingers(this IEndpointRouteBuilder routes)
    {
        RouteGroupBuilder group = routes.MapGroup("/webfinger");
        group.WithTags("WebFinger");

        group.MapGet("/", Index);

        return group;
    }

    public static Results<BadRequest<string>, Ok<WebFingerResource>> Index(string resource, IConfiguration configuration, ActivityPubDbContext dbContext)
    {
        if (!resource.Contains(':'))
        {
            return TypedResults.BadRequest("Malformed resource querystring missing ':'.");
        }
        if (!resource.Split(':')[1].Contains('@'))
        {
            return TypedResults.BadRequest("Malformed resource querystring missing '@'.");
        }

        string userId = resource.Split(":")[1].Split("@")[0];
        if (userId is not "bot")
        {
            return TypedResults.BadRequest("User was not 'bot'.");
        }

        UserInfo? user = dbContext.Users.Find($"{configuration["HostUrls:Server"]}/Users/{userId}");
        if (user is null)
        {
            return TypedResults.BadRequest("User could not be found.");
        }

        return TypedResults.Ok(new WebFingerResource(resource)
        {
            Links = new() { new ResourceLink("self", "application/activity+json", new($"{configuration["HostUrls:Server"]}/Users/{userId}")) }
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
                    return TypedResults.BadRequest("Could not send Accept message");
                }
                if (activityPub.GetPersonId(follow.Actor.First()) is not string followerId)
                {
                    return TypedResults.BadRequest("The Actor was not a Link or did not have a id.");
                }

                if (dbContext.FollowRelations.Find(followerId, userId) is null)
                {
                    return TypedResults.Accepted("Accepted as the Actor already followed the Object");
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
                            return TypedResults.BadRequest($"Could not Undo Follow because the Actor was not following the Object");
                        }
                        dbContext.FollowRelations.Remove(followRelation);
                        dbContext.SaveChanges();
                        return TypedResults.Accepted("Accepted");
                    default:
                        return TypedResults.BadRequest(Serialize(undo.Object));
                }
            default:
                return TypedResults.BadRequest("The Object type was not supported");
        }
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
