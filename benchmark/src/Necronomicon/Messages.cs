using System.Diagnostics;
using BenchmarkDotNet.Attributes;
using necronomicon;
using Steam.Protos.Dota2;

namespace Benchmarks.Necronomicon;

[MemoryDiagnoser]
public class Messages
{
    private static readonly string FileName = Frames.GetPathName();

    [Benchmark]
    public void MessagesFrameBase()
    {
        using var file = File.Open(FileName, FileMode.Open, FileAccess.Read);
        MessageBaseFrameOnlyParser.RunAsync(file).Wait();
    }

    [Benchmark]
    public void MessagesOnly()
    {
        using var file = File.Open(FileName, FileMode.Open, FileAccess.Read);
        MessageOnlyParser.RunAsync(file).Wait();
    }

    [Benchmark]
    public void MessagesOnlyDelStored()
    {
        using var file = File.Open(FileName, FileMode.Open, FileAccess.Read);
        MessageOnlyDelStoredParser.RunAsync(file).Wait();
    }

    [Benchmark]
    public void MessageDataOnly()
    {
        using var file = File.Open(FileName, FileMode.Open, FileAccess.Read);
        MessageDataOnlyParser.RunAsync(file).Wait();
    }

    [Benchmark]
    public void MessageDataOnlyDelStored()
    {
        using var file = File.Open(FileName, FileMode.Open, FileAccess.Read);
        MessageDataOnlyDelStoredParser.RunAsync(file).Wait();
    }

    [Benchmark]
    public void MessageDataProtobufs()
    {
        using var file = File.Open(FileName, FileMode.Open, FileAccess.Read);
        MessageDataProtobufsParser.RunAsync(file).Wait();
    }

    [Benchmark]
    public void MessageDataFromCallbacks()
    {
        using var file = File.Open(FileName, FileMode.Open, FileAccess.Read);
        MessageDataProtobufsCallbacksParser.RunAsync(file).Wait();
    }

    public class MessageBaseFrameOnlyParser
    {
        private Source2ReplayFrameCallbacks? _dellies;

        public MessageBaseFrameOnlyParser()
        {
        }

        public static async Task RunAsync(Stream stream)
        {
            var fella = new MessageBaseFrameOnlyParser();

            await Replay.ParseFileAsync(stream, fella.RunTheReplay);
        }

        private void RunTheReplay(Replay.File file)
        {
            var callbacks = new Source2ReplayFrameCallbacks(OnDemFileHeader);
            _dellies = callbacks;
            file.ParseAsSource2Replay(callbacks);
        }

        private void OnDemFileHeader(Source2Replay.FrameData data)
        {
            var fh = data.GetAsProtobuf<CDemoFileHeader>();

            var callbacks = _dellies;
            Debug.Assert(callbacks is not null);

            var onPacket = OnDemPacket;
            callbacks[EDemoCommands.DemPacket] = onPacket;
            callbacks[EDemoCommands.DemSignonPacket] = onPacket;
            callbacks[EDemoCommands.DemFullPacket] = OnDemFullPacket;
        }

        private void OnDemPacket(Source2Replay.FrameData data)
        {
            var p = data.GetAsProtobuf<CDemoPacket>();
        }

        private void OnDemFullPacket(Source2Replay.FrameData data)
        {
            var fp = data.GetAsProtobuf<CDemoFullPacket>();
        }
    }

    public class MessageOnlyParser
    {
        private Source2ReplayFrameCallbacks? _dellies;

        public MessageOnlyParser()
        {
        }

        public static async Task RunAsync(Stream stream)
        {
            var fella = new MessageOnlyParser();

            await Replay.ParseFileAsync(stream, fella.RunTheReplay);
        }

        private void RunTheReplay(Replay.File file)
        {
            var callbacks = new Source2ReplayFrameCallbacks(OnDemFileHeader);
            _dellies = callbacks;
            file.ParseAsSource2Replay(callbacks);
        }

        private void OnDemFileHeader(Source2Replay.FrameData data)
        {
            var fh = data.GetAsProtobuf<CDemoFileHeader>();

            var callbacks = _dellies;
            Debug.Assert(callbacks is not null);

            var onPacket = OnDemPacket;
            callbacks[EDemoCommands.DemPacket] = onPacket;
            callbacks[EDemoCommands.DemSignonPacket] = onPacket;
            callbacks[EDemoCommands.DemFullPacket] = OnDemFullPacket;
        }

        private void OnDemPacket(Source2Replay.FrameData data)
        {
            var p = data.GetAsProtobuf<CDemoPacket>();
            Source2ReplayPacket.ProcessPacketMessages(p.Data.Span, data.FrameTick, OnMessage);
        }

        private void OnDemFullPacket(Source2Replay.FrameData data)
        {
            var fp = data.GetAsProtobuf<CDemoFullPacket>();
            Source2ReplayPacket.ProcessPacketMessages(fp.Packet.Data.Span, data.FrameTick, OnMessage);
        }

        private void OnMessage(Source2ReplayPacket.Message message)
        {
        }
    }

    public class MessageOnlyDelStoredParser
    {
        private Source2ReplayFrameCallbacks? _dellies;
        private readonly Action<Source2ReplayPacket.Message> _delly;

        public MessageOnlyDelStoredParser()
        {
            _delly = OnMessage;
        }

        public static async Task RunAsync(Stream stream)
        {
            var fella = new MessageOnlyDelStoredParser();

            await Replay.ParseFileAsync(stream, fella.RunTheReplay);
        }

        private void RunTheReplay(Replay.File file)
        {
            var callbacks = new Source2ReplayFrameCallbacks(OnDemFileHeader);
            _dellies = callbacks;
            file.ParseAsSource2Replay(callbacks);
        }

        private void OnDemFileHeader(Source2Replay.FrameData data)
        {
            var fh = data.GetAsProtobuf<CDemoFileHeader>();

            var callbacks = _dellies;
            Debug.Assert(callbacks is not null);

            var onPacket = OnDemPacket;
            callbacks[EDemoCommands.DemPacket] = onPacket;
            callbacks[EDemoCommands.DemSignonPacket] = onPacket;
            callbacks[EDemoCommands.DemFullPacket] = OnDemFullPacket;
        }

        private void OnDemPacket(Source2Replay.FrameData data)
        {
            var p = data.GetAsProtobuf<CDemoPacket>();
            Source2ReplayPacket.ProcessPacketMessages(p.Data.Span, data.FrameTick, _delly);
        }

        private void OnDemFullPacket(Source2Replay.FrameData data)
        {
            var fp = data.GetAsProtobuf<CDemoFullPacket>();
            Source2ReplayPacket.ProcessPacketMessages(fp.Packet.Data.Span, data.FrameTick, _delly);
        }

        private void OnMessage(Source2ReplayPacket.Message message)
        {
        }
    }

    public class MessageDataOnlyParser
    {
        private Source2ReplayFrameCallbacks? _dellies;
        private readonly Action<Source2ReplayPacket.Message> _delly;

        public MessageDataOnlyParser()
        {
            _delly = OnMessage;
        }

        public static async Task RunAsync(Stream stream)
        {
            var fella = new MessageDataOnlyParser();

            await Replay.ParseFileAsync(stream, fella.RunTheReplay);
        }

        private void RunTheReplay(Replay.File file)
        {
            var callbacks = new Source2ReplayFrameCallbacks(OnDemFileHeader);
            _dellies = callbacks;
            file.ParseAsSource2Replay(callbacks);
        }

        private void OnDemFileHeader(Source2Replay.FrameData data)
        {
            var fh = data.GetAsProtobuf<CDemoFileHeader>();

            var callbacks = _dellies;
            Debug.Assert(callbacks is not null);

            var onPacket = OnDemPacket;
            callbacks[EDemoCommands.DemPacket] = onPacket;
            callbacks[EDemoCommands.DemSignonPacket] = onPacket;
            callbacks[EDemoCommands.DemFullPacket] = OnDemFullPacket;
        }

        private void OnDemPacket(Source2Replay.FrameData data)
        {
            var p = data.GetAsProtobuf<CDemoPacket>();
            Source2ReplayPacket.ProcessPacketMessages(p.Data.Span, data.FrameTick, _delly);
        }

        private void OnDemFullPacket(Source2Replay.FrameData data)
        {
            var fp = data.GetAsProtobuf<CDemoFullPacket>();
            Source2ReplayPacket.ProcessPacketMessages(fp.Packet.Data.Span, data.FrameTick, _delly);
        }

        private void OnMessage(Source2ReplayPacket.Message message)
        {
            message.RegisterCallbackForData(OnMessageData);
        }

        private void OnMessageData(Source2ReplayPacket.MessageData data)
        {

        }
    }

    public class MessageDataOnlyDelStoredParser
    {
        private Source2ReplayFrameCallbacks? _dellies;
        private readonly Action<Source2ReplayPacket.Message> _delly;
        private readonly Action<Source2ReplayPacket.MessageData> _delly2;

        public MessageDataOnlyDelStoredParser()
        {
            _delly = OnMessage;
            _delly2 = OnMessageData;
        }

        public static async Task RunAsync(Stream stream)
        {
            var fella = new MessageDataOnlyDelStoredParser();

            await Replay.ParseFileAsync(stream, fella.RunTheReplay);
        }

        private void RunTheReplay(Replay.File file)
        {
            var callbacks = new Source2ReplayFrameCallbacks(OnDemFileHeader);
            _dellies = callbacks;
            file.ParseAsSource2Replay(callbacks);
        }

        private void OnDemFileHeader(Source2Replay.FrameData data)
        {
            var fh = data.GetAsProtobuf<CDemoFileHeader>();

            var callbacks = _dellies;
            Debug.Assert(callbacks is not null);

            var onPacket = OnDemPacket;
            callbacks[EDemoCommands.DemPacket] = onPacket;
            callbacks[EDemoCommands.DemSignonPacket] = onPacket;
            callbacks[EDemoCommands.DemFullPacket] = OnDemFullPacket;
        }

        private void OnDemPacket(Source2Replay.FrameData data)
        {
            var p = data.GetAsProtobuf<CDemoPacket>();
            Source2ReplayPacket.ProcessPacketMessages(p.Data.Span, data.FrameTick, _delly);
        }

        private void OnDemFullPacket(Source2Replay.FrameData data)
        {
            var fp = data.GetAsProtobuf<CDemoFullPacket>();
            Source2ReplayPacket.ProcessPacketMessages(fp.Packet.Data.Span, data.FrameTick, _delly);
        }

        private void OnMessage(Source2ReplayPacket.Message message)
        {
            message.RegisterCallbackForData(_delly2);
        }

        private void OnMessageData(Source2ReplayPacket.MessageData data)
        {

        }
    }

    public class MessageDataProtobufsParser
    {
        private Source2ReplayFrameCallbacks? _dellies;
        private readonly Action<Source2ReplayPacket.Message> _delly;
        private readonly Action<Source2ReplayPacket.MessageData> _delly2;

        public MessageDataProtobufsParser()
        {
            _delly = OnMessage;
            _delly2 = OnMessageData;
        }

        public static async Task RunAsync(Stream stream)
        {
            var fella = new MessageDataProtobufsParser();

            await Replay.ParseFileAsync(stream, fella.RunTheReplay);
        }

        private void RunTheReplay(Replay.File file)
        {
            var callbacks = new Source2ReplayFrameCallbacks(OnDemFileHeader);
            _dellies = callbacks;
            file.ParseAsSource2Replay(callbacks);
        }

        private void OnDemFileHeader(Source2Replay.FrameData data)
        {
            var fh = data.GetAsProtobuf<CDemoFileHeader>();

            var callbacks = _dellies;
            Debug.Assert(callbacks is not null);

            var onPacket = OnDemPacket;
            callbacks[EDemoCommands.DemPacket] = onPacket;
            callbacks[EDemoCommands.DemSignonPacket] = onPacket;
            callbacks[EDemoCommands.DemFullPacket] = OnDemFullPacket;
        }

        private void OnDemPacket(Source2Replay.FrameData data)
        {
            var p = data.GetAsProtobuf<CDemoPacket>();
            Source2ReplayPacket.ProcessPacketMessages(p.Data.Span, data.FrameTick, _delly);
        }

        private void OnDemFullPacket(Source2Replay.FrameData data)
        {
            var fp = data.GetAsProtobuf<CDemoFullPacket>();
            Source2ReplayPacket.ProcessPacketMessages(fp.Packet.Data.Span, data.FrameTick, _delly);
        }

        private void OnMessage(Source2ReplayPacket.Message message)
        {
            message.RegisterCallbackForData(_delly2);
        }

        private void OnMessageData(Source2ReplayPacket.MessageData data)
        {
            switch (data.MessageType)
            {
                case (uint)NET_Messages.NetNop:
                    var pb0 = data.GetAsProtobuf<CNETMsg_NOP>();
                    break;
                // case (uint)NET_Messages.NetDisconnectLegacy:
                //     var pb1 = data.GetAsProtobuf<CNETMsg_Disconnect_Legacy>();
                //     break;
                case (uint)NET_Messages.NetSplitScreenUser:
                    var pb3 = data.GetAsProtobuf<CNETMsg_SplitScreenUser>();
                    break;
                case (uint)NET_Messages.NetTick:
                    var pb4 = data.GetAsProtobuf<CNETMsg_Tick>();
                    break;
                case (uint)NET_Messages.NetStringCmd:
                    var pb5 = data.GetAsProtobuf<CNETMsg_StringCmd>();
                    break;
                case (uint)NET_Messages.NetSetConVar:
                    var pb6 = data.GetAsProtobuf<CNETMsg_SetConVar>();
                    break;
                case (uint)NET_Messages.NetSignonState:
                    var pb7 = data.GetAsProtobuf<CNETMsg_SignonState>();
                    break;
                case (uint)NET_Messages.NetSpawnGroupLoad:
                    var pb8 = data.GetAsProtobuf<CNETMsg_SpawnGroup_Load>();
                    break;
                case (uint)NET_Messages.NetSpawnGroupManifestUpdate:
                    var pb9 = data.GetAsProtobuf<CNETMsg_SpawnGroup_ManifestUpdate>();
                    break;
                case (uint)NET_Messages.NetSpawnGroupSetCreationTick:
                    var pb11 = data.GetAsProtobuf<CNETMsg_SpawnGroup_SetCreationTick>();
                    break;
                case (uint)NET_Messages.NetSpawnGroupUnload:
                    var pb12 = data.GetAsProtobuf<CNETMsg_SpawnGroup_Unload>();
                    break;
                case (uint)NET_Messages.NetSpawnGroupLoadCompleted:
                    var pb13 = data.GetAsProtobuf<CNETMsg_SpawnGroup_LoadCompleted>();
                    break;
                case (uint)NET_Messages.NetDebugOverlay:
                    var pb15 = data.GetAsProtobuf<CNETMsg_DebugOverlay>();
                    break;

                case (uint)CLC_Messages.ClcClientInfo:
                    var pb20 = data.GetAsProtobuf<CCLCMsg_ClientInfo>();
                    break;
                case (uint)CLC_Messages.ClcMove:
                    var pb21 = data.GetAsProtobuf<CCLCMsg_Move>();
                    break;
                case (uint)CLC_Messages.ClcVoiceData:
                    var pb22 = data.GetAsProtobuf<CCLCMsg_VoiceData>();
                    break;
                case (uint)CLC_Messages.ClcBaselineAck:
                    var pb23 = data.GetAsProtobuf<CCLCMsg_BaselineAck>();
                    break;
                case (uint)CLC_Messages.ClcRespondCvarValue:
                    var pb25 = data.GetAsProtobuf<CCLCMsg_RespondCvarValue>();
                    break;
                case (uint)CLC_Messages.ClcFileCrccheck:
                    var pb26 = data.GetAsProtobuf<CCLCMsg_FileCRCCheck>();
                    break;
                case (uint)CLC_Messages.ClcLoadingProgress:
                    var pb27 = data.GetAsProtobuf<CCLCMsg_LoadingProgress>();
                    break;
                case (uint)CLC_Messages.ClcSplitPlayerConnect:
                    var pb28 = data.GetAsProtobuf<CCLCMsg_SplitPlayerConnect>();
                    break;
                case (uint)CLC_Messages.ClcSplitPlayerDisconnect:
                    var pb30 = data.GetAsProtobuf<CCLCMsg_SplitPlayerDisconnect>();
                    break;
                case (uint)CLC_Messages.ClcServerStatus:
                    var pb31 = data.GetAsProtobuf<CCLCMsg_ServerStatus>();
                    break;
                case (uint)CLC_Messages.ClcRequestPause:
                    var pb33 = data.GetAsProtobuf<CCLCMsg_RequestPause>();
                    break;
                case (uint)CLC_Messages.ClcCmdKeyValues:
                    var pb34 = data.GetAsProtobuf<CCLCMsg_CmdKeyValues>();
                    break;
                case (uint)CLC_Messages.ClcRconServerDetails:
                    var pb35 = data.GetAsProtobuf<CCLCMsg_RconServerDetails>();
                    break;
                case (uint)CLC_Messages.ClcHltvReplay:
                    var pb36 = data.GetAsProtobuf<CCLCMsg_HltvReplay>();
                    break;
                case (uint)CLC_Messages.ClcDiagnostic:
                    var pb37 = data.GetAsProtobuf<CCLCMsg_Diagnostic>();
                    break;

                case (uint)SVC_Messages.SvcServerInfo:
                    var pb40 = data.GetAsProtobuf<CSVCMsg_ServerInfo>();
                    break;
                case (uint)SVC_Messages.SvcFlattenedSerializer:
                    var pb41 = data.GetAsProtobuf<CSVCMsg_FlattenedSerializer>();
                    break;
                case (uint)SVC_Messages.SvcClassInfo:
                    var pb42 = data.GetAsProtobuf<CSVCMsg_ClassInfo>();
                    break;
                case (uint)SVC_Messages.SvcSetPause:
                    var pb43 = data.GetAsProtobuf<CSVCMsg_SetPause>();
                    break;
                case (uint)SVC_Messages.SvcCreateStringTable:
                    var pb44 = data.GetAsProtobuf<CSVCMsg_CreateStringTable>();
                    break;
                case (uint)SVC_Messages.SvcUpdateStringTable:
                    var pb45 = data.GetAsProtobuf<CSVCMsg_UpdateStringTable>();
                    break;
                case (uint)SVC_Messages.SvcVoiceInit:
                    var pb46 = data.GetAsProtobuf<CSVCMsg_VoiceInit>();
                    break;
                case (uint)SVC_Messages.SvcVoiceData:
                    var pb47 = data.GetAsProtobuf<CSVCMsg_VoiceData>();
                    break;
                case (uint)SVC_Messages.SvcPrint:
                    var pb48 = data.GetAsProtobuf<CSVCMsg_Print>();
                    break;
                case (uint)SVC_Messages.SvcSounds:
                    var pb49 = data.GetAsProtobuf<CSVCMsg_Sounds>();
                    break;
                case (uint)SVC_Messages.SvcSetView:
                    var pb50 = data.GetAsProtobuf<CSVCMsg_SetView>();
                    break;
                case (uint)SVC_Messages.SvcClearAllStringTables:
                    var pb51 = data.GetAsProtobuf<CSVCMsg_ClearAllStringTables>();
                    break;
                case (uint)SVC_Messages.SvcCmdKeyValues:
                    var pb52 = data.GetAsProtobuf<CSVCMsg_CmdKeyValues>();
                    break;
                case (uint)SVC_Messages.SvcBspdecal:
                    var pb53 = data.GetAsProtobuf<CSVCMsg_BSPDecal>();
                    break;
                case (uint)SVC_Messages.SvcSplitScreen:
                    var pb54 = data.GetAsProtobuf<CSVCMsg_SplitScreen>();
                    break;
                case (uint)SVC_Messages.SvcPacketEntities:
                    var pb55 = data.GetAsProtobuf<CSVCMsg_PacketEntities>();
                    break;
                case (uint)SVC_Messages.SvcPrefetch:
                    var pb56 = data.GetAsProtobuf<CSVCMsg_Prefetch>();
                    break;
                case (uint)SVC_Messages.SvcMenu:
                    var pb57 = data.GetAsProtobuf<CSVCMsg_Menu>();
                    break;
                case (uint)SVC_Messages.SvcGetCvarValue:
                    var pb58 = data.GetAsProtobuf<CSVCMsg_GetCvarValue>();
                    break;
                case (uint)SVC_Messages.SvcStopSound:
                    var pb59 = data.GetAsProtobuf<CSVCMsg_StopSound>();
                    break;
                case (uint)SVC_Messages.SvcPeerList:
                    var pb60 = data.GetAsProtobuf<CSVCMsg_PeerList>();
                    break;
                case (uint)SVC_Messages.SvcPacketReliable:
                    var pb61 = data.GetAsProtobuf<CSVCMsg_PacketReliable>();
                    break;
                case (uint)SVC_Messages.SvcHltvstatus:
                    var pb62 = data.GetAsProtobuf<CSVCMsg_HLTVStatus>();
                    break;
                case (uint)SVC_Messages.SvcServerSteamId:
                    var pb63 = data.GetAsProtobuf<CSVCMsg_ServerSteamID>();
                    break;
                case (uint)SVC_Messages.SvcFullFrameSplit:
                    var pb70 = data.GetAsProtobuf<CSVCMsg_FullFrameSplit>();
                    break;
                case (uint)SVC_Messages.SvcRconServerDetails:
                    var pb71 = data.GetAsProtobuf<CSVCMsg_RconServerDetails>();
                    break;
                case (uint)SVC_Messages.SvcUserMessage:
                    var pb72 = data.GetAsProtobuf<CSVCMsg_UserMessage>();
                    break;
                case (uint)SVC_Messages.SvcBroadcastCommand:
                    var pb74 = data.GetAsProtobuf<CSVCMsg_Broadcast_Command>();
                    break;
                case (uint)SVC_Messages.SvcHltvFixupOperatorStatus:
                    var pb75 = data.GetAsProtobuf<CSVCMsg_HltvFixupOperatorStatus>();
                    break;
                case (uint)SVC_Messages.SvcUserCmds:
                    var pb76 = data.GetAsProtobuf<CSVCMsg_UserCommands>();
                    break;

                // case (uint)EDotaUserMessages.DotaUmAddUnitToSelection:
                //     var pb464 = data.GetAsProtobuf<CDOTAUserMsg_AddUnitToSelection>();
                //     break;
                case (uint)EDotaUserMessages.DotaUmAidebugLine:
                    var pb465 = data.GetAsProtobuf<CDOTAUserMsg_AIDebugLine>();
                    break;
                case (uint)EDotaUserMessages.DotaUmChatEvent:
                    var pb466 = data.GetAsProtobuf<CDOTAUserMsg_ChatEvent>();
                    break;
                case (uint)EDotaUserMessages.DotaUmCombatHeroPositions:
                    var pb467 = data.GetAsProtobuf<CDOTAUserMsg_CombatHeroPositions>();
                    break;
                case (uint)EDotaUserMessages.DotaUmCombatLogData:
                    var pb468 = data.GetAsProtobuf<CMsgDOTACombatLogEntry>();
                    break;
                case (uint)EDotaUserMessages.DotaUmCombatLogBulkData:
                    var pb470 = data.GetAsProtobuf<CDOTAUserMsg_CombatLogBulkData>();
                    break;
                case (uint)EDotaUserMessages.DotaUmCreateLinearProjectile:
                    var pb471 = data.GetAsProtobuf<CDOTAUserMsg_CreateLinearProjectile>();
                    break;
                case (uint)EDotaUserMessages.DotaUmDestroyLinearProjectile:
                    var pb472 = data.GetAsProtobuf<CDOTAUserMsg_DestroyLinearProjectile>();
                    break;
                case (uint)EDotaUserMessages.DotaUmDodgeTrackingProjectiles:
                    var pb473 = data.GetAsProtobuf<CDOTAUserMsg_DodgeTrackingProjectiles>();
                    break;
                case (uint)EDotaUserMessages.DotaUmGlobalLightColor:
                    var pb474 = data.GetAsProtobuf<CDOTAUserMsg_GlobalLightColor>();
                    break;
                case (uint)EDotaUserMessages.DotaUmGlobalLightDirection:
                    var pb475 = data.GetAsProtobuf<CDOTAUserMsg_GlobalLightDirection>();
                    break;
                case (uint)EDotaUserMessages.DotaUmInvalidCommand:
                    var pb476 = data.GetAsProtobuf<CDOTAUserMsg_InvalidCommand>();
                    break;
                case (uint)EDotaUserMessages.DotaUmLocationPing:
                    var pb477 = data.GetAsProtobuf<CDOTAUserMsg_LocationPing>();
                    break;
                case (uint)EDotaUserMessages.DotaUmMapLine:
                    var pb478 = data.GetAsProtobuf<CDOTAUserMsg_MapLine>();
                    break;
                case (uint)EDotaUserMessages.DotaUmMiniKillCamInfo:
                    var pb479 = data.GetAsProtobuf<CDOTAUserMsg_MiniKillCamInfo>();
                    break;
                case (uint)EDotaUserMessages.DotaUmMinimapDebugPoint:
                    var pb480 = data.GetAsProtobuf<CDOTAUserMsg_MinimapDebugPoint>();
                    break;
                case (uint)EDotaUserMessages.DotaUmMinimapEvent:
                    var pb481 = data.GetAsProtobuf<CDOTAUserMsg_MinimapEvent>();
                    break;
                case (uint)EDotaUserMessages.DotaUmNevermoreRequiem:
                    var pb482 = data.GetAsProtobuf<CDOTAUserMsg_NevermoreRequiem>();
                    break;
                case (uint)EDotaUserMessages.DotaUmOverheadEvent:
                    var pb483 = data.GetAsProtobuf<CDOTAUserMsg_OverheadEvent>();
                    break;
                case (uint)EDotaUserMessages.DotaUmSetNextAutobuyItem:
                    var pb484 = data.GetAsProtobuf<CDOTAUserMsg_SetNextAutobuyItem>();
                    break;
                case (uint)EDotaUserMessages.DotaUmSharedCooldown:
                    var pb485 = data.GetAsProtobuf<CDOTAUserMsg_SharedCooldown>();
                    break;
                case (uint)EDotaUserMessages.DotaUmSpectatorPlayerClick:
                    var pb486 = data.GetAsProtobuf<CDOTAUserMsg_SpectatorPlayerClick>();
                    break;
                case (uint)EDotaUserMessages.DotaUmTutorialTipInfo:
                    var pb487 = data.GetAsProtobuf<CDOTAUserMsg_TutorialTipInfo>();
                    break;
                case (uint)EDotaUserMessages.DotaUmUnitEvent:
                    var pb488 = data.GetAsProtobuf<CDOTAUserMsg_UnitEvent>();
                    break;
                // case (uint)EDotaUserMessages.DotaUmParticleManager:
                //     var pb489 = data.GetAsProtobuf<CDOTAUserMsg_ParticleManager>();
                //     break;
                case (uint)EDotaUserMessages.DotaUmBotChat:
                    var pb490 = data.GetAsProtobuf<CDOTAUserMsg_BotChat>();
                    break;
                case (uint)EDotaUserMessages.DotaUmHudError:
                    var pb491 = data.GetAsProtobuf<CDOTAUserMsg_HudError>();
                    break;
                case (uint)EDotaUserMessages.DotaUmItemPurchased:
                    var pb492 = data.GetAsProtobuf<CDOTAUserMsg_ItemPurchased>();
                    break;
                case (uint)EDotaUserMessages.DotaUmPing:
                    var pb493 = data.GetAsProtobuf<CDOTAUserMsg_Ping>();
                    break;
                case (uint)EDotaUserMessages.DotaUmItemFound:
                    var pb494 = data.GetAsProtobuf<CDOTAUserMsg_ItemFound>();
                    break;
                // case (uint)EDotaUserMessages.DotaUmCharacterSpeakConcept:
                //     var pb495 = data.GetAsProtobuf<CDOTAUserMsg_CharacterSpeakConcept>();
                //     break;
                case (uint)EDotaUserMessages.DotaUmSwapVerify:
                    var pb496 = data.GetAsProtobuf<CDOTAUserMsg_SwapVerify>();
                    break;
                case (uint)EDotaUserMessages.DotaUmWorldLine:
                    var pb497 = data.GetAsProtobuf<CDOTAUserMsg_WorldLine>();
                    break;
                // case (uint)EDotaUserMessages.DotaUmTournamentDrop:
                //     var pb498 = data.GetAsProtobuf<CDOTAUserMsg_TournamentDrop>();
                //     break;
                case (uint)EDotaUserMessages.DotaUmItemAlert:
                    var pb499 = data.GetAsProtobuf<CDOTAUserMsg_ItemAlert>();
                    break;
                case (uint)EDotaUserMessages.DotaUmHalloweenDrops:
                    var pb500 = data.GetAsProtobuf<CDOTAUserMsg_HalloweenDrops>();
                    break;
                case (uint)EDotaUserMessages.DotaUmChatWheel:
                    var pb501 = data.GetAsProtobuf<CDOTAUserMsg_ChatWheel>();
                    break;
                case (uint)EDotaUserMessages.DotaUmReceivedXmasGift:
                    var pb502 = data.GetAsProtobuf<CDOTAUserMsg_ReceivedXmasGift>();
                    break;
                case (uint)EDotaUserMessages.DotaUmUpdateSharedContent:
                    var pb503 = data.GetAsProtobuf<CDOTAUserMsg_UpdateSharedContent>();
                    break;
                case (uint)EDotaUserMessages.DotaUmTutorialRequestExp:
                    var pb504 = data.GetAsProtobuf<CDOTAUserMsg_TutorialRequestExp>();
                    break;
                case (uint)EDotaUserMessages.DotaUmTutorialPingMinimap:
                    var pb505 = data.GetAsProtobuf<CDOTAUserMsg_TutorialPingMinimap>();
                    break;
                case (uint)EDotaUserMessages.DotaUmGamerulesStateChanged:
                    var pb506 = data.GetAsProtobuf<CDOTAUserMsg_GamerulesStateChanged>();
                    break;
                case (uint)EDotaUserMessages.DotaUmShowSurvey:
                    var pb507 = data.GetAsProtobuf<CDOTAUserMsg_ShowSurvey>();
                    break;
                case (uint)EDotaUserMessages.DotaUmTutorialFade:
                    var pb508 = data.GetAsProtobuf<CDOTAUserMsg_TutorialFade>();
                    break;
                case (uint)EDotaUserMessages.DotaUmAddQuestLogEntry:
                    var pb509 = data.GetAsProtobuf<CDOTAUserMsg_AddQuestLogEntry>();
                    break;
                case (uint)EDotaUserMessages.DotaUmSendStatPopup:
                    var pb510 = data.GetAsProtobuf<CDOTAUserMsg_SendStatPopup>();
                    break;
                case (uint)EDotaUserMessages.DotaUmTutorialFinish:
                    var pb511 = data.GetAsProtobuf<CDOTAUserMsg_TutorialFinish>();
                    break;
                case (uint)EDotaUserMessages.DotaUmSendRoshanPopup:
                    var pb512 = data.GetAsProtobuf<CDOTAUserMsg_SendRoshanPopup>();
                    break;
                case (uint)EDotaUserMessages.DotaUmSendGenericToolTip:
                    var pb513 = data.GetAsProtobuf<CDOTAUserMsg_SendGenericToolTip>();
                    break;
                case (uint)EDotaUserMessages.DotaUmSendFinalGold:
                    var pb514 = data.GetAsProtobuf<CDOTAUserMsg_SendFinalGold>();
                    break;
                case (uint)EDotaUserMessages.DotaUmCustomMsg:
                    var pb515 = data.GetAsProtobuf<CDOTAUserMsg_CustomMsg>();
                    break;
                case (uint)EDotaUserMessages.DotaUmCoachHudping:
                    var pb516 = data.GetAsProtobuf<CDOTAUserMsg_CoachHUDPing>();
                    break;
                case (uint)EDotaUserMessages.DotaUmClientLoadGridNav:
                    var pb517 = data.GetAsProtobuf<CDOTAUserMsg_ClientLoadGridNav>();
                    break;
                case (uint)EDotaUserMessages.DotaUmTeProjectile:
                    var pb518 = data.GetAsProtobuf<CDOTAUserMsg_TE_Projectile>();
                    break;
                case (uint)EDotaUserMessages.DotaUmTeProjectileLoc:
                    var pb519 = data.GetAsProtobuf<CDOTAUserMsg_TE_ProjectileLoc>();
                    break;
                case (uint)EDotaUserMessages.DotaUmTeDotaBloodImpact:
                    var pb520 = data.GetAsProtobuf<CDOTAUserMsg_TE_DotaBloodImpact>();
                    break;
                case (uint)EDotaUserMessages.DotaUmTeUnitAnimation:
                    var pb521 = data.GetAsProtobuf<CDOTAUserMsg_TE_UnitAnimation>();
                    break;
                case (uint)EDotaUserMessages.DotaUmTeUnitAnimationEnd:
                    var pb522 = data.GetAsProtobuf<CDOTAUserMsg_TE_UnitAnimationEnd>();
                    break;
                case (uint)EDotaUserMessages.DotaUmAbilityPing:
                    var pb523 = data.GetAsProtobuf<CDOTAUserMsg_AbilityPing>();
                    break;
                case (uint)EDotaUserMessages.DotaUmShowGenericPopup:
                    var pb524 = data.GetAsProtobuf<CDOTAUserMsg_ShowGenericPopup>();
                    break;
                case (uint)EDotaUserMessages.DotaUmVoteStart:
                    var pb525 = data.GetAsProtobuf<CDOTAUserMsg_VoteStart>();
                    break;
                case (uint)EDotaUserMessages.DotaUmVoteUpdate:
                    var pb526 = data.GetAsProtobuf<CDOTAUserMsg_VoteUpdate>();
                    break;
                case (uint)EDotaUserMessages.DotaUmVoteEnd:
                    var pb527 = data.GetAsProtobuf<CDOTAUserMsg_VoteEnd>();
                    break;
                case (uint)EDotaUserMessages.DotaUmBoosterState:
                    var pb528 = data.GetAsProtobuf<CDOTAUserMsg_BoosterState>();
                    break;
                case (uint)EDotaUserMessages.DotaUmWillPurchaseAlert:
                    var pb529 = data.GetAsProtobuf<CDOTAUserMsg_WillPurchaseAlert>();
                    break;
                case (uint)EDotaUserMessages.DotaUmTutorialMinimapPosition:
                    var pb530 = data.GetAsProtobuf<CDOTAUserMsg_TutorialMinimapPosition>();
                    break;
                case (uint)EDotaUserMessages.DotaUmAbilitySteal:
                    var pb532 = data.GetAsProtobuf<CDOTAUserMsg_AbilitySteal>();
                    break;
                case (uint)EDotaUserMessages.DotaUmCourierKilledAlert:
                    var pb533 = data.GetAsProtobuf<CDOTAUserMsg_CourierKilledAlert>();
                    break;
                case (uint)EDotaUserMessages.DotaUmEnemyItemAlert:
                    var pb534 = data.GetAsProtobuf<CDOTAUserMsg_EnemyItemAlert>();
                    break;
                case (uint)EDotaUserMessages.DotaUmStatsMatchDetails:
                    var pb535 = data.GetAsProtobuf<CDOTAUserMsg_StatsMatchDetails>();
                    break;
                case (uint)EDotaUserMessages.DotaUmMiniTaunt:
                    var pb536 = data.GetAsProtobuf<CDOTAUserMsg_MiniTaunt>();
                    break;
                case (uint)EDotaUserMessages.DotaUmBuyBackStateAlert:
                    var pb537 = data.GetAsProtobuf<CDOTAUserMsg_BuyBackStateAlert>();
                    break;
                case (uint)EDotaUserMessages.DotaUmSpeechBubble:
                    var pb538 = data.GetAsProtobuf<CDOTAUserMsg_SpeechBubble>();
                    break;
                case (uint)EDotaUserMessages.DotaUmCustomHeaderMessage:
                    var pb539 = data.GetAsProtobuf<CDOTAUserMsg_CustomHeaderMessage>();
                    break;
                case (uint)EDotaUserMessages.DotaUmQuickBuyAlert:
                    var pb540 = data.GetAsProtobuf<CDOTAUserMsg_QuickBuyAlert>();
                    break;
                case (uint)EDotaUserMessages.DotaUmStatsHeroDetails:
                    var pb541 = data.GetAsProtobuf<CDOTAUserMsg_StatsHeroMinuteDetails>();
                    break;
                case (uint)EDotaUserMessages.DotaUmPredictionResult:
                    var pb542 = data.GetAsProtobuf<CDotaMsg_PredictionResult>();
                    break;
                case (uint)EDotaUserMessages.DotaUmModifierAlert:
                    var pb543 = data.GetAsProtobuf<CDOTAUserMsg_ModifierAlert>();
                    break;
                case (uint)EDotaUserMessages.DotaUmHpmanaAlert:
                    var pb544 = data.GetAsProtobuf<CDOTAUserMsg_HPManaAlert>();
                    break;
                case (uint)EDotaUserMessages.DotaUmGlyphAlert:
                    var pb545 = data.GetAsProtobuf<CDOTAUserMsg_GlyphAlert>();
                    break;
                case (uint)EDotaUserMessages.DotaUmBeastChat:
                    var pb546 = data.GetAsProtobuf<CDOTAUserMsg_BeastChat>();
                    break;
                case (uint)EDotaUserMessages.DotaUmSpectatorPlayerUnitOrders:
                    var pb547 = data.GetAsProtobuf<CDOTAUserMsg_SpectatorPlayerUnitOrders>();
                    break;
                case (uint)EDotaUserMessages.DotaUmCustomHudElementCreate:
                    var pb548 = data.GetAsProtobuf<CDOTAUserMsg_CustomHudElement_Create>();
                    break;
                case (uint)EDotaUserMessages.DotaUmCustomHudElementModify:
                    var pb549 = data.GetAsProtobuf<CDOTAUserMsg_CustomHudElement_Modify>();
                    break;
                case (uint)EDotaUserMessages.DotaUmCustomHudElementDestroy:
                    var pb550 = data.GetAsProtobuf<CDOTAUserMsg_CustomHudElement_Destroy>();
                    break;
                case (uint)EDotaUserMessages.DotaUmCompendiumState:
                    var pb551 = data.GetAsProtobuf<CDOTAUserMsg_CompendiumState>();
                    break;
                case (uint)EDotaUserMessages.DotaUmProjectionAbility:
                    var pb552 = data.GetAsProtobuf<CDOTAUserMsg_ProjectionAbility>();
                    break;
                case (uint)EDotaUserMessages.DotaUmProjectionEvent:
                    var pb553 = data.GetAsProtobuf<CDOTAUserMsg_ProjectionEvent>();
                    break;
                case (uint)EDotaUserMessages.DotaUmCombatLogDataHltv:
                    var pb554 = data.GetAsProtobuf<CMsgDOTACombatLogEntry>();
                    break;
                case (uint)EDotaUserMessages.DotaUmXpalert:
                    var pb555 = data.GetAsProtobuf<CDOTAUserMsg_XPAlert>();
                    break;
                case (uint)EDotaUserMessages.DotaUmUpdateQuestProgress:
                    var pb556 = data.GetAsProtobuf<CDOTAUserMsg_UpdateQuestProgress>();
                    break;
                // case (uint)EDotaUserMessages.DotaUmMatchMetadata:
                //     var pb557 = data.GetAsProtobuf<CDOTAUserMsg_MatchMetadata>();
                //     break;
                case (uint)EDotaUserMessages.DotaUmMatchDetails:
                    var pb558 = data.GetAsProtobuf<CDOTAUserMsg_StatsMatchDetails>();
                    break;
                case (uint)EDotaUserMessages.DotaUmQuestStatus:
                    var pb559 = data.GetAsProtobuf<CDOTAUserMsg_QuestStatus>();
                    break;
                case (uint)EDotaUserMessages.DotaUmSuggestHeroPick:
                    var pb560 = data.GetAsProtobuf<CDOTAUserMsg_SuggestHeroPick>();
                    break;
                case (uint)EDotaUserMessages.DotaUmSuggestHeroRole:
                    var pb561 = data.GetAsProtobuf<CDOTAUserMsg_SuggestHeroRole>();
                    break;
                case (uint)EDotaUserMessages.DotaUmKillcamDamageTaken:
                    var pb562 = data.GetAsProtobuf<CDOTAUserMsg_KillcamDamageTaken>();
                    break;
                case (uint)EDotaUserMessages.DotaUmSelectPenaltyGold:
                    var pb563 = data.GetAsProtobuf<CDOTAUserMsg_SelectPenaltyGold>();
                    break;
                case (uint)EDotaUserMessages.DotaUmRollDiceResult:
                    var pb564 = data.GetAsProtobuf<CDOTAUserMsg_RollDiceResult>();
                    break;
                case (uint)EDotaUserMessages.DotaUmFlipCoinResult:
                    var pb565 = data.GetAsProtobuf<CDOTAUserMsg_FlipCoinResult>();
                    break;
                case (uint)EDotaUserMessages.DotaUmRequestItemSuggestions:
                    var pb566 = data.GetAsProtobuf<CDOTAUserMessage_RequestItemSuggestions>();
                    break;
                case (uint)EDotaUserMessages.DotaUmTeamCaptainChanged:
                    var pb567 = data.GetAsProtobuf<CDOTAUserMessage_TeamCaptainChanged>();
                    break;
                case (uint)EDotaUserMessages.DotaUmSendRoshanSpectatorPhase:
                    var pb568 = data.GetAsProtobuf<CDOTAUserMsg_SendRoshanSpectatorPhase>();
                    break;
                case (uint)EDotaUserMessages.DotaUmChatWheelCooldown:
                    var pb569 = data.GetAsProtobuf<CDOTAUserMsg_ChatWheelCooldown>();
                    break;
                case (uint)EDotaUserMessages.DotaUmDismissAllStatPopups:
                    var pb570 = data.GetAsProtobuf<CDOTAUserMsg_DismissAllStatPopups>();
                    break;
                case (uint)EDotaUserMessages.DotaUmTeDestroyProjectile:
                    var pb571 = data.GetAsProtobuf<CDOTAUserMsg_TE_DestroyProjectile>();
                    break;
                case (uint)EDotaUserMessages.DotaUmHeroRelicProgress:
                    var pb572 = data.GetAsProtobuf<CDOTAUserMsg_HeroRelicProgress>();
                    break;
                case (uint)EDotaUserMessages.DotaUmAbilityDraftRequestAbility:
                    var pb573 = data.GetAsProtobuf<CDOTAUserMsg_AbilityDraftRequestAbility>();
                    break;
                case (uint)EDotaUserMessages.DotaUmItemSold:
                    var pb574 = data.GetAsProtobuf<CDOTAUserMsg_ItemSold>();
                    break;
                case (uint)EDotaUserMessages.DotaUmDamageReport:
                    var pb575 = data.GetAsProtobuf<CDOTAUserMsg_DamageReport>();
                    break;
                case (uint)EDotaUserMessages.DotaUmSalutePlayer:
                    var pb576 = data.GetAsProtobuf<CDOTAUserMsg_SalutePlayer>();
                    break;
                case (uint)EDotaUserMessages.DotaUmTipAlert:
                    var pb577 = data.GetAsProtobuf<CDOTAUserMsg_TipAlert>();
                    break;
                case (uint)EDotaUserMessages.DotaUmReplaceQueryUnit:
                    var pb578 = data.GetAsProtobuf<CDOTAUserMsg_ReplaceQueryUnit>();
                    break;
                case (uint)EDotaUserMessages.DotaUmEmptyTeleportAlert:
                    var pb579 = data.GetAsProtobuf<CDOTAUserMsg_EmptyTeleportAlert>();
                    break;
                case (uint)EDotaUserMessages.DotaUmMarsArenaOfBloodAttack:
                    var pb580 = data.GetAsProtobuf<CDOTAUserMsg_MarsArenaOfBloodAttack>();
                    break;
                case (uint)EDotaUserMessages.DotaUmEsarcanaCombo:
                    var pb581 = data.GetAsProtobuf<CDOTAUserMsg_ESArcanaCombo>();
                    break;
                case (uint)EDotaUserMessages.DotaUmEsarcanaComboSummary:
                    var pb582 = data.GetAsProtobuf<CDOTAUserMsg_ESArcanaComboSummary>();
                    break;
                case (uint)EDotaUserMessages.DotaUmHighFiveLeftHanging:
                    var pb583 = data.GetAsProtobuf<CDOTAUserMsg_HighFiveLeftHanging>();
                    break;
                case (uint)EDotaUserMessages.DotaUmHighFiveCompleted:
                    var pb584 = data.GetAsProtobuf<CDOTAUserMsg_HighFiveCompleted>();
                    break;
                case (uint)EDotaUserMessages.DotaUmShovelUnearth:
                    var pb585 = data.GetAsProtobuf<CDOTAUserMsg_ShovelUnearth>();
                    break;
                case (uint)EDotaUserMessages.DotaUmRadarAlert:
                    var pb587 = data.GetAsProtobuf<CDOTAUserMsg_RadarAlert>();
                    break;
                case (uint)EDotaUserMessages.DotaUmAllStarEvent:
                    var pb588 = data.GetAsProtobuf<CDOTAUserMsg_AllStarEvent>();
                    break;
                case (uint)EDotaUserMessages.DotaUmTalentTreeAlert:
                    var pb589 = data.GetAsProtobuf<CDOTAUserMsg_TalentTreeAlert>();
                    break;
                case (uint)EDotaUserMessages.DotaUmQueuedOrderRemoved:
                    var pb590 = data.GetAsProtobuf<CDOTAUserMsg_QueuedOrderRemoved>();
                    break;
                case (uint)EDotaUserMessages.DotaUmDebugChallenge:
                    var pb591 = data.GetAsProtobuf<CDOTAUserMsg_DebugChallenge>();
                    break;
                case (uint)EDotaUserMessages.DotaUmOmarcanaCombo:
                    var pb592 = data.GetAsProtobuf<CDOTAUserMsg_OMArcanaCombo>();
                    break;
                case (uint)EDotaUserMessages.DotaUmFoundNeutralItem:
                    var pb593 = data.GetAsProtobuf<CDOTAUserMsg_FoundNeutralItem>();
                    break;
                case (uint)EDotaUserMessages.DotaUmOutpostCaptured:
                    var pb594 = data.GetAsProtobuf<CDOTAUserMsg_OutpostCaptured>();
                    break;
                case (uint)EDotaUserMessages.DotaUmOutpostGrantedXp:
                    var pb595 = data.GetAsProtobuf<CDOTAUserMsg_OutpostGrantedXP>();
                    break;
                case (uint)EDotaUserMessages.DotaUmMoveCameraToUnit:
                    var pb596 = data.GetAsProtobuf<CDOTAUserMsg_MoveCameraToUnit>();
                    break;
                case (uint)EDotaUserMessages.DotaUmPauseMinigameData:
                    var pb597 = data.GetAsProtobuf<CDOTAUserMsg_PauseMinigameData>();
                    break;
                case (uint)EDotaUserMessages.DotaUmVersusScenePlayerBehavior:
                    var pb598 = data.GetAsProtobuf<CDOTAUserMsg_VersusScene_PlayerBehavior>();
                    break;
                case (uint)EDotaUserMessages.DotaUmQoPArcanaSummary:
                    var pb600 = data.GetAsProtobuf<CDOTAUserMsg_QoP_ArcanaSummary>();
                    break;
                case (uint)EDotaUserMessages.DotaUmHotPotatoCreated:
                    var pb601 = data.GetAsProtobuf<CDOTAUserMsg_HotPotato_Created>();
                    break;
                case (uint)EDotaUserMessages.DotaUmHotPotatoExploded:
                    var pb602 = data.GetAsProtobuf<CDOTAUserMsg_HotPotato_Exploded>();
                    break;
                case (uint)EDotaUserMessages.DotaUmWkArcanaProgress:
                    var pb603 = data.GetAsProtobuf<CDOTAUserMsg_WK_Arcana_Progress>();
                    break;
                case (uint)EDotaUserMessages.DotaUmGuildChallengeProgress:
                    var pb604 = data.GetAsProtobuf<CDOTAUserMsg_GuildChallenge_Progress>();
                    break;
                case (uint)EDotaUserMessages.DotaUmWrarcanaProgress:
                    var pb605 = data.GetAsProtobuf<CDOTAUserMsg_WRArcanaProgress>();
                    break;
                case (uint)EDotaUserMessages.DotaUmWrarcanaSummary:
                    var pb606 = data.GetAsProtobuf<CDOTAUserMsg_WRArcanaSummary>();
                    break;
                case (uint)EDotaUserMessages.DotaUmEmptyItemSlotAlert:
                    var pb607 = data.GetAsProtobuf<CDOTAUserMsg_EmptyItemSlotAlert>();
                    break;
                case (uint)EDotaUserMessages.DotaUmAghsStatusAlert:
                    var pb608 = data.GetAsProtobuf<CDOTAUserMsg_AghsStatusAlert>();
                    break;
                case (uint)EDotaUserMessages.DotaUmPingConfirmation:
                    var pb609 = data.GetAsProtobuf<CDOTAUserMsg_PingConfirmation>();
                    break;
                case (uint)EDotaUserMessages.DotaUmMutedPlayers:
                    var pb610 = data.GetAsProtobuf<CDOTAUserMsg_MutedPlayers>();
                    break;
                case (uint)EDotaUserMessages.DotaUmContextualTip:
                    var pb611 = data.GetAsProtobuf<CDOTAUserMsg_ContextualTip>();
                    break;
                case (uint)EDotaUserMessages.DotaUmChatMessage:
                    var pb612 = data.GetAsProtobuf<CDOTAUserMsg_ChatMessage>();
                    break;
                case (uint)EDotaUserMessages.DotaUmNeutralCampAlert:
                    var pb613 = data.GetAsProtobuf<CDOTAUserMsg_NeutralCampAlert>();
                    break;
                case (uint)EDotaUserMessages.DotaUmRockPaperScissorsStarted:
                    var pb614 = data.GetAsProtobuf<CDOTAUserMsg_RockPaperScissorsStarted>();
                    break;
                case (uint)EDotaUserMessages.DotaUmRockPaperScissorsFinished:
                    var pb615 = data.GetAsProtobuf<CDOTAUserMsg_RockPaperScissorsFinished>();
                    break;
                case (uint)EDotaUserMessages.DotaUmDuelOpponentKilled:
                    var pb616 = data.GetAsProtobuf<CDOTAUserMsg_DuelOpponentKilled>();
                    break;
                case (uint)EDotaUserMessages.DotaUmDuelAccepted:
                    var pb617 = data.GetAsProtobuf<CDOTAUserMsg_DuelAccepted>();
                    break;
                case (uint)EDotaUserMessages.DotaUmDuelRequested:
                    var pb618 = data.GetAsProtobuf<CDOTAUserMsg_DuelRequested>();
                    break;
                case (uint)EDotaUserMessages.DotaUmMuertaReleaseEventAssignedTargetKilled:
                    var pb619 = data.GetAsProtobuf<CDOTAUserMsg_MuertaReleaseEvent_AssignedTargetKilled>();
                    break;
                case (uint)EDotaUserMessages.DotaUmPlayerDraftSuggestPick:
                    var pb620 = data.GetAsProtobuf<CDOTAUserMsg_PlayerDraftSuggestPick>();
                    break;
                case (uint)EDotaUserMessages.DotaUmPlayerDraftPick:
                    var pb621 = data.GetAsProtobuf<CDOTAUserMsg_PlayerDraftPick>();
                    break;
                case (uint)EDotaUserMessages.DotaUmUpdateLinearProjectileCpdata:
                    var pb622 = data.GetAsProtobuf<CDOTAUserMsg_UpdateLinearProjectileCPData>();
                    break;
                case (uint)EDotaUserMessages.DotaUmGiftPlayer:
                    var pb623 = data.GetAsProtobuf<CDOTAUserMsg_GiftPlayer>();
                    break;
                case (uint)EDotaUserMessages.DotaUmFacetPing:
                    var pb624 = data.GetAsProtobuf<CDOTAUserMsg_FacetPing>();
                    break;
                case (uint)EDotaUserMessages.DotaUmInnatePing:
                    var pb625 = data.GetAsProtobuf<CDOTAUserMsg_InnatePing>();
                    break;
                case (uint)EDotaUserMessages.DotaUmRoshanTimer:
                    var pb626 = data.GetAsProtobuf<CDOTAUserMsg_RoshanTimer>();
                    break;
                case (uint)EDotaUserMessages.DotaUmNeutralCraftAvailable:
                    var pb627 = data.GetAsProtobuf<CDOTAUserMsg_NeutralCraftAvailable>();
                    break;
                case (uint)EDotaUserMessages.DotaUmTimerAlert:
                    var pb628 = data.GetAsProtobuf<CDOTAUserMsg_TimerAlert>();
                    break;
                case (uint)EDotaUserMessages.DotaUmMadstoneAlert:
                    var pb629 = data.GetAsProtobuf<CDOTAUserMsg_MadstoneAlert>();
                    break;
                case (uint)EDotaUserMessages.DotaUmCourierLeftFountainAlert:
                    var pb630 = data.GetAsProtobuf<CDOTAUserMsg_CourierLeftFountainAlert>();
                    break;
                default:
                    break;
            }
        }
    }

    public class MessageDataProtobufsCallbacksParser
    {
        private Source2ReplayFrameCallbacks? _dellies;

        public MessageDataProtobufsCallbacksParser()
        {
        }

        public static async Task RunAsync(Stream stream)
        {
            var fella = new MessageDataProtobufsCallbacksParser();

            await Replay.ParseFileAsync(stream, fella.RunTheReplay);
        }

        private void RunTheReplay(Replay.File file)
        {
            var callbacks = new Source2ReplayFrameCallbacks(OnDemFileHeader);
            _dellies = callbacks;
            file.ParseAsSource2Replay(callbacks);
        }

        private void OnDemFileHeader(Source2Replay.FrameData data)
        {
            var fh = data.GetAsProtobuf<CDemoFileHeader>();

            var frameCallbacks = _dellies;
            Debug.Assert(frameCallbacks is not null);
            var builder = new Source2ReplayMessageCallbacks.Builder();
            var callbacks = builder.Callbacks;

            callbacks.Add((uint)NET_Messages.NetNop, OnNetNop);
            callbacks.Add((uint)NET_Messages.NetSplitScreenUser, OnNetSplitScreenUser);
            callbacks.Add((uint)NET_Messages.NetTick, OnNetTick);
            callbacks.Add((uint)NET_Messages.NetStringCmd, OnNetStringCmd);
            callbacks.Add((uint)NET_Messages.NetSetConVar, OnNetSetConVar);
            callbacks.Add((uint)NET_Messages.NetSignonState, OnNetSignonState);
            callbacks.Add((uint)NET_Messages.NetSpawnGroupLoad, OnNetSpawnGroupLoad);
            callbacks.Add((uint)NET_Messages.NetSpawnGroupManifestUpdate, OnNetSpawnGroupManifestUpdate);
            callbacks.Add((uint)NET_Messages.NetSpawnGroupSetCreationTick, OnNetSpawnGroupSetCreationTick);
            callbacks.Add((uint)NET_Messages.NetSpawnGroupUnload, OnNetSpawnGroupUnload);
            callbacks.Add((uint)NET_Messages.NetSpawnGroupLoadCompleted, OnNetSpawnGroupLoadCompleted);
            callbacks.Add((uint)NET_Messages.NetDebugOverlay, OnNetDebugOverlay);
            callbacks.Add((uint)CLC_Messages.ClcClientInfo, OnClcClientInfo);
            callbacks.Add((uint)CLC_Messages.ClcMove, OnClcMove);
            callbacks.Add((uint)CLC_Messages.ClcVoiceData, OnClcVoiceData);
            callbacks.Add((uint)CLC_Messages.ClcBaselineAck, OnClcBaselineAck);
            callbacks.Add((uint)CLC_Messages.ClcRespondCvarValue, OnClcRespondCvarValue);
            callbacks.Add((uint)CLC_Messages.ClcFileCrccheck, OnClcFileCrccheck);
            callbacks.Add((uint)CLC_Messages.ClcLoadingProgress, OnClcLoadingProgress);
            callbacks.Add((uint)CLC_Messages.ClcSplitPlayerConnect, OnClcSplitPlayerConnect);
            callbacks.Add((uint)CLC_Messages.ClcSplitPlayerDisconnect, OnClcSplitPlayerDisconnect);
            callbacks.Add((uint)CLC_Messages.ClcServerStatus, OnClcServerStatus);
            callbacks.Add((uint)CLC_Messages.ClcRequestPause, OnClcRequestPause);
            callbacks.Add((uint)CLC_Messages.ClcCmdKeyValues, OnClcCmdKeyValues);
            callbacks.Add((uint)CLC_Messages.ClcRconServerDetails, OnClcRconServerDetails);
            callbacks.Add((uint)CLC_Messages.ClcHltvReplay, OnClcHltvReplay);
            callbacks.Add((uint)CLC_Messages.ClcDiagnostic, OnClcDiagnostic);
            callbacks.Add((uint)SVC_Messages.SvcServerInfo, OnSvcServerInfo);
            callbacks.Add((uint)SVC_Messages.SvcFlattenedSerializer, OnSvcFlattenedSerializer);
            callbacks.Add((uint)SVC_Messages.SvcClassInfo, OnSvcClassInfo);
            callbacks.Add((uint)SVC_Messages.SvcSetPause, OnSvcSetPause);
            callbacks.Add((uint)SVC_Messages.SvcCreateStringTable, OnSvcCreateStringTable);
            callbacks.Add((uint)SVC_Messages.SvcUpdateStringTable, OnSvcUpdateStringTable);
            callbacks.Add((uint)SVC_Messages.SvcVoiceInit, OnSvcVoiceInit);
            callbacks.Add((uint)SVC_Messages.SvcVoiceData, OnSvcVoiceData);
            callbacks.Add((uint)SVC_Messages.SvcPrint, OnSvcPrint);
            callbacks.Add((uint)SVC_Messages.SvcSounds, OnSvcSounds);
            callbacks.Add((uint)SVC_Messages.SvcSetView, OnSvcSetView);
            callbacks.Add((uint)SVC_Messages.SvcClearAllStringTables, OnSvcClearAllStringTables);
            callbacks.Add((uint)SVC_Messages.SvcCmdKeyValues, OnSvcCmdKeyValues);
            callbacks.Add((uint)SVC_Messages.SvcBspdecal, OnSvcBspdecal);
            callbacks.Add((uint)SVC_Messages.SvcSplitScreen, OnSvcSplitScreen);
            callbacks.Add((uint)SVC_Messages.SvcPacketEntities, OnSvcPacketEntities);
            callbacks.Add((uint)SVC_Messages.SvcPrefetch, OnSvcPrefetch);
            callbacks.Add((uint)SVC_Messages.SvcMenu, OnSvcMenu);
            callbacks.Add((uint)SVC_Messages.SvcGetCvarValue, OnSvcGetCvarValue);
            callbacks.Add((uint)SVC_Messages.SvcStopSound, OnSvcStopSound);
            callbacks.Add((uint)SVC_Messages.SvcPeerList, OnSvcPeerList);
            callbacks.Add((uint)SVC_Messages.SvcPacketReliable, OnSvcPacketReliable);
            callbacks.Add((uint)SVC_Messages.SvcHltvstatus, OnSvcHltvstatus);
            callbacks.Add((uint)SVC_Messages.SvcServerSteamId, OnSvcServerSteamId);
            callbacks.Add((uint)SVC_Messages.SvcFullFrameSplit, OnSvcFullFrameSplit);
            callbacks.Add((uint)SVC_Messages.SvcRconServerDetails, OnSvcRconServerDetails);
            callbacks.Add((uint)SVC_Messages.SvcUserMessage, OnSvcUserMessage);
            callbacks.Add((uint)SVC_Messages.SvcBroadcastCommand, OnSvcBroadcastCommand);
            callbacks.Add((uint)SVC_Messages.SvcHltvFixupOperatorStatus, OnSvcHltvFixupOperatorStatus);
            callbacks.Add((uint)SVC_Messages.SvcUserCmds, OnSvcUserCmds);
            callbacks.Add((uint)EDotaUserMessages.DotaUmAidebugLine, OnDotaUmAidebugLine);
            callbacks.Add((uint)EDotaUserMessages.DotaUmChatEvent, OnDotaUmChatEvent);
            callbacks.Add((uint)EDotaUserMessages.DotaUmCombatHeroPositions, OnDotaUmCombatHeroPositions);
            callbacks.Add((uint)EDotaUserMessages.DotaUmCombatLogData, OnDotaUmCombatLogData);
            callbacks.Add((uint)EDotaUserMessages.DotaUmCombatLogBulkData, OnDotaUmCombatLogBulkData);
            callbacks.Add((uint)EDotaUserMessages.DotaUmCreateLinearProjectile, OnDotaUmCreateLinearProjectile);
            callbacks.Add((uint)EDotaUserMessages.DotaUmDestroyLinearProjectile, OnDotaUmDestroyLinearProjectile);
            callbacks.Add((uint)EDotaUserMessages.DotaUmDodgeTrackingProjectiles, OnDotaUmDodgeTrackingProjectiles);
            callbacks.Add((uint)EDotaUserMessages.DotaUmGlobalLightColor, OnDotaUmGlobalLightColor);
            callbacks.Add((uint)EDotaUserMessages.DotaUmGlobalLightDirection, OnDotaUmGlobalLightDirection);
            callbacks.Add((uint)EDotaUserMessages.DotaUmInvalidCommand, OnDotaUmInvalidCommand);
            callbacks.Add((uint)EDotaUserMessages.DotaUmLocationPing, OnDotaUmLocationPing);
            callbacks.Add((uint)EDotaUserMessages.DotaUmMapLine, OnDotaUmMapLine);
            callbacks.Add((uint)EDotaUserMessages.DotaUmMiniKillCamInfo, OnDotaUmMiniKillCamInfo);
            callbacks.Add((uint)EDotaUserMessages.DotaUmMinimapDebugPoint, OnDotaUmMinimapDebugPoint);
            callbacks.Add((uint)EDotaUserMessages.DotaUmMinimapEvent, OnDotaUmMinimapEvent);
            callbacks.Add((uint)EDotaUserMessages.DotaUmNevermoreRequiem, OnDotaUmNevermoreRequiem);
            callbacks.Add((uint)EDotaUserMessages.DotaUmOverheadEvent, OnDotaUmOverheadEvent);
            callbacks.Add((uint)EDotaUserMessages.DotaUmSetNextAutobuyItem, OnDotaUmSetNextAutobuyItem);
            callbacks.Add((uint)EDotaUserMessages.DotaUmSharedCooldown, OnDotaUmSharedCooldown);
            callbacks.Add((uint)EDotaUserMessages.DotaUmSpectatorPlayerClick, OnDotaUmSpectatorPlayerClick);
            callbacks.Add((uint)EDotaUserMessages.DotaUmTutorialTipInfo, OnDotaUmTutorialTipInfo);
            callbacks.Add((uint)EDotaUserMessages.DotaUmUnitEvent, OnDotaUmUnitEvent);
            callbacks.Add((uint)EDotaUserMessages.DotaUmBotChat, OnDotaUmBotChat);
            callbacks.Add((uint)EDotaUserMessages.DotaUmHudError, OnDotaUmHudError);
            callbacks.Add((uint)EDotaUserMessages.DotaUmItemPurchased, OnDotaUmItemPurchased);
            callbacks.Add((uint)EDotaUserMessages.DotaUmPing, OnDotaUmPing);
            callbacks.Add((uint)EDotaUserMessages.DotaUmItemFound, OnDotaUmItemFound);
            callbacks.Add((uint)EDotaUserMessages.DotaUmSwapVerify, OnDotaUmSwapVerify);
            callbacks.Add((uint)EDotaUserMessages.DotaUmWorldLine, OnDotaUmWorldLine);
            callbacks.Add((uint)EDotaUserMessages.DotaUmItemAlert, OnDotaUmItemAlert);
            callbacks.Add((uint)EDotaUserMessages.DotaUmHalloweenDrops, OnDotaUmHalloweenDrops);
            callbacks.Add((uint)EDotaUserMessages.DotaUmChatWheel, OnDotaUmChatWheel);
            callbacks.Add((uint)EDotaUserMessages.DotaUmReceivedXmasGift, OnDotaUmReceivedXmasGift);
            callbacks.Add((uint)EDotaUserMessages.DotaUmUpdateSharedContent, OnDotaUmUpdateSharedContent);
            callbacks.Add((uint)EDotaUserMessages.DotaUmTutorialRequestExp, OnDotaUmTutorialRequestExp);
            callbacks.Add((uint)EDotaUserMessages.DotaUmTutorialPingMinimap, OnDotaUmTutorialPingMinimap);
            callbacks.Add((uint)EDotaUserMessages.DotaUmGamerulesStateChanged, OnDotaUmGamerulesStateChanged);
            callbacks.Add((uint)EDotaUserMessages.DotaUmShowSurvey, OnDotaUmShowSurvey);
            callbacks.Add((uint)EDotaUserMessages.DotaUmTutorialFade, OnDotaUmTutorialFade);
            callbacks.Add((uint)EDotaUserMessages.DotaUmAddQuestLogEntry, OnDotaUmAddQuestLogEntry);
            callbacks.Add((uint)EDotaUserMessages.DotaUmSendStatPopup, OnDotaUmSendStatPopup);
            callbacks.Add((uint)EDotaUserMessages.DotaUmTutorialFinish, OnDotaUmTutorialFinish);
            callbacks.Add((uint)EDotaUserMessages.DotaUmSendRoshanPopup, OnDotaUmSendRoshanPopup);
            callbacks.Add((uint)EDotaUserMessages.DotaUmSendGenericToolTip, OnDotaUmSendGenericToolTip);
            callbacks.Add((uint)EDotaUserMessages.DotaUmSendFinalGold, OnDotaUmSendFinalGold);
            callbacks.Add((uint)EDotaUserMessages.DotaUmCustomMsg, OnDotaUmCustomMsg);
            callbacks.Add((uint)EDotaUserMessages.DotaUmCoachHudping, OnDotaUmCoachHudping);
            callbacks.Add((uint)EDotaUserMessages.DotaUmClientLoadGridNav, OnDotaUmClientLoadGridNav);
            callbacks.Add((uint)EDotaUserMessages.DotaUmTeProjectile, OnDotaUmTeProjectile);
            callbacks.Add((uint)EDotaUserMessages.DotaUmTeProjectileLoc, OnDotaUmTeProjectileLoc);
            callbacks.Add((uint)EDotaUserMessages.DotaUmTeDotaBloodImpact, OnDotaUmTeDotaBloodImpact);
            callbacks.Add((uint)EDotaUserMessages.DotaUmTeUnitAnimation, OnDotaUmTeUnitAnimation);
            callbacks.Add((uint)EDotaUserMessages.DotaUmTeUnitAnimationEnd, OnDotaUmTeUnitAnimationEnd);
            callbacks.Add((uint)EDotaUserMessages.DotaUmAbilityPing, OnDotaUmAbilityPing);
            callbacks.Add((uint)EDotaUserMessages.DotaUmShowGenericPopup, OnDotaUmShowGenericPopup);
            callbacks.Add((uint)EDotaUserMessages.DotaUmVoteStart, OnDotaUmVoteStart);
            callbacks.Add((uint)EDotaUserMessages.DotaUmVoteUpdate, OnDotaUmVoteUpdate);
            callbacks.Add((uint)EDotaUserMessages.DotaUmVoteEnd, OnDotaUmVoteEnd);
            callbacks.Add((uint)EDotaUserMessages.DotaUmBoosterState, OnDotaUmBoosterState);
            callbacks.Add((uint)EDotaUserMessages.DotaUmWillPurchaseAlert, OnDotaUmWillPurchaseAlert);
            callbacks.Add((uint)EDotaUserMessages.DotaUmTutorialMinimapPosition, OnDotaUmTutorialMinimapPosition);
            callbacks.Add((uint)EDotaUserMessages.DotaUmAbilitySteal, OnDotaUmAbilitySteal);
            callbacks.Add((uint)EDotaUserMessages.DotaUmCourierKilledAlert, OnDotaUmCourierKilledAlert);
            callbacks.Add((uint)EDotaUserMessages.DotaUmEnemyItemAlert, OnDotaUmEnemyItemAlert);
            callbacks.Add((uint)EDotaUserMessages.DotaUmStatsMatchDetails, OnDotaUmStatsMatchDetails);
            callbacks.Add((uint)EDotaUserMessages.DotaUmMiniTaunt, OnDotaUmMiniTaunt);
            callbacks.Add((uint)EDotaUserMessages.DotaUmBuyBackStateAlert, OnDotaUmBuyBackStateAlert);
            callbacks.Add((uint)EDotaUserMessages.DotaUmSpeechBubble, OnDotaUmSpeechBubble);
            callbacks.Add((uint)EDotaUserMessages.DotaUmCustomHeaderMessage, OnDotaUmCustomHeaderMessage);
            callbacks.Add((uint)EDotaUserMessages.DotaUmQuickBuyAlert, OnDotaUmQuickBuyAlert);
            callbacks.Add((uint)EDotaUserMessages.DotaUmStatsHeroDetails, OnDotaUmStatsHeroDetails);
            callbacks.Add((uint)EDotaUserMessages.DotaUmPredictionResult, OnDotaUmPredictionResult);
            callbacks.Add((uint)EDotaUserMessages.DotaUmModifierAlert, OnDotaUmModifierAlert);
            callbacks.Add((uint)EDotaUserMessages.DotaUmHpmanaAlert, OnDotaUmHpmanaAlert);
            callbacks.Add((uint)EDotaUserMessages.DotaUmGlyphAlert, OnDotaUmGlyphAlert);
            callbacks.Add((uint)EDotaUserMessages.DotaUmBeastChat, OnDotaUmBeastChat);
            callbacks.Add((uint)EDotaUserMessages.DotaUmSpectatorPlayerUnitOrders, OnDotaUmSpectatorPlayerUnitOrders);
            callbacks.Add((uint)EDotaUserMessages.DotaUmCustomHudElementCreate, OnDotaUmCustomHudElementCreate);
            callbacks.Add((uint)EDotaUserMessages.DotaUmCustomHudElementModify, OnDotaUmCustomHudElementModify);
            callbacks.Add((uint)EDotaUserMessages.DotaUmCustomHudElementDestroy, OnDotaUmCustomHudElementDestroy);
            callbacks.Add((uint)EDotaUserMessages.DotaUmCompendiumState, OnDotaUmCompendiumState);
            callbacks.Add((uint)EDotaUserMessages.DotaUmProjectionAbility, OnDotaUmProjectionAbility);
            callbacks.Add((uint)EDotaUserMessages.DotaUmProjectionEvent, OnDotaUmProjectionEvent);
            callbacks.Add((uint)EDotaUserMessages.DotaUmCombatLogDataHltv, OnDotaUmCombatLogDataHltv);
            callbacks.Add((uint)EDotaUserMessages.DotaUmXpalert, OnDotaUmXpalert);
            callbacks.Add((uint)EDotaUserMessages.DotaUmUpdateQuestProgress, OnDotaUmUpdateQuestProgress);
            callbacks.Add((uint)EDotaUserMessages.DotaUmMatchDetails, OnDotaUmMatchDetails);
            callbacks.Add((uint)EDotaUserMessages.DotaUmQuestStatus, OnDotaUmQuestStatus);
            callbacks.Add((uint)EDotaUserMessages.DotaUmSuggestHeroPick, OnDotaUmSuggestHeroPick);
            callbacks.Add((uint)EDotaUserMessages.DotaUmSuggestHeroRole, OnDotaUmSuggestHeroRole);
            callbacks.Add((uint)EDotaUserMessages.DotaUmKillcamDamageTaken, OnDotaUmKillcamDamageTaken);
            callbacks.Add((uint)EDotaUserMessages.DotaUmSelectPenaltyGold, OnDotaUmSelectPenaltyGold);
            callbacks.Add((uint)EDotaUserMessages.DotaUmRollDiceResult, OnDotaUmRollDiceResult);
            callbacks.Add((uint)EDotaUserMessages.DotaUmFlipCoinResult, OnDotaUmFlipCoinResult);
            callbacks.Add((uint)EDotaUserMessages.DotaUmRequestItemSuggestions, OnDotaUmRequestItemSuggestions);
            callbacks.Add((uint)EDotaUserMessages.DotaUmTeamCaptainChanged, OnDotaUmTeamCaptainChanged);
            callbacks.Add((uint)EDotaUserMessages.DotaUmSendRoshanSpectatorPhase, OnDotaUmSendRoshanSpectatorPhase);
            callbacks.Add((uint)EDotaUserMessages.DotaUmChatWheelCooldown, OnDotaUmChatWheelCooldown);
            callbacks.Add((uint)EDotaUserMessages.DotaUmDismissAllStatPopups, OnDotaUmDismissAllStatPopups);
            callbacks.Add((uint)EDotaUserMessages.DotaUmTeDestroyProjectile, OnDotaUmTeDestroyProjectile);
            callbacks.Add((uint)EDotaUserMessages.DotaUmHeroRelicProgress, OnDotaUmHeroRelicProgress);
            callbacks.Add((uint)EDotaUserMessages.DotaUmAbilityDraftRequestAbility, OnDotaUmAbilityDraftRequestAbility);
            callbacks.Add((uint)EDotaUserMessages.DotaUmItemSold, OnDotaUmItemSold);
            callbacks.Add((uint)EDotaUserMessages.DotaUmDamageReport, OnDotaUmDamageReport);
            callbacks.Add((uint)EDotaUserMessages.DotaUmSalutePlayer, OnDotaUmSalutePlayer);
            callbacks.Add((uint)EDotaUserMessages.DotaUmTipAlert, OnDotaUmTipAlert);
            callbacks.Add((uint)EDotaUserMessages.DotaUmReplaceQueryUnit, OnDotaUmReplaceQueryUnit);
            callbacks.Add((uint)EDotaUserMessages.DotaUmEmptyTeleportAlert, OnDotaUmEmptyTeleportAlert);
            callbacks.Add((uint)EDotaUserMessages.DotaUmMarsArenaOfBloodAttack, OnDotaUmMarsArenaOfBloodAttack);
            callbacks.Add((uint)EDotaUserMessages.DotaUmEsarcanaCombo, OnDotaUmEsarcanaCombo);
            callbacks.Add((uint)EDotaUserMessages.DotaUmEsarcanaComboSummary, OnDotaUmEsarcanaComboSummary);
            callbacks.Add((uint)EDotaUserMessages.DotaUmHighFiveLeftHanging, OnDotaUmHighFiveLeftHanging);
            callbacks.Add((uint)EDotaUserMessages.DotaUmHighFiveCompleted, OnDotaUmHighFiveCompleted);
            callbacks.Add((uint)EDotaUserMessages.DotaUmShovelUnearth, OnDotaUmShovelUnearth);
            callbacks.Add((uint)EDotaUserMessages.DotaUmRadarAlert, OnDotaUmRadarAlert);
            callbacks.Add((uint)EDotaUserMessages.DotaUmAllStarEvent, OnDotaUmAllStarEvent);
            callbacks.Add((uint)EDotaUserMessages.DotaUmTalentTreeAlert, OnDotaUmTalentTreeAlert);
            callbacks.Add((uint)EDotaUserMessages.DotaUmQueuedOrderRemoved, OnDotaUmQueuedOrderRemoved);
            callbacks.Add((uint)EDotaUserMessages.DotaUmDebugChallenge, OnDotaUmDebugChallenge);
            callbacks.Add((uint)EDotaUserMessages.DotaUmOmarcanaCombo, OnDotaUmOmarcanaCombo);
            callbacks.Add((uint)EDotaUserMessages.DotaUmFoundNeutralItem, OnDotaUmFoundNeutralItem);
            callbacks.Add((uint)EDotaUserMessages.DotaUmOutpostCaptured, OnDotaUmOutpostCaptured);
            callbacks.Add((uint)EDotaUserMessages.DotaUmOutpostGrantedXp, OnDotaUmOutpostGrantedXp);
            callbacks.Add((uint)EDotaUserMessages.DotaUmMoveCameraToUnit, OnDotaUmMoveCameraToUnit);
            callbacks.Add((uint)EDotaUserMessages.DotaUmPauseMinigameData, OnDotaUmPauseMinigameData);
            callbacks.Add((uint)EDotaUserMessages.DotaUmVersusScenePlayerBehavior, OnDotaUmVersusScenePlayerBehavior);
            callbacks.Add((uint)EDotaUserMessages.DotaUmQoPArcanaSummary, OnDotaUmQoPArcanaSummary);
            callbacks.Add((uint)EDotaUserMessages.DotaUmHotPotatoCreated, OnDotaUmHotPotatoCreated);
            callbacks.Add((uint)EDotaUserMessages.DotaUmHotPotatoExploded, OnDotaUmHotPotatoExploded);
            callbacks.Add((uint)EDotaUserMessages.DotaUmWkArcanaProgress, OnDotaUmWkArcanaProgress);
            callbacks.Add((uint)EDotaUserMessages.DotaUmGuildChallengeProgress, OnDotaUmGuildChallengeProgress);
            callbacks.Add((uint)EDotaUserMessages.DotaUmWrarcanaProgress, OnDotaUmWrarcanaProgress);
            callbacks.Add((uint)EDotaUserMessages.DotaUmWrarcanaSummary, OnDotaUmWrarcanaSummary);
            callbacks.Add((uint)EDotaUserMessages.DotaUmEmptyItemSlotAlert, OnDotaUmEmptyItemSlotAlert);
            callbacks.Add((uint)EDotaUserMessages.DotaUmAghsStatusAlert, OnDotaUmAghsStatusAlert);
            callbacks.Add((uint)EDotaUserMessages.DotaUmPingConfirmation, OnDotaUmPingConfirmation);
            callbacks.Add((uint)EDotaUserMessages.DotaUmMutedPlayers, OnDotaUmMutedPlayers);
            callbacks.Add((uint)EDotaUserMessages.DotaUmContextualTip, OnDotaUmContextualTip);
            callbacks.Add((uint)EDotaUserMessages.DotaUmChatMessage, OnDotaUmChatMessage);
            callbacks.Add((uint)EDotaUserMessages.DotaUmNeutralCampAlert, OnDotaUmNeutralCampAlert);
            callbacks.Add((uint)EDotaUserMessages.DotaUmRockPaperScissorsStarted, OnDotaUmRockPaperScissorsStarted);
            callbacks.Add((uint)EDotaUserMessages.DotaUmRockPaperScissorsFinished, OnDotaUmRockPaperScissorsFinished);
            callbacks.Add((uint)EDotaUserMessages.DotaUmDuelOpponentKilled, OnDotaUmDuelOpponentKilled);
            callbacks.Add((uint)EDotaUserMessages.DotaUmDuelAccepted, OnDotaUmDuelAccepted);
            callbacks.Add((uint)EDotaUserMessages.DotaUmDuelRequested, OnDotaUmDuelRequested);
            callbacks.Add((uint)EDotaUserMessages.DotaUmMuertaReleaseEventAssignedTargetKilled, OnDotaUmMuertaReleaseEventAssignedTargetKilled);
            callbacks.Add((uint)EDotaUserMessages.DotaUmPlayerDraftSuggestPick, OnDotaUmPlayerDraftSuggestPick);
            callbacks.Add((uint)EDotaUserMessages.DotaUmPlayerDraftPick, OnDotaUmPlayerDraftPick);
            callbacks.Add((uint)EDotaUserMessages.DotaUmUpdateLinearProjectileCpdata, OnDotaUmUpdateLinearProjectileCpdata);
            callbacks.Add((uint)EDotaUserMessages.DotaUmGiftPlayer, OnDotaUmGiftPlayer);
            callbacks.Add((uint)EDotaUserMessages.DotaUmFacetPing, OnDotaUmFacetPing);
            callbacks.Add((uint)EDotaUserMessages.DotaUmInnatePing, OnDotaUmInnatePing);
            callbacks.Add((uint)EDotaUserMessages.DotaUmRoshanTimer, OnDotaUmRoshanTimer);
            callbacks.Add((uint)EDotaUserMessages.DotaUmNeutralCraftAvailable, OnDotaUmNeutralCraftAvailable);
            callbacks.Add((uint)EDotaUserMessages.DotaUmTimerAlert, OnDotaUmTimerAlert);
            callbacks.Add((uint)EDotaUserMessages.DotaUmMadstoneAlert, OnDotaUmMadstoneAlert);
            callbacks.Add((uint)EDotaUserMessages.DotaUmCourierLeftFountainAlert, OnDotaUmCourierLeftFountainAlert);
            
            var built = builder.Complete();
            built.AttachToFrameCallbacks(frameCallbacks);
        }



        private void OnNetNop(Source2ReplayPacket.MessageData data)
        {
            var pb0 = data.GetAsProtobuf<CNETMsg_NOP>();
        }
        // case (uint)NET_Messages.NetDisconnectLegacy:
        //     var pb1 = data.GetAsProtobuf<CNETMsg_Disconnect_Legacy>();
        //     break;

        private void OnNetSplitScreenUser(Source2ReplayPacket.MessageData data)
        {
            var pb3 = data.GetAsProtobuf<CNETMsg_SplitScreenUser>();
        }

        private void OnNetTick(Source2ReplayPacket.MessageData data)
        {
            var pb4 = data.GetAsProtobuf<CNETMsg_Tick>();
        }

        private void OnNetStringCmd(Source2ReplayPacket.MessageData data)
        {
            var pb5 = data.GetAsProtobuf<CNETMsg_StringCmd>();
        }

        private void OnNetSetConVar(Source2ReplayPacket.MessageData data)
        {
            var pb6 = data.GetAsProtobuf<CNETMsg_SetConVar>();
        }

        private void OnNetSignonState(Source2ReplayPacket.MessageData data)
        {
            var pb7 = data.GetAsProtobuf<CNETMsg_SignonState>();
        }

        private void OnNetSpawnGroupLoad(Source2ReplayPacket.MessageData data)
        {
            var pb8 = data.GetAsProtobuf<CNETMsg_SpawnGroup_Load>();
        }

        private void OnNetSpawnGroupManifestUpdate(Source2ReplayPacket.MessageData data)
        {
            var pb9 = data.GetAsProtobuf<CNETMsg_SpawnGroup_ManifestUpdate>();
        }

        private void OnNetSpawnGroupSetCreationTick(Source2ReplayPacket.MessageData data)
        {
            var pb11 = data.GetAsProtobuf<CNETMsg_SpawnGroup_SetCreationTick>();
        }

        private void OnNetSpawnGroupUnload(Source2ReplayPacket.MessageData data)
        {
            var pb12 = data.GetAsProtobuf<CNETMsg_SpawnGroup_Unload>();
        }

        private void OnNetSpawnGroupLoadCompleted(Source2ReplayPacket.MessageData data)
        {
            var pb13 = data.GetAsProtobuf<CNETMsg_SpawnGroup_LoadCompleted>();
        }

        private void OnNetDebugOverlay(Source2ReplayPacket.MessageData data)
        {
            var pb15 = data.GetAsProtobuf<CNETMsg_DebugOverlay>();
        }


        private void OnClcClientInfo(Source2ReplayPacket.MessageData data)
        {
            var pb20 = data.GetAsProtobuf<CCLCMsg_ClientInfo>();
        }

        private void OnClcMove(Source2ReplayPacket.MessageData data)
        {
            var pb21 = data.GetAsProtobuf<CCLCMsg_Move>();
        }

        private void OnClcVoiceData(Source2ReplayPacket.MessageData data)
        {
            var pb22 = data.GetAsProtobuf<CCLCMsg_VoiceData>();
        }

        private void OnClcBaselineAck(Source2ReplayPacket.MessageData data)
        {
            var pb23 = data.GetAsProtobuf<CCLCMsg_BaselineAck>();
        }

        private void OnClcRespondCvarValue(Source2ReplayPacket.MessageData data)
        {
            var pb25 = data.GetAsProtobuf<CCLCMsg_RespondCvarValue>();
        }

        private void OnClcFileCrccheck(Source2ReplayPacket.MessageData data)
        {
            var pb26 = data.GetAsProtobuf<CCLCMsg_FileCRCCheck>();
        }

        private void OnClcLoadingProgress(Source2ReplayPacket.MessageData data)
        {
            var pb27 = data.GetAsProtobuf<CCLCMsg_LoadingProgress>();
        }

        private void OnClcSplitPlayerConnect(Source2ReplayPacket.MessageData data)
        {
            var pb28 = data.GetAsProtobuf<CCLCMsg_SplitPlayerConnect>();
        }

        private void OnClcSplitPlayerDisconnect(Source2ReplayPacket.MessageData data)
        {
            var pb30 = data.GetAsProtobuf<CCLCMsg_SplitPlayerDisconnect>();
        }

        private void OnClcServerStatus(Source2ReplayPacket.MessageData data)
        {
            var pb31 = data.GetAsProtobuf<CCLCMsg_ServerStatus>();
        }

        private void OnClcRequestPause(Source2ReplayPacket.MessageData data)
        {
            var pb33 = data.GetAsProtobuf<CCLCMsg_RequestPause>();
        }

        private void OnClcCmdKeyValues(Source2ReplayPacket.MessageData data)
        {
            var pb34 = data.GetAsProtobuf<CCLCMsg_CmdKeyValues>();
        }

        private void OnClcRconServerDetails(Source2ReplayPacket.MessageData data)
        {
            var pb35 = data.GetAsProtobuf<CCLCMsg_RconServerDetails>();
        }

        private void OnClcHltvReplay(Source2ReplayPacket.MessageData data)
        {
            var pb36 = data.GetAsProtobuf<CCLCMsg_HltvReplay>();
        }

        private void OnClcDiagnostic(Source2ReplayPacket.MessageData data)
        {
            var pb37 = data.GetAsProtobuf<CCLCMsg_Diagnostic>();
        }


        private void OnSvcServerInfo(Source2ReplayPacket.MessageData data)
        {
            var pb40 = data.GetAsProtobuf<CSVCMsg_ServerInfo>();
        }

        private void OnSvcFlattenedSerializer(Source2ReplayPacket.MessageData data)
        {
            var pb41 = data.GetAsProtobuf<CSVCMsg_FlattenedSerializer>();
        }

        private void OnSvcClassInfo(Source2ReplayPacket.MessageData data)
        {
            var pb42 = data.GetAsProtobuf<CSVCMsg_ClassInfo>();
        }

        private void OnSvcSetPause(Source2ReplayPacket.MessageData data)
        {
            var pb43 = data.GetAsProtobuf<CSVCMsg_SetPause>();
        }

        private void OnSvcCreateStringTable(Source2ReplayPacket.MessageData data)
        {
            var pb44 = data.GetAsProtobuf<CSVCMsg_CreateStringTable>();
        }

        private void OnSvcUpdateStringTable(Source2ReplayPacket.MessageData data)
        {
            var pb45 = data.GetAsProtobuf<CSVCMsg_UpdateStringTable>();
        }

        private void OnSvcVoiceInit(Source2ReplayPacket.MessageData data)
        {
            var pb46 = data.GetAsProtobuf<CSVCMsg_VoiceInit>();
        }

        private void OnSvcVoiceData(Source2ReplayPacket.MessageData data)
        {
            var pb47 = data.GetAsProtobuf<CSVCMsg_VoiceData>();
        }

        private void OnSvcPrint(Source2ReplayPacket.MessageData data)
        {
            var pb48 = data.GetAsProtobuf<CSVCMsg_Print>();
        }

        private void OnSvcSounds(Source2ReplayPacket.MessageData data)
        {
            var pb49 = data.GetAsProtobuf<CSVCMsg_Sounds>();
        }

        private void OnSvcSetView(Source2ReplayPacket.MessageData data)
        {
            var pb50 = data.GetAsProtobuf<CSVCMsg_SetView>();
        }

        private void OnSvcClearAllStringTables(Source2ReplayPacket.MessageData data)
        {
            var pb51 = data.GetAsProtobuf<CSVCMsg_ClearAllStringTables>();
        }

        private void OnSvcCmdKeyValues(Source2ReplayPacket.MessageData data)
        {
            var pb52 = data.GetAsProtobuf<CSVCMsg_CmdKeyValues>();
        }

        private void OnSvcBspdecal(Source2ReplayPacket.MessageData data)
        {
            var pb53 = data.GetAsProtobuf<CSVCMsg_BSPDecal>();
        }

        private void OnSvcSplitScreen(Source2ReplayPacket.MessageData data)
        {
            var pb54 = data.GetAsProtobuf<CSVCMsg_SplitScreen>();
        }

        private void OnSvcPacketEntities(Source2ReplayPacket.MessageData data)
        {
            var pb55 = data.GetAsProtobuf<CSVCMsg_PacketEntities>();
        }

        private void OnSvcPrefetch(Source2ReplayPacket.MessageData data)
        {
            var pb56 = data.GetAsProtobuf<CSVCMsg_Prefetch>();
        }

        private void OnSvcMenu(Source2ReplayPacket.MessageData data)
        {
            var pb57 = data.GetAsProtobuf<CSVCMsg_Menu>();
        }

        private void OnSvcGetCvarValue(Source2ReplayPacket.MessageData data)
        {
            var pb58 = data.GetAsProtobuf<CSVCMsg_GetCvarValue>();
        }

        private void OnSvcStopSound(Source2ReplayPacket.MessageData data)
        {
            var pb59 = data.GetAsProtobuf<CSVCMsg_StopSound>();
        }

        private void OnSvcPeerList(Source2ReplayPacket.MessageData data)
        {
            var pb60 = data.GetAsProtobuf<CSVCMsg_PeerList>();
        }

        private void OnSvcPacketReliable(Source2ReplayPacket.MessageData data)
        {
            var pb61 = data.GetAsProtobuf<CSVCMsg_PacketReliable>();
        }

        private void OnSvcHltvstatus(Source2ReplayPacket.MessageData data)
        {
            var pb62 = data.GetAsProtobuf<CSVCMsg_HLTVStatus>();
        }

        private void OnSvcServerSteamId(Source2ReplayPacket.MessageData data)
        {
            var pb63 = data.GetAsProtobuf<CSVCMsg_ServerSteamID>();
        }

        private void OnSvcFullFrameSplit(Source2ReplayPacket.MessageData data)
        {
            var pb70 = data.GetAsProtobuf<CSVCMsg_FullFrameSplit>();
        }

        private void OnSvcRconServerDetails(Source2ReplayPacket.MessageData data)
        {
            var pb71 = data.GetAsProtobuf<CSVCMsg_RconServerDetails>();
        }

        private void OnSvcUserMessage(Source2ReplayPacket.MessageData data)
        {
            var pb72 = data.GetAsProtobuf<CSVCMsg_UserMessage>();
        }

        private void OnSvcBroadcastCommand(Source2ReplayPacket.MessageData data)
        {
            var pb74 = data.GetAsProtobuf<CSVCMsg_Broadcast_Command>();
        }

        private void OnSvcHltvFixupOperatorStatus(Source2ReplayPacket.MessageData data)
        {
            var pb75 = data.GetAsProtobuf<CSVCMsg_HltvFixupOperatorStatus>();
        }

        private void OnSvcUserCmds(Source2ReplayPacket.MessageData data)
        {
            var pb76 = data.GetAsProtobuf<CSVCMsg_UserCommands>();
        }

        // case (uint)EDotaUserMessages.DotaUmAddUnitToSelection:
        //     var pb464 = data.GetAsProtobuf<CDOTAUserMsg_AddUnitToSelection>();
        //     break;

        private void OnDotaUmAidebugLine(Source2ReplayPacket.MessageData data)
        {
            var pb465 = data.GetAsProtobuf<CDOTAUserMsg_AIDebugLine>();
        }

        private void OnDotaUmChatEvent(Source2ReplayPacket.MessageData data)
        {
            var pb466 = data.GetAsProtobuf<CDOTAUserMsg_ChatEvent>();
        }

        private void OnDotaUmCombatHeroPositions(Source2ReplayPacket.MessageData data)
        {
            var pb467 = data.GetAsProtobuf<CDOTAUserMsg_CombatHeroPositions>();
        }

        private void OnDotaUmCombatLogData(Source2ReplayPacket.MessageData data)
        {
            var pb468 = data.GetAsProtobuf<CMsgDOTACombatLogEntry>();
        }

        private void OnDotaUmCombatLogBulkData(Source2ReplayPacket.MessageData data)
        {
            var pb470 = data.GetAsProtobuf<CDOTAUserMsg_CombatLogBulkData>();
        }

        private void OnDotaUmCreateLinearProjectile(Source2ReplayPacket.MessageData data)
        {
            var pb471 = data.GetAsProtobuf<CDOTAUserMsg_CreateLinearProjectile>();
        }

        private void OnDotaUmDestroyLinearProjectile(Source2ReplayPacket.MessageData data)
        {
            var pb472 = data.GetAsProtobuf<CDOTAUserMsg_DestroyLinearProjectile>();
        }

        private void OnDotaUmDodgeTrackingProjectiles(Source2ReplayPacket.MessageData data)
        {
            var pb473 = data.GetAsProtobuf<CDOTAUserMsg_DodgeTrackingProjectiles>();
        }

        private void OnDotaUmGlobalLightColor(Source2ReplayPacket.MessageData data)
        {
            var pb474 = data.GetAsProtobuf<CDOTAUserMsg_GlobalLightColor>();
        }

        private void OnDotaUmGlobalLightDirection(Source2ReplayPacket.MessageData data)
        {
            var pb475 = data.GetAsProtobuf<CDOTAUserMsg_GlobalLightDirection>();
        }

        private void OnDotaUmInvalidCommand(Source2ReplayPacket.MessageData data)
        {
            var pb476 = data.GetAsProtobuf<CDOTAUserMsg_InvalidCommand>();
        }

        private void OnDotaUmLocationPing(Source2ReplayPacket.MessageData data)
        {
            var pb477 = data.GetAsProtobuf<CDOTAUserMsg_LocationPing>();
        }

        private void OnDotaUmMapLine(Source2ReplayPacket.MessageData data)
        {
            var pb478 = data.GetAsProtobuf<CDOTAUserMsg_MapLine>();
        }

        private void OnDotaUmMiniKillCamInfo(Source2ReplayPacket.MessageData data)
        {
            var pb479 = data.GetAsProtobuf<CDOTAUserMsg_MiniKillCamInfo>();
        }

        private void OnDotaUmMinimapDebugPoint(Source2ReplayPacket.MessageData data)
        {
            var pb480 = data.GetAsProtobuf<CDOTAUserMsg_MinimapDebugPoint>();
        }

        private void OnDotaUmMinimapEvent(Source2ReplayPacket.MessageData data)
        {
            var pb481 = data.GetAsProtobuf<CDOTAUserMsg_MinimapEvent>();
        }

        private void OnDotaUmNevermoreRequiem(Source2ReplayPacket.MessageData data)
        {
            var pb482 = data.GetAsProtobuf<CDOTAUserMsg_NevermoreRequiem>();
        }

        private void OnDotaUmOverheadEvent(Source2ReplayPacket.MessageData data)
        {
            var pb483 = data.GetAsProtobuf<CDOTAUserMsg_OverheadEvent>();
        }

        private void OnDotaUmSetNextAutobuyItem(Source2ReplayPacket.MessageData data)
        {
            var pb484 = data.GetAsProtobuf<CDOTAUserMsg_SetNextAutobuyItem>();
        }

        private void OnDotaUmSharedCooldown(Source2ReplayPacket.MessageData data)
        {
            var pb485 = data.GetAsProtobuf<CDOTAUserMsg_SharedCooldown>();
        }

        private void OnDotaUmSpectatorPlayerClick(Source2ReplayPacket.MessageData data)
        {
            var pb486 = data.GetAsProtobuf<CDOTAUserMsg_SpectatorPlayerClick>();
        }

        private void OnDotaUmTutorialTipInfo(Source2ReplayPacket.MessageData data)
        {
            var pb487 = data.GetAsProtobuf<CDOTAUserMsg_TutorialTipInfo>();
        }

        private void OnDotaUmUnitEvent(Source2ReplayPacket.MessageData data)
        {
            var pb488 = data.GetAsProtobuf<CDOTAUserMsg_UnitEvent>();
        }
        // case (uint)EDotaUserMessages.DotaUmParticleManager:
        //     var pb489 = data.GetAsProtobuf<CDOTAUserMsg_ParticleManager>();
        //     break;

        private void OnDotaUmBotChat(Source2ReplayPacket.MessageData data)
        {
            var pb490 = data.GetAsProtobuf<CDOTAUserMsg_BotChat>();
        }

        private void OnDotaUmHudError(Source2ReplayPacket.MessageData data)
        {
            var pb491 = data.GetAsProtobuf<CDOTAUserMsg_HudError>();
        }

        private void OnDotaUmItemPurchased(Source2ReplayPacket.MessageData data)
        {
            var pb492 = data.GetAsProtobuf<CDOTAUserMsg_ItemPurchased>();
        }

        private void OnDotaUmPing(Source2ReplayPacket.MessageData data)
        {
            var pb493 = data.GetAsProtobuf<CDOTAUserMsg_Ping>();
        }

        private void OnDotaUmItemFound(Source2ReplayPacket.MessageData data)
        {
            var pb494 = data.GetAsProtobuf<CDOTAUserMsg_ItemFound>();
        }
        // case (uint)EDotaUserMessages.DotaUmCharacterSpeakConcept:
        //     var pb495 = data.GetAsProtobuf<CDOTAUserMsg_CharacterSpeakConcept>();
        //     break;

        private void OnDotaUmSwapVerify(Source2ReplayPacket.MessageData data)
        {
            var pb496 = data.GetAsProtobuf<CDOTAUserMsg_SwapVerify>();
        }

        private void OnDotaUmWorldLine(Source2ReplayPacket.MessageData data)
        {
            var pb497 = data.GetAsProtobuf<CDOTAUserMsg_WorldLine>();
        }
        // case (uint)EDotaUserMessages.DotaUmTournamentDrop:
        //     var pb498 = data.GetAsProtobuf<CDOTAUserMsg_TournamentDrop>();
        //     break;

        private void OnDotaUmItemAlert(Source2ReplayPacket.MessageData data)
        {
            var pb499 = data.GetAsProtobuf<CDOTAUserMsg_ItemAlert>();
        }

        private void OnDotaUmHalloweenDrops(Source2ReplayPacket.MessageData data)
        {
            var pb500 = data.GetAsProtobuf<CDOTAUserMsg_HalloweenDrops>();
        }

        private void OnDotaUmChatWheel(Source2ReplayPacket.MessageData data)
        {
            var pb501 = data.GetAsProtobuf<CDOTAUserMsg_ChatWheel>();
        }

        private void OnDotaUmReceivedXmasGift(Source2ReplayPacket.MessageData data)
        {
            var pb502 = data.GetAsProtobuf<CDOTAUserMsg_ReceivedXmasGift>();
        }

        private void OnDotaUmUpdateSharedContent(Source2ReplayPacket.MessageData data)
        {
            var pb503 = data.GetAsProtobuf<CDOTAUserMsg_UpdateSharedContent>();
        }

        private void OnDotaUmTutorialRequestExp(Source2ReplayPacket.MessageData data)
        {
            var pb504 = data.GetAsProtobuf<CDOTAUserMsg_TutorialRequestExp>();
        }

        private void OnDotaUmTutorialPingMinimap(Source2ReplayPacket.MessageData data)
        {
            var pb505 = data.GetAsProtobuf<CDOTAUserMsg_TutorialPingMinimap>();
        }

        private void OnDotaUmGamerulesStateChanged(Source2ReplayPacket.MessageData data)
        {
            var pb506 = data.GetAsProtobuf<CDOTAUserMsg_GamerulesStateChanged>();
        }

        private void OnDotaUmShowSurvey(Source2ReplayPacket.MessageData data)
        {
            var pb507 = data.GetAsProtobuf<CDOTAUserMsg_ShowSurvey>();
        }

        private void OnDotaUmTutorialFade(Source2ReplayPacket.MessageData data)
        {
            var pb508 = data.GetAsProtobuf<CDOTAUserMsg_TutorialFade>();
        }

        private void OnDotaUmAddQuestLogEntry(Source2ReplayPacket.MessageData data)
        {
            var pb509 = data.GetAsProtobuf<CDOTAUserMsg_AddQuestLogEntry>();
        }

        private void OnDotaUmSendStatPopup(Source2ReplayPacket.MessageData data)
        {
            var pb510 = data.GetAsProtobuf<CDOTAUserMsg_SendStatPopup>();
        }

        private void OnDotaUmTutorialFinish(Source2ReplayPacket.MessageData data)
        {
            var pb511 = data.GetAsProtobuf<CDOTAUserMsg_TutorialFinish>();
        }

        private void OnDotaUmSendRoshanPopup(Source2ReplayPacket.MessageData data)
        {
            var pb512 = data.GetAsProtobuf<CDOTAUserMsg_SendRoshanPopup>();
        }

        private void OnDotaUmSendGenericToolTip(Source2ReplayPacket.MessageData data)
        {
            var pb513 = data.GetAsProtobuf<CDOTAUserMsg_SendGenericToolTip>();
        }

        private void OnDotaUmSendFinalGold(Source2ReplayPacket.MessageData data)
        {
            var pb514 = data.GetAsProtobuf<CDOTAUserMsg_SendFinalGold>();
        }

        private void OnDotaUmCustomMsg(Source2ReplayPacket.MessageData data)
        {
            var pb515 = data.GetAsProtobuf<CDOTAUserMsg_CustomMsg>();
        }

        private void OnDotaUmCoachHudping(Source2ReplayPacket.MessageData data)
        {
            var pb516 = data.GetAsProtobuf<CDOTAUserMsg_CoachHUDPing>();
        }

        private void OnDotaUmClientLoadGridNav(Source2ReplayPacket.MessageData data)
        {
            var pb517 = data.GetAsProtobuf<CDOTAUserMsg_ClientLoadGridNav>();
        }

        private void OnDotaUmTeProjectile(Source2ReplayPacket.MessageData data)
        {
            var pb518 = data.GetAsProtobuf<CDOTAUserMsg_TE_Projectile>();
        }

        private void OnDotaUmTeProjectileLoc(Source2ReplayPacket.MessageData data)
        {
            var pb519 = data.GetAsProtobuf<CDOTAUserMsg_TE_ProjectileLoc>();
        }

        private void OnDotaUmTeDotaBloodImpact(Source2ReplayPacket.MessageData data)
        {
            var pb520 = data.GetAsProtobuf<CDOTAUserMsg_TE_DotaBloodImpact>();
        }

        private void OnDotaUmTeUnitAnimation(Source2ReplayPacket.MessageData data)
        {
            var pb521 = data.GetAsProtobuf<CDOTAUserMsg_TE_UnitAnimation>();
        }

        private void OnDotaUmTeUnitAnimationEnd(Source2ReplayPacket.MessageData data)
        {
            var pb522 = data.GetAsProtobuf<CDOTAUserMsg_TE_UnitAnimationEnd>();
        }

        private void OnDotaUmAbilityPing(Source2ReplayPacket.MessageData data)
        {
            var pb523 = data.GetAsProtobuf<CDOTAUserMsg_AbilityPing>();
        }

        private void OnDotaUmShowGenericPopup(Source2ReplayPacket.MessageData data)
        {
            var pb524 = data.GetAsProtobuf<CDOTAUserMsg_ShowGenericPopup>();
        }

        private void OnDotaUmVoteStart(Source2ReplayPacket.MessageData data)
        {
            var pb525 = data.GetAsProtobuf<CDOTAUserMsg_VoteStart>();
        }

        private void OnDotaUmVoteUpdate(Source2ReplayPacket.MessageData data)
        {
            var pb526 = data.GetAsProtobuf<CDOTAUserMsg_VoteUpdate>();
        }

        private void OnDotaUmVoteEnd(Source2ReplayPacket.MessageData data)
        {
            var pb527 = data.GetAsProtobuf<CDOTAUserMsg_VoteEnd>();
        }

        private void OnDotaUmBoosterState(Source2ReplayPacket.MessageData data)
        {
            var pb528 = data.GetAsProtobuf<CDOTAUserMsg_BoosterState>();
        }

        private void OnDotaUmWillPurchaseAlert(Source2ReplayPacket.MessageData data)
        {
            var pb529 = data.GetAsProtobuf<CDOTAUserMsg_WillPurchaseAlert>();
        }

        private void OnDotaUmTutorialMinimapPosition(Source2ReplayPacket.MessageData data)
        {
            var pb530 = data.GetAsProtobuf<CDOTAUserMsg_TutorialMinimapPosition>();
        }

        private void OnDotaUmAbilitySteal(Source2ReplayPacket.MessageData data)
        {
            var pb532 = data.GetAsProtobuf<CDOTAUserMsg_AbilitySteal>();
        }

        private void OnDotaUmCourierKilledAlert(Source2ReplayPacket.MessageData data)
        {
            var pb533 = data.GetAsProtobuf<CDOTAUserMsg_CourierKilledAlert>();
        }

        private void OnDotaUmEnemyItemAlert(Source2ReplayPacket.MessageData data)
        {
            var pb534 = data.GetAsProtobuf<CDOTAUserMsg_EnemyItemAlert>();
        }

        private void OnDotaUmStatsMatchDetails(Source2ReplayPacket.MessageData data)
        {
            var pb535 = data.GetAsProtobuf<CDOTAUserMsg_StatsMatchDetails>();
        }

        private void OnDotaUmMiniTaunt(Source2ReplayPacket.MessageData data)
        {
            var pb536 = data.GetAsProtobuf<CDOTAUserMsg_MiniTaunt>();
        }

        private void OnDotaUmBuyBackStateAlert(Source2ReplayPacket.MessageData data)
        {
            var pb537 = data.GetAsProtobuf<CDOTAUserMsg_BuyBackStateAlert>();
        }

        private void OnDotaUmSpeechBubble(Source2ReplayPacket.MessageData data)
        {
            var pb538 = data.GetAsProtobuf<CDOTAUserMsg_SpeechBubble>();
        }

        private void OnDotaUmCustomHeaderMessage(Source2ReplayPacket.MessageData data)
        {
            var pb539 = data.GetAsProtobuf<CDOTAUserMsg_CustomHeaderMessage>();
        }

        private void OnDotaUmQuickBuyAlert(Source2ReplayPacket.MessageData data)
        {
            var pb540 = data.GetAsProtobuf<CDOTAUserMsg_QuickBuyAlert>();
        }

        private void OnDotaUmStatsHeroDetails(Source2ReplayPacket.MessageData data)
        {
            var pb541 = data.GetAsProtobuf<CDOTAUserMsg_StatsHeroMinuteDetails>();
        }

        private void OnDotaUmPredictionResult(Source2ReplayPacket.MessageData data)
        {
            var pb542 = data.GetAsProtobuf<CDotaMsg_PredictionResult>();
        }

        private void OnDotaUmModifierAlert(Source2ReplayPacket.MessageData data)
        {
            var pb543 = data.GetAsProtobuf<CDOTAUserMsg_ModifierAlert>();
        }

        private void OnDotaUmHpmanaAlert(Source2ReplayPacket.MessageData data)
        {
            var pb544 = data.GetAsProtobuf<CDOTAUserMsg_HPManaAlert>();
        }

        private void OnDotaUmGlyphAlert(Source2ReplayPacket.MessageData data)
        {
            var pb545 = data.GetAsProtobuf<CDOTAUserMsg_GlyphAlert>();
        }

        private void OnDotaUmBeastChat(Source2ReplayPacket.MessageData data)
        {
            var pb546 = data.GetAsProtobuf<CDOTAUserMsg_BeastChat>();
        }

        private void OnDotaUmSpectatorPlayerUnitOrders(Source2ReplayPacket.MessageData data)
        {
            var pb547 = data.GetAsProtobuf<CDOTAUserMsg_SpectatorPlayerUnitOrders>();
        }

        private void OnDotaUmCustomHudElementCreate(Source2ReplayPacket.MessageData data)
        {
            var pb548 = data.GetAsProtobuf<CDOTAUserMsg_CustomHudElement_Create>();
        }

        private void OnDotaUmCustomHudElementModify(Source2ReplayPacket.MessageData data)
        {
            var pb549 = data.GetAsProtobuf<CDOTAUserMsg_CustomHudElement_Modify>();
        }

        private void OnDotaUmCustomHudElementDestroy(Source2ReplayPacket.MessageData data)
        {
            var pb550 = data.GetAsProtobuf<CDOTAUserMsg_CustomHudElement_Destroy>();
        }

        private void OnDotaUmCompendiumState(Source2ReplayPacket.MessageData data)
        {
            var pb551 = data.GetAsProtobuf<CDOTAUserMsg_CompendiumState>();
        }

        private void OnDotaUmProjectionAbility(Source2ReplayPacket.MessageData data)
        {
            var pb552 = data.GetAsProtobuf<CDOTAUserMsg_ProjectionAbility>();
        }

        private void OnDotaUmProjectionEvent(Source2ReplayPacket.MessageData data)
        {
            var pb553 = data.GetAsProtobuf<CDOTAUserMsg_ProjectionEvent>();
        }

        private void OnDotaUmCombatLogDataHltv(Source2ReplayPacket.MessageData data)
        {
            var pb554 = data.GetAsProtobuf<CMsgDOTACombatLogEntry>();
        }

        private void OnDotaUmXpalert(Source2ReplayPacket.MessageData data)
        {
            var pb555 = data.GetAsProtobuf<CDOTAUserMsg_XPAlert>();
        }

        private void OnDotaUmUpdateQuestProgress(Source2ReplayPacket.MessageData data)
        {
            var pb556 = data.GetAsProtobuf<CDOTAUserMsg_UpdateQuestProgress>();
        }
        // case (uint)EDotaUserMessages.DotaUmMatchMetadata:
        //     var pb557 = data.GetAsProtobuf<CDOTAUserMsg_MatchMetadata>();
        //     break;

        private void OnDotaUmMatchDetails(Source2ReplayPacket.MessageData data)
        {
            var pb558 = data.GetAsProtobuf<CDOTAUserMsg_StatsMatchDetails>();
        }

        private void OnDotaUmQuestStatus(Source2ReplayPacket.MessageData data)
        {
            var pb559 = data.GetAsProtobuf<CDOTAUserMsg_QuestStatus>();
        }

        private void OnDotaUmSuggestHeroPick(Source2ReplayPacket.MessageData data)
        {
            var pb560 = data.GetAsProtobuf<CDOTAUserMsg_SuggestHeroPick>();
        }

        private void OnDotaUmSuggestHeroRole(Source2ReplayPacket.MessageData data)
        {
            var pb561 = data.GetAsProtobuf<CDOTAUserMsg_SuggestHeroRole>();
        }

        private void OnDotaUmKillcamDamageTaken(Source2ReplayPacket.MessageData data)
        {
            var pb562 = data.GetAsProtobuf<CDOTAUserMsg_KillcamDamageTaken>();
        }

        private void OnDotaUmSelectPenaltyGold(Source2ReplayPacket.MessageData data)
        {
            var pb563 = data.GetAsProtobuf<CDOTAUserMsg_SelectPenaltyGold>();
        }

        private void OnDotaUmRollDiceResult(Source2ReplayPacket.MessageData data)
        {
            var pb564 = data.GetAsProtobuf<CDOTAUserMsg_RollDiceResult>();
        }

        private void OnDotaUmFlipCoinResult(Source2ReplayPacket.MessageData data)
        {
            var pb565 = data.GetAsProtobuf<CDOTAUserMsg_FlipCoinResult>();
        }

        private void OnDotaUmRequestItemSuggestions(Source2ReplayPacket.MessageData data)
        {
            var pb566 = data.GetAsProtobuf<CDOTAUserMessage_RequestItemSuggestions>();
        }

        private void OnDotaUmTeamCaptainChanged(Source2ReplayPacket.MessageData data)
        {
            var pb567 = data.GetAsProtobuf<CDOTAUserMessage_TeamCaptainChanged>();
        }

        private void OnDotaUmSendRoshanSpectatorPhase(Source2ReplayPacket.MessageData data)
        {
            var pb568 = data.GetAsProtobuf<CDOTAUserMsg_SendRoshanSpectatorPhase>();
        }

        private void OnDotaUmChatWheelCooldown(Source2ReplayPacket.MessageData data)
        {
            var pb569 = data.GetAsProtobuf<CDOTAUserMsg_ChatWheelCooldown>();
        }

        private void OnDotaUmDismissAllStatPopups(Source2ReplayPacket.MessageData data)
        {
            var pb570 = data.GetAsProtobuf<CDOTAUserMsg_DismissAllStatPopups>();
        }

        private void OnDotaUmTeDestroyProjectile(Source2ReplayPacket.MessageData data)
        {
            var pb571 = data.GetAsProtobuf<CDOTAUserMsg_TE_DestroyProjectile>();
        }

        private void OnDotaUmHeroRelicProgress(Source2ReplayPacket.MessageData data)
        {
            var pb572 = data.GetAsProtobuf<CDOTAUserMsg_HeroRelicProgress>();
        }

        private void OnDotaUmAbilityDraftRequestAbility(Source2ReplayPacket.MessageData data)
        {
            var pb573 = data.GetAsProtobuf<CDOTAUserMsg_AbilityDraftRequestAbility>();
        }

        private void OnDotaUmItemSold(Source2ReplayPacket.MessageData data)
        {
            var pb574 = data.GetAsProtobuf<CDOTAUserMsg_ItemSold>();
        }

        private void OnDotaUmDamageReport(Source2ReplayPacket.MessageData data)
        {
            var pb575 = data.GetAsProtobuf<CDOTAUserMsg_DamageReport>();
        }

        private void OnDotaUmSalutePlayer(Source2ReplayPacket.MessageData data)
        {
            var pb576 = data.GetAsProtobuf<CDOTAUserMsg_SalutePlayer>();
        }

        private void OnDotaUmTipAlert(Source2ReplayPacket.MessageData data)
        {
            var pb577 = data.GetAsProtobuf<CDOTAUserMsg_TipAlert>();
        }

        private void OnDotaUmReplaceQueryUnit(Source2ReplayPacket.MessageData data)
        {
            var pb578 = data.GetAsProtobuf<CDOTAUserMsg_ReplaceQueryUnit>();
        }

        private void OnDotaUmEmptyTeleportAlert(Source2ReplayPacket.MessageData data)
        {
            var pb579 = data.GetAsProtobuf<CDOTAUserMsg_EmptyTeleportAlert>();
        }

        private void OnDotaUmMarsArenaOfBloodAttack(Source2ReplayPacket.MessageData data)
        {
            var pb580 = data.GetAsProtobuf<CDOTAUserMsg_MarsArenaOfBloodAttack>();
        }

        private void OnDotaUmEsarcanaCombo(Source2ReplayPacket.MessageData data)
        {
            var pb581 = data.GetAsProtobuf<CDOTAUserMsg_ESArcanaCombo>();
        }

        private void OnDotaUmEsarcanaComboSummary(Source2ReplayPacket.MessageData data)
        {
            var pb582 = data.GetAsProtobuf<CDOTAUserMsg_ESArcanaComboSummary>();
        }

        private void OnDotaUmHighFiveLeftHanging(Source2ReplayPacket.MessageData data)
        {
            var pb583 = data.GetAsProtobuf<CDOTAUserMsg_HighFiveLeftHanging>();
        }

        private void OnDotaUmHighFiveCompleted(Source2ReplayPacket.MessageData data)
        {
            var pb584 = data.GetAsProtobuf<CDOTAUserMsg_HighFiveCompleted>();
        }

        private void OnDotaUmShovelUnearth(Source2ReplayPacket.MessageData data)
        {
            var pb585 = data.GetAsProtobuf<CDOTAUserMsg_ShovelUnearth>();
        }

        private void OnDotaUmRadarAlert(Source2ReplayPacket.MessageData data)
        {
            var pb587 = data.GetAsProtobuf<CDOTAUserMsg_RadarAlert>();
        }

        private void OnDotaUmAllStarEvent(Source2ReplayPacket.MessageData data)
        {
            var pb588 = data.GetAsProtobuf<CDOTAUserMsg_AllStarEvent>();
        }

        private void OnDotaUmTalentTreeAlert(Source2ReplayPacket.MessageData data)
        {
            var pb589 = data.GetAsProtobuf<CDOTAUserMsg_TalentTreeAlert>();
        }

        private void OnDotaUmQueuedOrderRemoved(Source2ReplayPacket.MessageData data)
        {
            var pb590 = data.GetAsProtobuf<CDOTAUserMsg_QueuedOrderRemoved>();
        }

        private void OnDotaUmDebugChallenge(Source2ReplayPacket.MessageData data)
        {
            var pb591 = data.GetAsProtobuf<CDOTAUserMsg_DebugChallenge>();
        }

        private void OnDotaUmOmarcanaCombo(Source2ReplayPacket.MessageData data)
        {
            var pb592 = data.GetAsProtobuf<CDOTAUserMsg_OMArcanaCombo>();
        }

        private void OnDotaUmFoundNeutralItem(Source2ReplayPacket.MessageData data)
        {
            var pb593 = data.GetAsProtobuf<CDOTAUserMsg_FoundNeutralItem>();
        }

        private void OnDotaUmOutpostCaptured(Source2ReplayPacket.MessageData data)
        {
            var pb594 = data.GetAsProtobuf<CDOTAUserMsg_OutpostCaptured>();
        }

        private void OnDotaUmOutpostGrantedXp(Source2ReplayPacket.MessageData data)
        {
            var pb595 = data.GetAsProtobuf<CDOTAUserMsg_OutpostGrantedXP>();
        }

        private void OnDotaUmMoveCameraToUnit(Source2ReplayPacket.MessageData data)
        {
            var pb596 = data.GetAsProtobuf<CDOTAUserMsg_MoveCameraToUnit>();
        }

        private void OnDotaUmPauseMinigameData(Source2ReplayPacket.MessageData data)
        {
            var pb597 = data.GetAsProtobuf<CDOTAUserMsg_PauseMinigameData>();
        }

        private void OnDotaUmVersusScenePlayerBehavior(Source2ReplayPacket.MessageData data)
        {
            var pb598 = data.GetAsProtobuf<CDOTAUserMsg_VersusScene_PlayerBehavior>();
        }

        private void OnDotaUmQoPArcanaSummary(Source2ReplayPacket.MessageData data)
        {
            var pb600 = data.GetAsProtobuf<CDOTAUserMsg_QoP_ArcanaSummary>();
        }

        private void OnDotaUmHotPotatoCreated(Source2ReplayPacket.MessageData data)
        {
            var pb601 = data.GetAsProtobuf<CDOTAUserMsg_HotPotato_Created>();
        }

        private void OnDotaUmHotPotatoExploded(Source2ReplayPacket.MessageData data)
        {
            var pb602 = data.GetAsProtobuf<CDOTAUserMsg_HotPotato_Exploded>();
        }

        private void OnDotaUmWkArcanaProgress(Source2ReplayPacket.MessageData data)
        {
            var pb603 = data.GetAsProtobuf<CDOTAUserMsg_WK_Arcana_Progress>();
        }

        private void OnDotaUmGuildChallengeProgress(Source2ReplayPacket.MessageData data)
        {
            var pb604 = data.GetAsProtobuf<CDOTAUserMsg_GuildChallenge_Progress>();
        }

        private void OnDotaUmWrarcanaProgress(Source2ReplayPacket.MessageData data)
        {
            var pb605 = data.GetAsProtobuf<CDOTAUserMsg_WRArcanaProgress>();
        }

        private void OnDotaUmWrarcanaSummary(Source2ReplayPacket.MessageData data)
        {
            var pb606 = data.GetAsProtobuf<CDOTAUserMsg_WRArcanaSummary>();
        }

        private void OnDotaUmEmptyItemSlotAlert(Source2ReplayPacket.MessageData data)
        {
            var pb607 = data.GetAsProtobuf<CDOTAUserMsg_EmptyItemSlotAlert>();
        }

        private void OnDotaUmAghsStatusAlert(Source2ReplayPacket.MessageData data)
        {
            var pb608 = data.GetAsProtobuf<CDOTAUserMsg_AghsStatusAlert>();
        }

        private void OnDotaUmPingConfirmation(Source2ReplayPacket.MessageData data)
        {
            var pb609 = data.GetAsProtobuf<CDOTAUserMsg_PingConfirmation>();
        }

        private void OnDotaUmMutedPlayers(Source2ReplayPacket.MessageData data)
        {
            var pb610 = data.GetAsProtobuf<CDOTAUserMsg_MutedPlayers>();
        }

        private void OnDotaUmContextualTip(Source2ReplayPacket.MessageData data)
        {
            var pb611 = data.GetAsProtobuf<CDOTAUserMsg_ContextualTip>();
        }

        private void OnDotaUmChatMessage(Source2ReplayPacket.MessageData data)
        {
            var pb612 = data.GetAsProtobuf<CDOTAUserMsg_ChatMessage>();
        }

        private void OnDotaUmNeutralCampAlert(Source2ReplayPacket.MessageData data)
        {
            var pb613 = data.GetAsProtobuf<CDOTAUserMsg_NeutralCampAlert>();
        }

        private void OnDotaUmRockPaperScissorsStarted(Source2ReplayPacket.MessageData data)
        {
            var pb614 = data.GetAsProtobuf<CDOTAUserMsg_RockPaperScissorsStarted>();
        }

        private void OnDotaUmRockPaperScissorsFinished(Source2ReplayPacket.MessageData data)
        {
            var pb615 = data.GetAsProtobuf<CDOTAUserMsg_RockPaperScissorsFinished>();
        }

        private void OnDotaUmDuelOpponentKilled(Source2ReplayPacket.MessageData data)
        {
            var pb616 = data.GetAsProtobuf<CDOTAUserMsg_DuelOpponentKilled>();
        }

        private void OnDotaUmDuelAccepted(Source2ReplayPacket.MessageData data)
        {
            var pb617 = data.GetAsProtobuf<CDOTAUserMsg_DuelAccepted>();
        }

        private void OnDotaUmDuelRequested(Source2ReplayPacket.MessageData data)
        {
            var pb618 = data.GetAsProtobuf<CDOTAUserMsg_DuelRequested>();
        }

        private void OnDotaUmMuertaReleaseEventAssignedTargetKilled(Source2ReplayPacket.MessageData data)
        {
            var pb619 = data.GetAsProtobuf<CDOTAUserMsg_MuertaReleaseEvent_AssignedTargetKilled>();
        }

        private void OnDotaUmPlayerDraftSuggestPick(Source2ReplayPacket.MessageData data)
        {
            var pb620 = data.GetAsProtobuf<CDOTAUserMsg_PlayerDraftSuggestPick>();
        }

        private void OnDotaUmPlayerDraftPick(Source2ReplayPacket.MessageData data)
        {
            var pb621 = data.GetAsProtobuf<CDOTAUserMsg_PlayerDraftPick>();
        }

        private void OnDotaUmUpdateLinearProjectileCpdata(Source2ReplayPacket.MessageData data)
        {
            var pb622 = data.GetAsProtobuf<CDOTAUserMsg_UpdateLinearProjectileCPData>();
        }

        private void OnDotaUmGiftPlayer(Source2ReplayPacket.MessageData data)
        {
            var pb623 = data.GetAsProtobuf<CDOTAUserMsg_GiftPlayer>();
        }

        private void OnDotaUmFacetPing(Source2ReplayPacket.MessageData data)
        {
            var pb624 = data.GetAsProtobuf<CDOTAUserMsg_FacetPing>();
        }

        private void OnDotaUmInnatePing(Source2ReplayPacket.MessageData data)
        {
            var pb625 = data.GetAsProtobuf<CDOTAUserMsg_InnatePing>();
        }

        private void OnDotaUmRoshanTimer(Source2ReplayPacket.MessageData data)
        {
            var pb626 = data.GetAsProtobuf<CDOTAUserMsg_RoshanTimer>();
        }

        private void OnDotaUmNeutralCraftAvailable(Source2ReplayPacket.MessageData data)
        {
            var pb627 = data.GetAsProtobuf<CDOTAUserMsg_NeutralCraftAvailable>();
        }

        private void OnDotaUmTimerAlert(Source2ReplayPacket.MessageData data)
        {
            var pb628 = data.GetAsProtobuf<CDOTAUserMsg_TimerAlert>();
        }

        private void OnDotaUmMadstoneAlert(Source2ReplayPacket.MessageData data)
        {
            var pb629 = data.GetAsProtobuf<CDOTAUserMsg_MadstoneAlert>();
        }

        private void OnDotaUmCourierLeftFountainAlert(Source2ReplayPacket.MessageData data)
        {
            var pb630 = data.GetAsProtobuf<CDOTAUserMsg_CourierLeftFountainAlert>();
        }
    }
}
