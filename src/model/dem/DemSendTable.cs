using System.Diagnostics;
using necronomicon.model.engine;
using necronomicon.source;
using Steam.Protos.Dota2;

namespace necronomicon.model.dem;

public class DemSendTables
{
    private readonly Necronomicon _parser;
    public DemSendTables(Necronomicon parser)
    {
        _parser = parser;
        _parser.Callbacks.OnDemSendTables.Add(OnCDemoSendTables);
    }

    private HashSet<string> pointerTypes = new HashSet<string>
        {
            "PhysicsRagdollPose_t",
            "CBodyComponent",
            "CLightComponent",
            "CEntityIdentity",
            "CPhysicsComponent",
            "CRenderComponent",
            "CDOTAGamerules",
            "CDOTAGameManager",
            "CDOTASpectatorGraphManager",
            "CPlayerLocalData",
            "CPlayer_CameraServices",
            "CDOTAGameRules",
        };
        
    private HashSet<string> vectorTypes = new HashSet<string>
        {
            "CUtlVector",
            "CNetworkUtlVectorBase",
            "CUtlVectorEmbeddedNetworkVar",
        };

    public async Task OnCDemoSendTables(CDemoSendTables sendTables)
    {
        var data = sendTables.Data.Span;

        if (!InputStreamSource.TryReadVarInt32(ref data, out var dataSize))
            throw new Exception(); // TODO Specify

        // ?
        Debug.Assert(data.Length == dataSize);

        var flattenedSerializer = CSVCMsg_FlattenedSerializer.Parser.ParseFrom(data[..dataSize]);

        var patches = new List<FieldPatch>();
        foreach (var patch in FieldPatches.Patches)
        {
            if (patch.ShouldApply(_parser.GameBuild))
            {
                patches.Add(patch);
            }
        }

        Dictionary<int, Field> fields = new Dictionary<int, Field>();
        Dictionary<string, FieldType> fieldTypes = new Dictionary<string, FieldType>();
        foreach (var serializer in flattenedSerializer.Serializers)
        {
            Serializer newSerializer = new Serializer(flattenedSerializer.Symbols[serializer.SerializerNameSym], serializer.SerializerVersion);

            foreach (var fieldIndex in serializer.FieldsIndex)
            {
                try
                {
                    if (!fields.ContainsKey(fieldIndex))
                    {
                        Field newField = new Field(flattenedSerializer, flattenedSerializer.Fields[fieldIndex]);

                        if (!fieldTypes.ContainsKey(newField.VarType))
                        {
                            fieldTypes[newField.VarType] = FieldType.Parse(newField.VarType);
                        }

                        newField.FieldType = fieldTypes[newField.VarType];

                        if (!string.IsNullOrEmpty(newField.SerializerName) && _parser.Serializers.TryGetValue(newField.SerializerName, out var fieldSerializer))
                        {
                            newField.Serializer = fieldSerializer;
                        }

                        // apply any build-specific patches to the field
                        foreach (var patch in patches)
                        {
                            patch.Patch(newField);
                        }

                        if (newField.Serializer != null)
                        {
                            if (newField.FieldType.Pointer || pointerTypes.Contains(newField.FieldType.BaseType))
                            {
                                newField.SetModel(FieldModel.FixedTable);
                            }
                            else
                            {
                                newField.SetModel(FieldModel.VariableTable);
                            }
                        }
                        else if (newField.FieldType.Count > 0 && newField.FieldType.BaseType != "char")
                        {
                            newField.SetModel(FieldModel.FixedArray);
                        }
                        else if (vectorTypes.Contains(newField.FieldType.BaseType))
                        {
                            newField.SetModel(FieldModel.VariableArray);
                        }
                        else
                        {
                            newField.SetModel(FieldModel.Simple);
                        }
                        fields[fieldIndex] = newField;
                    }
                }
                catch (Exception ex)
                {
                    throw new NecronomiconException($"error {ex.Message}", ex);
                }



                newSerializer.Fields.Add(fields[fieldIndex]);
            }

            _parser.Serializers[newSerializer.Name] = newSerializer;
        }

        await Task.CompletedTask;
    }
}
