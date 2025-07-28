using necronomicon.model.frames;
using Steam.Protos.Dota2;

namespace necronomicon.model.dem;

public class DemPackets
{
    public readonly List<EmbeddedMessage> _embeddedMessages;
    private readonly Necronomicon _parser;
    public DemPackets(Necronomicon parser)
    {
        _parser = parser;
        _embeddedMessages = new List<EmbeddedMessage>();

        _parser.Callbacks.OnDemPacket.Add(OnCDemoPacket);
    }

    public async Task OnCDemoPacket(CDemoPacket packet)
    {
        EmbeddedMessage embeddedMessage = new EmbeddedMessage(_parser, packet.Data);
        _embeddedMessages.Add(embeddedMessage);
        embeddedMessage.ParseMessages();
        await Task.CompletedTask;
    }
}
