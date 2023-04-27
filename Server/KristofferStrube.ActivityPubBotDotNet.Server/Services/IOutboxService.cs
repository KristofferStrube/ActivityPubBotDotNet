using KristofferStrube.ActivityStreams;

namespace KristofferStrube.ActivityPubBotDotNet.Server;

public interface IOutboxService
{
    bool HasOutboxFor(string userId);
    IEnumerable<IObjectOrLink> GetOutboxItems(string userId);
}