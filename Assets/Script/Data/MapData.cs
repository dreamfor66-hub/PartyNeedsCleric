using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

public enum WaveHoldType { HoldUntilCount, HoldUntilTime }

[System.Serializable]
public class SpawnEntry
{
    public CharacterData data;
    public Vector2 position;
}

[System.Serializable]
public class WaveData
{
    public WaveHoldType holdType;
    public float value;                   // count or time
    public List<SpawnEntry> spawns = new();
}

[CreateAssetMenu(menuName = "Game/MapData")]
public class MapData : ScriptableObject
{
    [Header("Map Size")]
    public Vector2 size = new Vector2(300, 500);

    [Header("Ω√¿€¡° 123")]
    [TableList(AlwaysExpanded = false)]
    public Vector2[] playerStartPoints = new Vector2[3];

    [Header("Collisions")]
    [TableList(AlwaysExpanded = false)]
    public List<Vector2> collisions = new();

    [Header("Waves")]
    [TableList(AlwaysExpanded = false)]
    public List<WaveData> waves = new();

    [Header("Generated Meta")]
    [TableList(AlwaysExpanded = true)]
    [ReadOnly]
    public GeneratedMapMeta generatedMeta = new GeneratedMapMeta();

    public void RebuildGeneratedMeta()
    {
        generatedMeta = MapFeatureExtractor.Extract(this);
    }

    public void RebuildGeneratedMetaIfNeeded()
    {
        if (generatedMeta == null)
            generatedMeta = new GeneratedMapMeta();

        RebuildGeneratedMeta();
    }

    public float GetFeature(string key, float defaultValue = 0f)
    {
        if (generatedMeta == null)
            return defaultValue;

        return generatedMeta.Get(key, defaultValue);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        RebuildGeneratedMetaIfNeeded();
    }
#endif
}