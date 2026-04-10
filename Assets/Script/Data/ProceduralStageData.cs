using System;
using System.Collections.Generic;
using UnityEngine;

public enum StageRoomType
{
    NormalCombat,
    Elite,
    Boss,
    Recovery,
    SkillReward,
    UpgradeReward,
    RelicReward
}

public enum RoomTemplateType
{
    SmallCircle,
    Horizontal,
    Vertical,
    CentralObstacle,
    SideObstacles,
    BossArena
}

[Flags]
public enum EnemySpawnTag
{
    None = 0,
    Basic = 1 << 0,
    Ranged = 1 << 1,
    Tank = 1 << 2,
    Summoner = 1 << 3,
    Elite = 1 << 4,
    Boss = 1 << 5
}

public enum SpawnDistributionType
{
    RadialSpread,
    SplitSides,
    FocusOneSide,
    OuterRing,
    CenterPress
}

[Serializable]
public class StageRunConfig
{
    public int totalFloors = 10;
    public int bossFloorInterval = 5;
    public int mapWidth = 7;
    public int startNodeCount = 3;
    public int pathCount = 6;
    public int maxNodesPerFloor = 3;
    public int startBudget = 5;
    public int budgetIncreasePerFloor = 2;
    public int routeChoiceCount = 2;
    public int recentRouteHistorySize = 3;
    [Range(0f, 1f)] public float repeatedRoutePenalty = 0.35f;
    public float minSpawnDistanceFromPlayers = 140f;
    public float minSpawnDistanceBetweenEnemies = 48f;
    public float obstacleAvoidancePadding = 20f;
    public List<RoomWaveRange> waveRanges = new();

    public Vector2Int GetWaveRange(StageRoomType roomType)
    {
        for (int i = 0; i < waveRanges.Count; i++)
        {
            if (waveRanges[i].roomType == roomType)
                return waveRanges[i].range;
        }

        return roomType switch
        {
            StageRoomType.Elite => new Vector2Int(1, 2),
            StageRoomType.Boss => new Vector2Int(1, 1),
            _ => new Vector2Int(1, 3),
        };
    }

    public int GetBudgetForFloor(int floorNumber, StageRoomType roomType)
    {
        int budget = startBudget + Mathf.Max(0, floorNumber - 1) * budgetIncreasePerFloor;
        if (roomType == StageRoomType.Elite)
            budget += 3;
        return Mathf.Max(1, budget);
    }

    public void EnsureDefaults()
    {
        if (waveRanges == null)
            waveRanges = new List<RoomWaveRange>();

        mapWidth = Mathf.Max(3, mapWidth);
        startNodeCount = Mathf.Clamp(startNodeCount, 1, mapWidth);
        pathCount = Mathf.Max(startNodeCount, pathCount);
        maxNodesPerFloor = Mathf.Clamp(maxNodesPerFloor, 1, mapWidth);
        totalFloors = Mathf.Max(1, totalFloors);

        if (waveRanges.Count == 0)
        {
            waveRanges.Add(new RoomWaveRange { roomType = StageRoomType.NormalCombat, range = new Vector2Int(1, 3) });
            waveRanges.Add(new RoomWaveRange { roomType = StageRoomType.Elite, range = new Vector2Int(1, 2) });
            waveRanges.Add(new RoomWaveRange { roomType = StageRoomType.Boss, range = new Vector2Int(1, 1) });
        }
    }
}

[Serializable]
public class RoomWaveRange
{
    public StageRoomType roomType;
    public Vector2Int range = new Vector2Int(1, 1);
}

[Serializable]
public class SpawnAreaData
{
    public Rect area = new Rect(-100f, -100f, 200f, 200f);
}

[Serializable]
public class RoomTemplateData
{
    public string templateId = "template";
    public RoomTemplateType templateType;
    public Vector2 mapSize = new Vector2(500f, 500f);
    public Vector2[] playerStartPoints = Array.Empty<Vector2>();
    public List<SpawnAreaData> spawnAreas = new();
    public List<Vector2> fixedObstacles = new();
    public bool canUseForBoss;

    public static RoomTemplateData FromMapData(MapData map, RoomTemplateType templateType, bool canUseForBoss)
    {
        float inset = 48f;
        return new RoomTemplateData
        {
            templateId = map != null ? map.name : templateType.ToString(),
            templateType = templateType,
            mapSize = map != null ? map.size : new Vector2(500f, 500f),
            playerStartPoints = map != null && map.playerStartPoints != null && map.playerStartPoints.Length > 0
                ? (Vector2[])map.playerStartPoints.Clone()
                : new[] { new Vector2(-48f, -180f), new Vector2(48f, -180f), new Vector2(0f, -224f) },
            spawnAreas = new List<SpawnAreaData>
            {
                new SpawnAreaData
                {
                    area = map != null
                        ? new Rect(-(map.size.x * 0.5f) + inset, -(map.size.y * 0.5f) + inset, map.size.x - inset * 2f, map.size.y - inset * 2f)
                        : new Rect(-200f, -200f, 400f, 400f)
                }
            },
            fixedObstacles = map != null && map.collisions != null ? new List<Vector2>(map.collisions) : new List<Vector2>(),
            canUseForBoss = canUseForBoss
        };
    }
}

[Serializable]
public class RoomTypeWeightData
{
    public int floorMin = 1;
    public int floorMax = 999;
    public List<RoomTypeWeightEntry> weights = new();

    public float GetWeight(StageRoomType roomType)
    {
        for (int i = 0; i < weights.Count; i++)
        {
            if (weights[i].roomType == roomType)
                return Mathf.Max(0f, weights[i].weight);
        }

        return 0f;
    }
}

[Serializable]
public class RoomTypeWeightEntry
{
    public StageRoomType roomType;
    public float weight = 1f;
}

[Serializable]
public class EnemySpawnProfile
{
    public string id = "enemy";
    public CharacterData characterData;
    public int cost = 1;
    public int availableFromFloor = 1;
    public int maxCountPerEncounter = 99;
    public int maxCountPerWave = 99;
    public EnemySpawnTag tags = EnemySpawnTag.Basic;
    public List<CharacterData> incompatibleWith = new();
}

[Serializable]
public class RewardProfile
{
    public StageRoomType roomType;
    public int candidateCount = 1;
    public float commonWeight = 1f;
    public float rareWeight = 0.2f;
    public bool guaranteedReward;
}

[Serializable]
public class StageRouteChoice
{
    public StageRoomType roomType;
    public int targetFloor;

    public override string ToString()
    {
        return $"{targetFloor}F:{roomType}";
    }
}

public class GeneratedRoom
{
    public int floorNumber;
    public StageRoomType roomType;
    public RoomTemplateData template;
    public SpawnDistributionType distributionType;
    public int waveCount;
    public int budget;
    public MapData runtimeMap;
}

public class StageMapNode
{
    public int id;
    public int floorNumber;
    public int column;
    public StageRoomType roomType;
    public bool isEntry;
    public Vector2 uiPosition;
    public readonly List<StageMapNode> nextNodes = new();
    public readonly List<StageMapNode> previousNodes = new();

    public bool IsBoss => roomType == StageRoomType.Boss;
}

public class StageMap
{
    public readonly List<StageMapNode> nodes = new();
    public readonly List<StageMapNode> startNodes = new();
    public StageMapNode entryNode;
    public StageMapNode bossNode;

    public IEnumerable<StageMapNode> GetNodesOnFloor(int floorNumber)
    {
        for (int i = 0; i < nodes.Count; i++)
        {
            if (nodes[i].floorNumber == floorNumber)
                yield return nodes[i];
        }
    }
}
