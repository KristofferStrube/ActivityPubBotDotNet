namespace KristofferStrube.ActivityPubBotDotNet.Server;

public record FollowRelation(string FollowerId, string FollowedId)
{
    public UserInfo Follower { get; set; }

    public UserInfo Followed { get; set; }
}
