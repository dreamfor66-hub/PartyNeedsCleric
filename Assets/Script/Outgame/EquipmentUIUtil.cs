using UnityEngine;

public static class EquipmentUIUtil
{
    public static Color GetFrameColor(EquipmentRarity r)
    {
        var gv = GameVariables.Instance;
        if (gv == null) return new Color(0.75f, 0.75f, 0.75f, 1f);
        return gv.GetEquipmentFrameColor(r);
    }

    public static Color GetNameColor(EquipmentRarity r)
    {
        var gv = GameVariables.Instance;
        if (gv == null) return new Color(0.75f, 0.75f, 0.75f, 1f);
        return gv.GetEquipmentNameColor(r);
    }
}