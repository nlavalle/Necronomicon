using System.Diagnostics;
using System.Numerics;
using System.Text.RegularExpressions;
using Google.Protobuf;
using necronomicon.processor;
using Steam.Protos.Dota2;

namespace necronomicon.model.frames;

public class EmbeddedMessage
{
    public List<uint> Commands;
    private byte[] _messageCache;
    private Necronomicon _parser;
    internal EmbeddedMessage(Necronomicon parser, ByteString byteString)
    {
        _parser = parser;
        _messageCache = byteString.ToByteArray();
        Commands = new List<uint>();
    }

    public void ParseMessages()
    {
        var bitReader = new BitReaderWrapper(_messageCache);
        while (bitReader.Reader.Position + 8 <= bitReader.Reader.Length)
        {
            var messageType = bitReader.ReadEmbeddedInt();
            Commands.Add(messageType);
            var dataSize = bitReader.ReadVarUInt32();

            Debug.Assert(
                dataSize > 0 &&
                bitReader.Reader.Length - bitReader.Reader.Position >= dataSize,
                "Fucked up your embedded packet size bro");

            byte[] messageBuffer;
            Span<byte> messageSpan;
            switch (messageType)
            {
                case (int)SVC_Messages.SvcServerInfo:
                    messageBuffer = new byte[dataSize];
                    messageSpan = messageBuffer;
                    bitReader.ReadToSpanBuffer(messageSpan);
                    CSVCMsg_ServerInfo serverInfo = CSVCMsg_ServerInfo.Parser.ParseFrom(messageSpan);
                    if (serverInfo != null)
                    {
                        // Need this for entity parsing later
                        _parser.ClassIdSize = (uint)BitOperations.Log2((uint)serverInfo.MaxClasses) + 1;

                        // Get game build info
                        var matchPattern = Regex.Match(serverInfo.GameDir, @"/dota_v(\d+)/");
                        if (matchPattern.Groups.Count < 2)
                        {
                            throw new NecronomiconException($"unable to determine game build from {serverInfo.GameDir}");
                        }

                        _parser.GameBuild = uint.Parse(matchPattern.Groups[1].Value);
                    }
                    continue;
                case (int)SVC_Messages.SvcPacketEntities:
                    messageBuffer = new byte[dataSize];
                    messageSpan = messageBuffer;
                    bitReader.ReadToSpanBuffer(messageSpan);
                    CSVCMsg_PacketEntities packetEntities = CSVCMsg_PacketEntities.Parser.ParseFrom(messageBuffer);
                    if (packetEntities != null)
                    {
                        foreach (var handler in _parser.Callbacks.OnSvcPacketEntities)
                        {
                            handler(packetEntities);
                        }
                    }
                    continue;
                case (int)SVC_Messages.SvcCreateStringTable:
                    messageBuffer = new byte[dataSize];
                    messageSpan = messageBuffer;
                    bitReader.ReadToSpanBuffer(messageSpan);
                    CSVCMsg_CreateStringTable createStringTable = CSVCMsg_CreateStringTable.Parser.ParseFrom(messageBuffer);
                    if (createStringTable != null)
                    {
                        foreach (var handler in _parser.Callbacks.OnSvcCreateStringTable)
                        {
                            handler(createStringTable);
                        }
                    }
                    continue;
                case (int)SVC_Messages.SvcUpdateStringTable:
                    messageBuffer = new byte[dataSize];
                    messageSpan = messageBuffer;
                    bitReader.ReadToSpanBuffer(messageSpan);
                    CSVCMsg_UpdateStringTable updateStringTable = CSVCMsg_UpdateStringTable.Parser.ParseFrom(messageBuffer);
                    if (updateStringTable != null)
                    {
                        foreach (var handler in _parser.Callbacks.OnSvcUpdateStringTable)
                        {
                            handler(updateStringTable);
                        }
                    }
                    continue;
                case (int)EDotaUserMessages.DotaUmCombatLogDataHltv:
                    messageBuffer = new byte[dataSize];
                    messageSpan = messageBuffer;
                    bitReader.ReadToSpanBuffer(messageSpan);
                    var test = CMsgDOTACombatLogEntry.Parser.ParseFrom(messageBuffer);
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
                            // Debug.WriteLine("sup");
                            break;
                        case DOTA_COMBATLOG_TYPES.DotaCombatlogLocation:
                            // Debug.WriteLine("hallo");
                            break;
                        case DOTA_COMBATLOG_TYPES.DotaCombatlogHeroSaved:
                            // Debug.WriteLine("idk");
                            break;
                    }
                    continue;
            }

            bitReader.Reader.Position += (int)(dataSize * 8);
        }
    }
}