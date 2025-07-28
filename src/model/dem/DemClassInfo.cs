using System.Numerics;
using necronomicon.model.engine;
using Steam.Protos.Dota2;

namespace necronomicon.model.dem;

public class DemClassInfo
{
    private readonly Necronomicon _parser;
    public DemClassInfo(Necronomicon parser)
    {
        _parser = parser;
        _parser.Callbacks.OnDemClassInfo.Add(OnCDemoClassInfo);
    }

    public async Task OnCDemoClassInfo(CDemoClassInfo classInfo)
    {
        foreach (var infoClass in classInfo.Classes)
        {
            var classId = infoClass.ClassId;
            var networkName = infoClass.NetworkName;

            if (_parser.Serializers.ContainsKey(networkName))
            {
                var newClass = new Class(classId, networkName, _parser.Serializers[networkName]);
                _parser.ClassInfos[classId] = new ClassInfo(classId, networkName, "serializer");
                _parser.ClassesById[classId] = newClass;
                _parser.ClassesByName[networkName] = newClass;
            }
            else
            {
                throw new NecronomiconException($"Missing the serializer for: {networkName}");
            }

            // Debug.WriteLine($"ClassID: {classId} - Network Name: {networkName}");
        }

        _parser.ClassIdSize = (uint)BitOperations.Log2((uint)_parser.ClassesById.Count) + 1;

        _parser.UpdateInstanceBaseline();

        await Task.CompletedTask;
    }
}