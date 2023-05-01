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
}
