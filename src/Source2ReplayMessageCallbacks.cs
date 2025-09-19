using System.Collections.Frozen;
using Steam.Protos.Dota2;

namespace necronomicon;

public class Source2ReplayMessageCallbacks
{
    private readonly FrozenDictionary<uint, Action<Source2ReplayPacket.MessageData>> _callbacks;
    private readonly Action<Source2ReplayPacket.Message> _message;
    private readonly Action<int, CDemoStringTables>? _fullPacketAux;
    private Action<Source2ReplayPacket.Message>? _unknown;

    public Source2ReplayMessageCallbacks(FrozenDictionary<uint, Action<Source2ReplayPacket.MessageData>> callbacks, Action<int, CDemoStringTables>? onFullPacketAux = null, Action<Source2ReplayPacket.Message>? unknown = null)
    {
        _callbacks = callbacks;
        _message = ProcessPacket;
        _fullPacketAux = onFullPacketAux;
        _unknown = unknown;
    }

    public void AttachToFrameCallbacks(Source2ReplayFrameCallbacks frameCallbacks)
    {
        var packet = PacketFrameEntry;
        frameCallbacks[EDemoCommands.DemPacket] = packet;
        frameCallbacks[EDemoCommands.DemSignonPacket] = packet;
        frameCallbacks[EDemoCommands.DemFullPacket] = FullPacketFrameEntry;
    }

    public void SetUnknownCallback(Action<Source2ReplayPacket.Message>? callback)
    {
        _unknown = callback;
    }

    public void PacketFrameEntry(Source2Replay.FrameData data)
    {
        var protobuf = data.GetAsProtobuf<CDemoPacket>();

        Source2ReplayPacket.ProcessPacketMessages(protobuf.Data.Span, data.FrameTick, _message);
    }

    public void FullPacketFrameEntry(Source2Replay.FrameData data)
    {
        var protobuf = data.GetAsProtobuf<CDemoFullPacket>();

        if (_fullPacketAux is not null)
            _fullPacketAux(data.FrameTick, protobuf.StringTable);

        Source2ReplayPacket.ProcessPacketMessages(protobuf.Packet.Data.Span, data.FrameTick, _message);
    }

    public void ProcessPacket(Source2ReplayPacket.Message message)
    {
        if (_callbacks.TryGetValue(message.MessageType, out var callback))
        {
            message.RegisterCallbackForData(callback);
        }
        else if (_unknown is not null)
        {
            _unknown(message);
        }
    }

    public sealed class Builder
    {
        public Dictionary<uint, Action<Source2ReplayPacket.MessageData>> Callbacks { get; } = [];
        public Action<int, CDemoStringTables>? OnFullPacketAux { get; set; }
        public Action<Source2ReplayPacket.Message>? OnUnknownMessage { get; set; }

        public Source2ReplayMessageCallbacks Complete()
            => new(Callbacks.ToFrozenDictionary(), OnFullPacketAux, OnUnknownMessage);
    }
}
