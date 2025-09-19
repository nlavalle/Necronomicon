using necronomicon.model.engine;
using Steam.Protos.Dota2;

namespace Benchmarks.Necronomicon.ReplayAnalysis;

public class Death
{
    public int FrameTick { get; set; }
    public string DeathUnit { get; set; } = string.Empty;
    public string KillerUnit { get; set; } = string.Empty;
    public float Timestamp { get; set; }
    public float TimestampRaw { get; set; }

    public Death(CMsgDOTACombatLogEntry itemUseEntry, int frameTick, StringTable combatLogNames)
    {
        FrameTick = frameTick;
        Timestamp = itemUseEntry.Timestamp;
        TimestampRaw = itemUseEntry.TimestampRaw;
        if (combatLogNames.Items.Count >= itemUseEntry.TargetName)
        {
            var deathName = combatLogNames.Items[(int)itemUseEntry.TargetName];
            DeathUnit = deathName?.Key ?? "UNKNOWN";
        }
        if (combatLogNames.Items.Count >= itemUseEntry.AttackerName)
        {
            var killerName = combatLogNames.Items[(int)itemUseEntry.AttackerName];
            KillerUnit = killerName?.Key ?? "UNKNOWN";
        }
    }
}