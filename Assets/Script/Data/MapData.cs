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

    [Header("������ 123")]
    [TableList(AlwaysExpanded = false)]
    public Vector2[] playerStartPoints = new Vector2[3];

    [Header("Collisions")]
    [TableList(AlwaysExpanded = false)]
    public List<Vector2> collisions = new();

    [Header("Outer Boundary")]
    public bool useOuterBoundaryWalls = false;

    [Header("Waves")]
    [TableList(AlwaysExpanded = false)]
    public List<WaveData> waves = new();
}
