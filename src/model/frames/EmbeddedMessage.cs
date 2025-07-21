using System.Diagnostics;
using Google.Protobuf;
using necronomicon.processor;
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
        var bitReader = new BitReaderWrapper(_messageCache);
        while (bitReader.Reader.Position + 8 <= bitReader.Reader.Length)
        {
            var messageType = bitReader.ReadEmbeddedInt();
            var dataSize = bitReader.ReadVarInt32();

            Debug.Assert(
                dataSize > 0 &&
                bitReader.Reader.Length - bitReader.Reader.Position >= dataSize,
                "Fucked up your embedded packet size bro");

            switch (messageType)
            {
                case (int)SVC_Messages.SvcPacketEntities:
                    byte[] byteBuffer1 = new byte[dataSize];
                    Span<byte> messageSpan2 = byteBuffer1;
                    bitReader.ReadToSpanBuffer(messageSpan2);
                    PacketEntity packetEntity = new PacketEntity(CSVCMsg_PacketEntities.Parser.ParseFrom(byteBuffer1));
                    // packetEntity.Parse();
                    continue;
                case (int)EDotaUserMessages.DotaUmCombatLogDataHltv:
                    byte[] byteBuffer = new byte[dataSize];
                    Span<byte> messageSpan = byteBuffer;
                    bitReader.ReadToSpanBuffer(messageSpan);
                    var test = CMsgDOTACombatLogEntry.Parser.ParseFrom(byteBuffer);
                    if (test.LocationX != 0 || test.LocationY != 0)
                    {
                        if (
                            test.Type != DOTA_COMBATLOG_TYPES.DotaCombatlogGold &&
                            test.Type != DOTA_COMBATLOG_TYPES.DotaCombatlogXp
                        )
                        {
                            Debug.WriteLine($@"
Target: {test.TargetName} - {test.TargetTeam}
Type: {test.Type}
Location (X,Y): {test.LocationX}, {test.LocationY}
Timestamp: {test.Timestamp}
                        ");
                        }

                    }
                    switch (test.Type)
                    {
                        case DOTA_COMBATLOG_TYPES.DotaCombatlogPlayerstats:
                            Debug.WriteLine("sup");
                            break;
                        case DOTA_COMBATLOG_TYPES.DotaCombatlogLocation:
                            Debug.WriteLine("hallo");
                            break;
                        case DOTA_COMBATLOG_TYPES.DotaCombatlogHeroSaved:
                            Debug.WriteLine("idk");
                            break;
                    }
                    continue;
            }

            bitReader.Reader.Position += dataSize * 8;
        }
    }
}