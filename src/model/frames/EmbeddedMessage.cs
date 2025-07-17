using System.Diagnostics;
using Google.Protobuf;
using necronomicon.source;
using Steam.Protos.Dota2;

namespace necronomicon.model.frames;

public class EmbeddedMessage
{
    private byte[] _messageCache;

    internal EmbeddedMessage(ByteString byteString)
    {
        _messageCache = byteString.ToByteArray();
    }

    public void ParseMessages()
    {
        using var ms = new MemoryStream(_messageCache);
        using var reader = new BinaryReader(ms);
        while (ms.Position < ms.Length)
        {
            int msgType = ReadVarInt(reader);
            NET_Messages netType = (NET_Messages)msgType;
            int msgSize = ReadVarInt(reader);

            byte[] msgData = reader.ReadBytes(msgSize);

            switch (netType)
            {
                case NET_Messages.NetTick:
                    var tickMsg = CNETMsg_Tick.Parser.ParseFrom(msgData);
                    Console.WriteLine($"Tick: {tickMsg.Tick}");
                    break;
                case NET_Messages.NetStringCmd:
                    var strMsg = CNETMsg_StringCmd.Parser.ParseFrom(msgData);
                    Console.WriteLine($"StringCmd: {strMsg.Command}");
                    break;
                default:
                    Debug.WriteLine($"Unknown message type: {netType}");
                    break;
            }
        }
    }
    private int ReadVarInt(BinaryReader reader)
    {
        int result = 0;
        int shift = 0;
        byte b;

        do
        {
            b = reader.ReadByte();
            result |= (b & 0x7F) << shift;
            shift += 7;
        } while ((b & 0x80) != 0);

        return result;
    }
}