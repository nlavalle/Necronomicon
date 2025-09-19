using necronomicon.model.engine;
using Steam.Protos.Dota2;

namespace Benchmarks.Necronomicon.ReplayAnalysis;

public class ItemUse
{
    public int FrameTick { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public float Timestamp { get; set; }
    public float TimestampRaw { get; set; }

    public ItemUse(CMsgDOTACombatLogEntry itemUseEntry, int frameTick, StringTable combatLogNames)
    {
        FrameTick = frameTick;
        Timestamp = itemUseEntry.Timestamp;
        TimestampRaw = itemUseEntry.TimestampRaw;
        if (combatLogNames.Items.Count >= itemUseEntry.AttackerName)
        {
            var itemAttackerName = combatLogNames.Items[(int)itemUseEntry.AttackerName];
            ItemName = itemAttackerName?.Key ?? "UNKNOWN";
        }
        if (combatLogNames.Items.Count >= itemUseEntry.InflictorName)
        {
            var inflictorName = combatLogNames.Items[(int)itemUseEntry.InflictorName];
            UserName = inflictorName?.Key ?? "UNKNOWN";
        }
    }
}