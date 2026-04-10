using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/StageData")]
public class StageData : ScriptableObject
{
    [TableList]
    public List<FloorData> floors;

    [Title("Procedural Stage Flow")]
    public bool useProceduralStageFlow = true;
    public StageRunConfig runConfig = new();
    [TableList]
    public List<RoomTemplateData> roomTemplates = new();
    [TableList]
    public List<RoomTypeWeightData> roomTypeWeights = new();
    [TableList]
    public List<EnemySpawnProfile> enemySpawnProfiles = new();
    [TableList]
    public List<RewardProfile> rewardProfiles = new();

    public void EnsureProceduralDefaults()
    {
        floors ??= new List<FloorData>();
        runConfig ??= new StageRunConfig();
        roomTemplates ??= new List<RoomTemplateData>();
        roomTypeWeights ??= new List<RoomTypeWeightData>();
        enemySpawnProfiles ??= new List<EnemySpawnProfile>();
        rewardProfiles ??= new List<RewardProfile>();

        runConfig.EnsureDefaults();

        if (roomTemplates.Count == 0)
            BootstrapRoomTemplatesFromLegacyMaps();

        if (roomTypeWeights.Count == 0)
            BootstrapDefaultRoomWeights();

        if (enemySpawnProfiles.Count == 0)
            BootstrapEnemyProfilesFromLegacyMaps();

        if (rewardProfiles.Count == 0)
            BootstrapRewardProfiles();
    }

    void BootstrapRoomTemplatesFromLegacyMaps()
    {
        var templateTypes = new[]
        {
            RoomTemplateType.SmallCircle,
            RoomTemplateType.CentralObstacle,
            RoomTemplateType.BossArena
        };

        var uniqueMaps = floors
            .Where(f => f != null && f.maps != null)
            .SelectMany(f => f.maps)
            .Where(m => m != null)
            .Distinct()
            .Take(3)
            .ToList();

        for (int i = 0; i < uniqueMaps.Count; i++)
        {
            bool canUseForBoss = i == uniqueMaps.Count - 1;
            roomTemplates.Add(RoomTemplateData.FromMapData(uniqueMaps[i], templateTypes[Mathf.Min(i, templateTypes.Length - 1)], canUseForBoss));
        }

        while (roomTemplates.Count < 3)
        {
            if (roomTemplates.Count == 0)
                roomTemplates.Add(RoomTemplateData.FromMapData(null, RoomTemplateType.SmallCircle, false));
            else if (roomTemplates.Count == 1)
                roomTemplates.Add(RoomTemplateData.FromMapData(null, RoomTemplateType.CentralObstacle, false));
            else
                roomTemplates.Add(RoomTemplateData.FromMapData(null, RoomTemplateType.BossArena, true));
        }
    }

    void BootstrapDefaultRoomWeights()
    {
        roomTypeWeights.Add(new RoomTypeWeightData
        {
            floorMin = 1,
            floorMax = 4,
            weights = new List<RoomTypeWeightEntry>
            {
                new RoomTypeWeightEntry { roomType = StageRoomType.NormalCombat, weight = 0.8f },
                new RoomTypeWeightEntry { roomType = StageRoomType.Elite, weight = 0.2f }
            }
        });

        roomTypeWeights.Add(new RoomTypeWeightData
        {
            floorMin = 5,
            floorMax = 999,
            weights = new List<RoomTypeWeightEntry>
            {
                new RoomTypeWeightEntry { roomType = StageRoomType.NormalCombat, weight = 0.7f },
                new RoomTypeWeightEntry { roomType = StageRoomType.Elite, weight = 0.3f }
            }
        });
    }

    void BootstrapEnemyProfilesFromLegacyMaps()
    {
        var uniqueEnemies = floors
            .Where(f => f != null && f.maps != null)
            .SelectMany(f => f.maps)
            .Where(m => m != null && m.waves != null)
            .SelectMany(m => m.waves)
            .Where(w => w != null && w.spawns != null)
            .SelectMany(w => w.spawns)
            .Select(s => s.data)
            .Where(c => c != null)
            .Distinct()
            .ToList();

        foreach (var enemy in uniqueEnemies)
        {
            enemySpawnProfiles.Add(new EnemySpawnProfile
            {
                id = enemy.name,
                characterData = enemy,
                cost = EstimateEnemyCost(enemy),
                availableFromFloor = enemy.isBoss ? runConfig.bossFloorInterval : 1,
                maxCountPerEncounter = enemy.isBoss ? 1 : enemy.characterType == CharacterType.Ranged ? 3 : 5,
                maxCountPerWave = enemy.isBoss ? 1 : enemy.characterType == CharacterType.Ranged ? 2 : 4,
                tags = EstimateEnemyTags(enemy)
            });
        }
    }

    void BootstrapRewardProfiles()
    {
        rewardProfiles.Add(new RewardProfile { roomType = StageRoomType.NormalCombat, candidateCount = 1, commonWeight = 1f, rareWeight = 0.1f, guaranteedReward = false });
        rewardProfiles.Add(new RewardProfile { roomType = StageRoomType.Elite, candidateCount = 2, commonWeight = 0.8f, rareWeight = 0.4f, guaranteedReward = true });
        rewardProfiles.Add(new RewardProfile { roomType = StageRoomType.Boss, candidateCount = 3, commonWeight = 0.5f, rareWeight = 1f, guaranteedReward = true });
    }

    static int EstimateEnemyCost(CharacterData enemy)
    {
        if (enemy == null)
            return 1;

        if (enemy.isBoss)
            return 999;

        if (enemy.characterType == CharacterType.Ranged)
            return 3;

        if (enemy.baseHp >= 1200)
            return 4;

        if (enemy.baseHp >= 800)
            return 2;

        return 1;
    }

    static EnemySpawnTag EstimateEnemyTags(CharacterData enemy)
    {
        if (enemy == null)
            return EnemySpawnTag.Basic;

        EnemySpawnTag tags = EnemySpawnTag.Basic;

        if (enemy.characterType == CharacterType.Ranged)
            tags |= EnemySpawnTag.Ranged;

        if (enemy.baseHp >= 1200 || enemy.radius >= 20f)
            tags |= EnemySpawnTag.Tank;

        if (enemy.isBoss)
            tags |= EnemySpawnTag.Boss;

        return tags;
    }
}

[Serializable]
public class FloorData
{
    public List<MapData> maps;
    public float multiplier = 1f;
}
