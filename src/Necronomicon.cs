using System.Diagnostics;
using System.Text.RegularExpressions;
using necronomicon.model;
using necronomicon.model.engine;
using necronomicon.model.frames;
using necronomicon.source;
using Steam.Protos.Dota2;

namespace necronomicon;

public class Necronomicon
{
    private readonly InputStreamSource _inputStreamSource;
    private HashSet<FrameSkeleton> _frameSkeletons;
    public Necronomicon(string path)
    {
        _inputStreamSource = new InputStreamSource(path);
        _frameSkeletons = new HashSet<FrameSkeleton>();
    }
    public void infoForFile()
    {
        infoForSource(_inputStreamSource);
    }

    public void infoForSource(InputStreamSource source)
    {
        var engineType = determineEngineType(source);
        if (engineType != "dota")
        {
            throw new NecronomiconException($"Unable to parse engine type: {engineType}");
        }

        foreach (var frameSkeleton in _frameSkeletons)
        {
            if (frameSkeleton.FrameCommand == EDemoCommands.DemPacket)
            {
                CDemoPacket? packet = frameSkeleton.GetAsProtobuf<CDemoPacket>(frameSkeleton.FrameCommand);

                if (packet != null)
                {
                    EmbeddedMessage embeddedMessage = new EmbeddedMessage(packet.Data);
                    embeddedMessage.ParseMessages();
                }
            }
            Debug.WriteLine($"Tick {frameSkeleton.FrameTick}");
            Debug.WriteLine($"Command {frameSkeleton.FrameCommand}");
        }

        // source.setPosition(engineType.getInfoOffset());
        // var pi = engineType.getNextPacketInstance(source);
        // return (Demo.CDemoFileInfo)pi.parse();
    }

    private string determineEngineType(InputStreamSource source)
    {
        var buf = source.ReadEngineHeader();
        string engineMagicHeader = System.Text.Encoding.UTF8.GetString(buf);
        if (!EngineMagicHeaderExtensions.TryParseStringValue(engineMagicHeader, out var magicHeaderEnum))
        {
            throw new InvalidDataException("Invalid Header");
        }
        DemFile demFile = new DemFile(source);
        _frameSkeletons = demFile.ParseFilePackets();
        CDemoFileHeader cDemoFileHeader = demFile.GetFileHeader();

        string gameId = string.Empty;
        if (cDemoFileHeader.HasGame)
        {
            gameId = cDemoFileHeader.Game;
        }
        else if (cDemoFileHeader.HasGameDirectory)
        {
            var matchPattern = Regex.Match(cDemoFileHeader.GameDirectory, @".*[/\\](\w+)$");
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
                return "dota";
            case "citadel":
                throw new NotImplementedException();
            default:
                throw new NecronomiconException($"Unexpected new game type: {gameId}");
        }
    }


}
