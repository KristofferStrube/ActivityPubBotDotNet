namespace KristofferStrube.ActivityPubBotDotNet.Server;

public record UserInfo(string Name, string Id)
{
    public List<FollowRelation> Followers { get; set; }
    public List<FollowRelation> Following { get; set; }
}
