using System.Diagnostics;
using System.Text.RegularExpressions;
using necronomicon;
using necronomicon.model;
using Steam.Protos.Dota2;

namespace necronomicon.Examples;

// This class demonstrates using the API to attach callbacks only when
// information is known that you can make decisions from, it also chooses
// to hold no state from the library processing the files
public class TheEasiest
{
    public TheEasiest()
    {
    }

    public async Task StartAsync(Stream stream)
    {
        await Replay.ParseFileAsync(stream, OnEngineKnown);
    }

    private void OnEngineKnown(Replay.File file)
    {
        switch (file.MagicValue)
        {
            case EngineMagicHeader.SOURCE_2:
                file.ParseAsSource2Replay(OnGameKnown);
                break;
            default:
                throw new Exception();
        }

        file.RegisterCompletionTask(OnCompletion);
    }

    private void OnGameKnown(Source2ReplayFileHeaderHandler.InterestHelper helper)
    {

    }

    private ValueTask OnCompletion()
    {
        Debug.WriteLine("I'm done");

        return ValueTask.CompletedTask;
    }
}

// This class demonstrates using the API to attach to the raw data retrieved
// from the file, it is allowed to choose how it uses this data and what state
// it stores from the library and what subprocesses are called
public class RawDogginIt
{
    public string? GameId { get; private set; }
    private Action<Source2ReplayPacket.MessageData> _delly1;
    private Action<Source2ReplayPacket.MessageData> _delly2;


    private RawDogginIt()
    {
        _delly1 = WhatTheFuckIsThis;
        _delly2 = TableMessage;
    }

    public static async Task RunAsync(Stream stream)
    {
        var fella = new RawDogginIt();

        await Replay.ParseFileAsync(stream, fella.CheckTheReplay);
    }

    private void CheckTheReplay(Replay.File file)
    {
        file.ParseAsSource2Replay(CheckHeaderFrame);
    }

    private void CheckHeaderFrame(Source2Replay.Frame frame)
    {
        if (frame.FrameCommand != EDemoCommands.DemFileHeader)
            throw new Exception();

        frame.RegisterCallbackForData(HandleFileHeader);
    }

    private void CheckTheFrame(Source2Replay.Frame frame)
    {
        switch (frame.FrameCommand)
        {
            case EDemoCommands.DemPacket:
            case EDemoCommands.DemSignonPacket:
                frame.RegisterCallbackForData(HandlePacket);
                break;
            case EDemoCommands.DemSendTables:
                frame.RegisterCallbackForData(HandleSendTables);
                break;
            case EDemoCommands.DemFullPacket:
                frame.RegisterCallbackForData(HandleFullPacket);
                break;
            case EDemoCommands.DemClassInfo:
                frame.RegisterCallbackForData(HandleClassInfo);
                break;
            case EDemoCommands.DemFileInfo:
                frame.RegisterCallbackForData(HandleFileInfo);
                break;
            default:
                break;
        }
    }

    private void HandleFileHeader(Source2Replay.FrameData frame)
    {
        Debug.Assert(frame.FrameCommand == EDemoCommands.DemFileHeader);

        CDemoFileHeader fileHeader = frame.GetAsProtobuf<CDemoFileHeader>();
        string gameId = string.Empty;
        if (fileHeader.HasGame)
        {
            gameId = fileHeader.Game;
        }
        else if (fileHeader.HasGameDirectory)
        {
            var matchPattern = Regex.Match(fileHeader.GameDirectory, @".*[/\\](\w+)$");
            if (matchPattern.Success)
            {
                gameId = matchPattern.Groups[1].Value;
            }
        }

        if (gameId == string.Empty)
        {
            throw new NecronomiconException("Unable to get Game ID");
        }
        switch (gameId)
        {
            case "csgo":
                throw new NotImplementedException();
            case "dota":
                GameId = "dota";
                break;
            case "citadel":
                GameId = "citadel";
                break;
            default:
                throw new NecronomiconException($"Unexpected new game type: {gameId}");
        }

        frame.ChangeCallbackForNextFrame(CheckTheFrame);
    }

    private void HandleFileInfo(Source2Replay.FrameData frame)
    {
        var fileInfo = frame.GetAsProtobuf<CDemoFileInfo>();
    }

    private void HandleSendTables(Source2Replay.FrameData frame)
    {
        var sendTables = frame.GetAsProtobuf<CDemoSendTables>();
    }

    private void HandleClassInfo(Source2Replay.FrameData frame)
    {
        var classInfo = frame.GetAsProtobuf<CDemoClassInfo>();
    }

    private void HandlePacket(Source2Replay.FrameData frame)
    {
        var packet = frame.GetAsProtobuf<CDemoPacket>();
        Source2ReplayPacket.ProcessPacketMessages(packet.Data.Span, frame.FrameTick, HandleMessage);
    }

    private void HandleFullPacket(Source2Replay.FrameData frame)
    {
        var packet = frame.GetAsProtobuf<CDemoFullPacket>();
        Source2ReplayPacket.ProcessPacketMessages(packet.Packet.Data.Span, frame.FrameTick, HandleMessage);
    }

    private void HandleMessage(Source2ReplayPacket.Message message)
    {
        if (message.MessageType < 20)
            message.RegisterCallbackForData(_delly1);
        if (message.MessageType == 44)
            message.RegisterCallbackForData(_delly2);
    }

    private void WhatTheFuckIsThis(Source2ReplayPacket.MessageData message)
    {
        Debug.Assert(message.MessageType < 20);
    }

    private void TableMessage(Source2ReplayPacket.MessageData message)
    {
        var guy = message.GetAsProtobuf<CSVCMsg_CreateStringTable>();
    }
}
