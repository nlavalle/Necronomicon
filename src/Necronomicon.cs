using System.Diagnostics;
using System.Text.RegularExpressions;
using necronomicon.model;
using necronomicon.model.engine;
using necronomicon.model.frames;
using necronomicon.processor;
using necronomicon.source;
using Steam.Protos.Dota2;

namespace necronomicon;

public class Necronomicon
{
    private readonly InputStreamSource _inputStreamSource;
    private HashSet<FrameSkeleton> _frameSkeletons;
    private HashSet<ClassInfo> _classInfos;
    private Dictionary<string, Serializer> _serializers;

    private HashSet<string> pointerTypes = new HashSet<string>
        {
            "PhysicsRagdollPose_t",
            "CBodyComponent",
            "CEntityIdentity",
            "CPhysicsComponent",
            "CRenderComponent",
            "CDOTAGamerules",
            "CDOTAGameManager",
            "CDOTASpectatorGraphManager",
            "CPlayerLocalData",
            "CPlayer_CameraServices",
            "CDOTAGameRules"
        };
    public Necronomicon(string path)
    {
        _inputStreamSource = new InputStreamSource(path);
        _frameSkeletons = new HashSet<FrameSkeleton>();
        _classInfos = new HashSet<ClassInfo>();
        _serializers = new Dictionary<string, Serializer>();
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
            if (frameSkeleton.FrameCommand == EDemoCommands.DemSendTables)
            {
                CDemoSendTables? sendTables = frameSkeleton.GetAsProtobuf<CDemoSendTables>(frameSkeleton.FrameCommand);
                if (sendTables != null)
                {
                    BitReaderWrapper reader = new BitReaderWrapper(sendTables.Data.ToArray());
                    var dataSize = reader.ReadVarUInt32();
                    byte[] byteBuffer = new byte[dataSize];
                    Span<byte> messageSpan = byteBuffer;
                    reader.ReadToSpanBuffer(messageSpan);
                    var flattenedSerializer = CSVCMsg_FlattenedSerializer.Parser.ParseFrom(byteBuffer);

                    Dictionary<string, FieldType> fieldTypes = new Dictionary<string, FieldType>();
                    foreach (var serializer in flattenedSerializer.Serializers)
                    {
                        Serializer newSerializer = new Serializer(flattenedSerializer.Symbols[serializer.SerializerNameSym], serializer.SerializerVersion);
                        foreach (var fieldIndex in serializer.FieldsIndex)
                        {
                            Field newField = new Field(flattenedSerializer, flattenedSerializer.Fields[fieldIndex]);
                            if (!fieldTypes.ContainsKey(newField.VarType))
                            {
                                fieldTypes[newField.VarType] = FieldType.Parse(newField.VarType);
                            }

                            newField.FieldType = fieldTypes[newField.VarType];

                            if (!string.IsNullOrEmpty(newField.SerializerName) && _serializers.TryGetValue(newField.SerializerName, out var fieldSerializer))
                            {
                                newField.Serializer = fieldSerializer;
                            }

                            // // apply any build-specific patches to the field
                            // foreach (var patch in patches)
                            // {
                            //     patch.Patch(field);
                            // }

                            if (newField.Serializer != null)
                            {
                                if (newField.FieldType.Pointer || pointerTypes.Contains(newField.FieldType.BaseType))
                                {
                                    newField.Model = FieldModel.FixedTable;
                                }
                                else
                                {
                                    newField.Model = FieldModel.VariableTable;
                                }
                            }
                            else if (newField.FieldType.Count > 0 && newField.FieldType.BaseType != "char")
                            {
                                newField.Model = FieldModel.FixedArray;
                            }
                            else if (newField.FieldType.BaseType == "CUtlVector" || newField.FieldType.BaseType == "CNetworkUtlVectorBase")
                            {
                                newField.Model = FieldModel.VariableArray;
                            }
                            else
                            {
                                newField.Model = FieldModel.Simple;
                            }

                            newSerializer.Fields.Add(newField);
                        }

                        _serializers[newSerializer.Name] = newSerializer;
                    }
                }
            }
            if (frameSkeleton.FrameCommand == EDemoCommands.DemClassInfo)
            {
                CDemoClassInfo? classInfo = frameSkeleton.GetAsProtobuf<CDemoClassInfo>(frameSkeleton.FrameCommand);
                if (classInfo != null)
                {
                    foreach (var infoClass in classInfo.Classes)
                    {
                        var classId = infoClass.ClassId;
                        var networkName = infoClass.NetworkName;

                        if (!_classInfos.Any(ci => ci.Id == classId))
                        {
                            _classInfos.Add(new ClassInfo(classId, networkName, "serializer"));
                        }

                        Debug.WriteLine($"ClassID: {classId} - Network Name: {networkName}");
                    }
                }
            }
            if (frameSkeleton.FrameCommand == EDemoCommands.DemPacket)
            {
                CDemoPacket? packet = frameSkeleton.GetAsProtobuf<CDemoPacket>(frameSkeleton.FrameCommand);

                if (packet != null)
                {
                    EmbeddedMessage embeddedMessage = new EmbeddedMessage(packet.Data);
                    embeddedMessage.ParseMessages();
                }
            }
            // Debug.WriteLine($"Tick {frameSkeleton.FrameTick}");
            // Debug.WriteLine($"Command {frameSkeleton.FrameCommand}");
        }
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
