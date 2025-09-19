using System.Text.Json.Serialization;

namespace Benchmarks.Necronomicon.ReplayAnalysis;

public class HeroPosition
{
    [JsonPropertyName("hero_name")]
    public string HeroName { get; set; } = string.Empty;

    [JsonPropertyName("frame_tick")]
    public int FrameTick { get; set; }

    [JsonPropertyName("cell_x")]
    public ulong CellX { get; set; }

    [JsonPropertyName("cell_y")]
    public ulong CellY { get; set; }

    [JsonPropertyName("cell_z")]
    public ulong CellZ { get; set; }

    [JsonPropertyName("vec_x")]
    public float VecX { get; set; }

    [JsonPropertyName("vec_y")]
    public float VecY { get; set; }

    [JsonPropertyName("vec_z")]
    public float VecZ { get; set; }
    public HeroPosition(string name, int frameTick, Dictionary<string, object> cBodyComponentDictionary)
    {
        HeroName = name;
        FrameTick = frameTick;

        CellX = (ulong)(cBodyComponentDictionary["m_cellX"] ?? 0);
        CellY = (ulong)(cBodyComponentDictionary["m_cellY"] ?? 0);
        CellZ = (ulong)(cBodyComponentDictionary["m_cellZ"] ?? 0);
        VecX = (float)(cBodyComponentDictionary["m_vecX"] ?? 0);
        VecY = (float)(cBodyComponentDictionary["m_vecY"] ?? 0);
        VecZ = (float)(cBodyComponentDictionary["m_vecZ"] ?? 0);
    }
}
