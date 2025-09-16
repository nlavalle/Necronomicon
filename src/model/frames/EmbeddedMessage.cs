using System.Buffers;
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
    private ReadOnlyMemory<byte> _messageCache;
    private Necronomicon _parser;
    internal EmbeddedMessage(Necronomicon parser, ByteString byteString)
    {
        _parser = parser;
        _messageCache = byteString.Memory;
        Commands = new List<uint>();
    }

    public void ParseMessages()
    {
        byte[]? messageBuffer = null;

        var bitReader = new FastBitReader(_messageCache.Span);
        while (bitReader.Reader.Position + 8 <= bitReader.Reader.Length)
        {
            Span<byte> messageSpan;

            var messageType = bitReader.ReadEmbeddedInt();
            Commands.Add(messageType);
            var dataSize = (int)bitReader.ReadVarUInt32();

            Debug.Assert(
                dataSize >= 0 &&
                bitReader.Reader.Length - bitReader.Reader.Position >= dataSize,
                "Fucked up your embedded packet size bro");

            switch (messageType)
            {
                case (int)SVC_Messages.SvcServerInfo:
                    messageSpan = ProcessHelpers.ResolveArray(ref messageBuffer, dataSize);
                    bitReader.ReadToSpanBuffer(messageSpan);
                    CSVCMsg_ServerInfo serverInfo = CSVCMsg_ServerInfo.Parser.ParseFrom(messageSpan);
                    if (serverInfo != null)
                    {
                        // Need this for entity parsing later
                        _parser.ClassIdSize = (uint)BitOperations.Log2((uint)serverInfo.MaxClasses) + 1;

                        // Get game build info
                        var matchPattern = Regex.Match(serverInfo.GameDir, @"[dota|citadel]_v(\d+)");
                        if (matchPattern.Groups.Count < 2)
                        {
                            throw new NecronomiconException($"unable to determine game build from {serverInfo.GameDir}");
                        }

                        _parser.GameBuild = uint.Parse(matchPattern.Groups[1].Value);
                    }
                    continue;
                case (int)SVC_Messages.SvcPacketEntities:
                    messageSpan = ProcessHelpers.ResolveArray(ref messageBuffer, dataSize);
                    bitReader.ReadToSpanBuffer(messageSpan);
                    CSVCMsg_PacketEntities packetEntities = CSVCMsg_PacketEntities.Parser.ParseFrom(messageSpan);
                    if (packetEntities != null)
                    {
                        foreach (var handler in _parser.Callbacks.OnSvcPacketEntities)
                        {
                            handler(packetEntities);
                        }
                    }
                    continue;
                case (int)SVC_Messages.SvcCreateStringTable:
                    messageSpan = ProcessHelpers.ResolveArray(ref messageBuffer, dataSize);
                    bitReader.ReadToSpanBuffer(messageSpan);
                    CSVCMsg_CreateStringTable createStringTable = CSVCMsg_CreateStringTable.Parser.ParseFrom(messageSpan);
                    if (createStringTable != null)
                    {
                        foreach (var handler in _parser.Callbacks.OnSvcCreateStringTable)
                        {
                            handler(createStringTable);
                        }
                    }
                    continue;
                case (int)SVC_Messages.SvcUpdateStringTable:
                    messageSpan = ProcessHelpers.ResolveArray(ref messageBuffer, dataSize);
                    bitReader.ReadToSpanBuffer(messageSpan);
                    CSVCMsg_UpdateStringTable updateStringTable = CSVCMsg_UpdateStringTable.Parser.ParseFrom(messageSpan);
                    if (updateStringTable != null)
                    {
                        foreach (var handler in _parser.Callbacks.OnSvcUpdateStringTable)
                        {
                            handler(updateStringTable);
                        }
                    }
                    continue;
                case (int)SVC_Messages.SvcUserCmds:
                    messageBuffer = new byte[dataSize];
                    messageSpan = messageBuffer;
                    bitReader.ReadToSpanBuffer(messageSpan);
                    CSVCMsg_UserCommands userCommands = CSVCMsg_UserCommands.Parser.ParseFrom(messageBuffer);
                    if (userCommands != null)
                    {
                        // User Commands not implemented yet
                    }
                    continue;
                case (int)EDotaUserMessages.DotaUmCombatLogDataHltv:
                    messageSpan = ProcessHelpers.ResolveArray(ref messageBuffer, dataSize);
                    bitReader.ReadToSpanBuffer(messageSpan);
                    CMsgDOTACombatLogEntry combatLogEntry = CMsgDOTACombatLogEntry.Parser.ParseFrom(messageSpan);

                    if (combatLogEntry != null)
                    {
                        foreach (var handler in _parser.Callbacks.OnCombatLogEntries)
                        {
                            handler(combatLogEntry);
                        }
                    }
                    continue;
                default:
                    bitReader.Reader.Position += dataSize * 8;
                    continue;
            }

        }

        if (messageBuffer is not null)
            ArrayPool<byte>.Shared.Return(messageBuffer);
    }
}
