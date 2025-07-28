using Steam.Protos.Dota2;

namespace necronomicon.model.engine;

public class FieldPatch
{
    public uint MinBuild { get; }
    public uint MaxBuild { get; }
    public Action<Field> Patch { get; }
    public FieldPatch(uint minBuild, uint max_classes, Action<Field> patch)
    {
        MinBuild = minBuild;
        MaxBuild = max_classes;
        Patch = patch;
    }

    public bool ShouldApply(uint build)
    {
        if (MinBuild == 0 && MaxBuild == 0)
            return true;

        return build >= MinBuild && build <= MaxBuild;
    }
}

public static class FieldPatches
{
    public static readonly List<FieldPatch> Patches = new()
        {
            new FieldPatch(0, 990, f =>
            {
                switch (f.VarName)
                {
                    case "angExtraLocalAngles":
                    case "angLocalAngles":
                    case "m_angInitialAngles":
                    case "m_angRotation":
                    case "m_ragAngles":
                    case "m_vLightDirection":
                        f.Encoder = f.ParentName == "CBodyComponentBaseAnimatingOverlay" ? "qangle_pitch_yaw" : "QAngle";
                        break;

                    case "dirPrimary":
                    case "localSound":
                    case "m_flElasticity":
                    case "m_location":
                    case "m_poolOrigin":
                    case "m_ragPos":
                    case "m_vecEndPos":
                    case "m_vecLadderDir":
                    case "m_vecPlayerMountPositionBottom":
                    case "m_vecPlayerMountPositionTop":
                    case "m_viewtarget":
                    case "m_WorldMaxs":
                    case "m_WorldMins":
                    case "origin":
                    case "vecLocalOrigin":
                        f.Encoder = "coord";
                        break;

                    case "m_vecLadderNormal":
                        f.Encoder = "normal";
                        break;
                }
            }),

            new FieldPatch(0, 954, f =>
            {
                switch (f.VarName)
                {
                    case "m_flMana":
                    case "m_flMaxMana":
                        f.LowValue = null;
                        f.HighValue = 8192.0f;
                        break;
                }
            }),

            new FieldPatch(1016, 1027, f =>
            {
                switch (f.VarName)
                {
                    case "m_bItemWhiteList":
                    case "m_bWorldTreeState":
                    case "m_iPlayerIDsInControl":
                    case "m_iPlayerSteamID":
                    case "m_ulTeamBannerLogo":
                    case "m_ulTeamBaseLogo":
                    case "m_ulTeamLogo":
                        f.Encoder = "fixed64";
                        break;
                }
            }),

            new FieldPatch(0, 0, f =>
            {
                switch (f.VarName)
                {
                    case "m_flSimulationTime":
                    case "m_flAnimTime":
                        f.Encoder = "simtime";
                        break;
                    case "m_flRuneTime":
                        f.Encoder = "runetime";
                        break;
                }
            })
        };
}