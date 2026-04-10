using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EncounterGenerator
{
    readonly StageData stageData;

    public EncounterGenerator(StageData stageData)
    {
        this.stageData = stageData;
    }

    public void PopulateEncounter(GeneratedRoom room)
    {
        room.waveCount = ResolveWaveCount(room.roomType);
        room.budget = stageData.runConfig.GetBudgetForFloor(room.floorNumber, room.roomType);
        room.distributionType = (SpawnDistributionType)Random.Range(0, 5);
        room.runtimeMap.waves = new List<WaveData>();

        if (room.roomType == StageRoomType.Boss)
        {
            room.runtimeMap.waves.Add(BuildBossWave(room));
            return;
        }

        var encounterCounts = new Dictionary<CharacterData, int>();
        int remainingBudget = room.budget;

        for (int waveIndex = 0; waveIndex < room.waveCount; waveIndex++)
        {
            int wavesLeft = room.waveCount - waveIndex;
            int waveBudget = Mathf.Max(1, remainingBudget / wavesLeft);
            if (waveIndex == room.waveCount - 1)
                waveBudget = remainingBudget;

            var wave = BuildBudgetWave(room, waveBudget, encounterCounts, waveIndex);
            room.runtimeMap.waves.Add(wave);
            remainingBudget = Mathf.Max(0, remainingBudget - ComputeWaveCost(wave));
        }
    }

    int ResolveWaveCount(StageRoomType roomType)
    {
        Vector2Int range = stageData.runConfig.GetWaveRange(roomType);
        if (range.y < range.x)
            range.y = range.x;
        return Random.Range(range.x, range.y + 1);
    }

    WaveData BuildBossWave(GeneratedRoom room)
    {
        var bossProfile = stageData.enemySpawnProfiles
            .Where(x => x != null && x.characterData != null && (x.tags & EnemySpawnTag.Boss) != 0 && x.availableFromFloor <= room.floorNumber)
            .OrderBy(x => x.cost)
            .LastOrDefault();

        if (bossProfile == null)
        {
            bossProfile = stageData.enemySpawnProfiles
                .Where(x => x != null && x.characterData != null)
                .OrderBy(x => x.cost)
                .LastOrDefault();
        }

        if (bossProfile == null)
        {
            return new WaveData
            {
                holdType = WaveHoldType.HoldUntilCount,
                value = 0f,
                spawns = new List<SpawnEntry>()
            };
        }

        var spawns = new List<SpawnEntry>();
        var occupied = new List<Vector2>();
        Vector2 spawnPosition = ResolveSpawnPosition(room, occupied, room.distributionType, bossProfile.characterData.radius);
        spawns.Add(new SpawnEntry { data = bossProfile.characterData, position = spawnPosition });
        return new WaveData
        {
            holdType = WaveHoldType.HoldUntilCount,
            value = 0f,
            spawns = spawns
        };
    }

    WaveData BuildBudgetWave(GeneratedRoom room, int waveBudget, Dictionary<CharacterData, int> encounterCounts, int waveIndex)
    {
        var spawns = new List<SpawnEntry>();
        var waveCounts = new Dictionary<CharacterData, int>();
        var occupied = new List<Vector2>();
        int targetBudget = Mathf.Max(1, waveBudget);
        var exactComposition = BuildExactComposition(room, targetBudget, encounterCounts, waveIndex);

        if (exactComposition == null || exactComposition.Count == 0)
        {
            Debug.LogWarning($"[Encounter] Failed to find exact composition for {room.floorNumber}F {room.roomType} budget={targetBudget}");
            return new WaveData
            {
                holdType = WaveHoldType.HoldUntilCount,
                value = 0f,
                spawns = spawns
            };
        }

        for (int i = 0; i < exactComposition.Count; i++)
        {
            var profile = exactComposition[i];
            Vector2 spawnPosition = ResolveSpawnPosition(room, occupied, room.distributionType, profile.characterData.radius);
            spawns.Add(new SpawnEntry { data = profile.characterData, position = spawnPosition });
            occupied.Add(spawnPosition);
            encounterCounts[profile.characterData] = encounterCounts.TryGetValue(profile.characterData, out int totalCount) ? totalCount + 1 : 1;
            waveCounts[profile.characterData] = waveCounts.TryGetValue(profile.characterData, out int waveCount) ? waveCount + 1 : 1;
        }

        return new WaveData
        {
            holdType = WaveHoldType.HoldUntilCount,
            value = 0f,
            spawns = spawns
        };
    }

    List<EnemySpawnProfile> BuildExactComposition(GeneratedRoom room, int targetBudget, Dictionary<CharacterData, int> encounterCounts, int waveIndex)
    {
        var candidates = stageData.enemySpawnProfiles
            .Where(x => x != null && x.characterData != null)
            .Where(x => x.availableFromFloor <= room.floorNumber)
            .Where(x => (x.tags & EnemySpawnTag.Boss) == 0)
            .Where(x => x.cost <= targetBudget)
            .Where(x => !encounterCounts.TryGetValue(x.characterData, out int encounterCount) || encounterCount < x.maxCountPerEncounter)
            .Where(x => !ViolatesCompositionRules(x, room.floorNumber, encounterCounts, new Dictionary<CharacterData, int>(), waveIndex))
            .OrderBy(x => x.cost)
            .ToList();

        if (candidates.Count == 0)
            return null;

        var combinations = new List<List<EnemySpawnProfile>>();
        SearchExactCompositions(
            room,
            candidates,
            targetBudget,
            0,
            encounterCounts,
            new Dictionary<CharacterData, int>(),
            new List<EnemySpawnProfile>(),
            combinations,
            waveIndex);

        if (combinations.Count == 0)
            return null;

        float totalWeight = 0f;
        var weights = new List<float>(combinations.Count);
        for (int i = 0; i < combinations.Count; i++)
        {
            float weight = ScoreComposition(combinations[i], encounterCounts);
            weights.Add(weight);
            totalWeight += weight;
        }

        float roll = Random.value * totalWeight;
        for (int i = 0; i < combinations.Count; i++)
        {
            roll -= weights[i];
            if (roll <= 0f)
                return combinations[i];
        }

        return combinations[combinations.Count - 1];
    }

    void SearchExactCompositions(
        GeneratedRoom room,
        List<EnemySpawnProfile> candidates,
        int remainingBudget,
        int startIndex,
        Dictionary<CharacterData, int> encounterCounts,
        Dictionary<CharacterData, int> waveCounts,
        List<EnemySpawnProfile> current,
        List<List<EnemySpawnProfile>> results,
        int waveIndex)
    {
        if (remainingBudget == 0)
        {
            results.Add(new List<EnemySpawnProfile>(current));
            return;
        }

        if (results.Count >= 24 || current.Count >= 8)
            return;

        for (int i = startIndex; i < candidates.Count; i++)
        {
            var profile = candidates[i];
            if (profile.cost > remainingBudget)
                break;

            if (!CanAddProfile(room, profile, encounterCounts, waveCounts, waveIndex))
                continue;

            current.Add(profile);
            waveCounts[profile.characterData] = waveCounts.TryGetValue(profile.characterData, out int count) ? count + 1 : 1;
            SearchExactCompositions(room, candidates, remainingBudget - profile.cost, i, encounterCounts, waveCounts, current, results, waveIndex);

            if (waveCounts[profile.characterData] <= 1)
                waveCounts.Remove(profile.characterData);
            else
                waveCounts[profile.characterData]--;
            current.RemoveAt(current.Count - 1);
        }
    }

    bool CanAddProfile(GeneratedRoom room, EnemySpawnProfile profile, Dictionary<CharacterData, int> encounterCounts, Dictionary<CharacterData, int> waveCounts, int waveIndex)
    {
        if (encounterCounts.TryGetValue(profile.characterData, out int encounterCount) && encounterCount >= profile.maxCountPerEncounter)
            return false;

        if (waveCounts.TryGetValue(profile.characterData, out int waveCount) && waveCount >= profile.maxCountPerWave)
            return false;

        return !ViolatesCompositionRules(profile, room.floorNumber, encounterCounts, waveCounts, waveIndex);
    }

    float ScoreComposition(List<EnemySpawnProfile> composition, Dictionary<CharacterData, int> encounterCounts)
    {
        int totalCount = composition.Count;
        int uniqueCount = composition.Select(x => x.characterData).Distinct().Count();
        int rangedCount = composition.Count(x => (x.tags & EnemySpawnTag.Ranged) != 0);
        float repeatedPenalty = 0f;

        for (int i = 0; i < composition.Count; i++)
        {
            if (encounterCounts.TryGetValue(composition[i].characterData, out int repeated))
                repeatedPenalty += repeated * 0.35f;
        }

        float weight = 1f;
        weight += uniqueCount * 0.6f;
        weight += rangedCount > 0 ? 0.35f : 0f;
        weight += totalCount <= 4 ? 0.25f : 0f;
        weight -= repeatedPenalty;
        return Mathf.Max(0.05f, weight);
    }

    bool ViolatesCompositionRules(EnemySpawnProfile profile, int floorNumber, Dictionary<CharacterData, int> encounterCounts, Dictionary<CharacterData, int> waveCounts, int waveIndex)
    {
        if ((profile.tags & EnemySpawnTag.Summoner) != 0)
        {
            int summonerCount = encounterCounts.Keys.Count(x => HasTag(x, EnemySpawnTag.Summoner));
            if (summonerCount >= 1)
                return true;
        }

        if ((profile.tags & EnemySpawnTag.Ranged) != 0 && floorNumber <= 2)
        {
            int rangedCount = encounterCounts.Keys.Count(x => HasTag(x, EnemySpawnTag.Ranged));
            if (rangedCount >= 1 || waveIndex > 0)
                return true;
        }

        foreach (var other in encounterCounts.Keys)
        {
            if (profile.incompatibleWith != null && profile.incompatibleWith.Contains(other))
                return true;
        }

        return false;
    }

    EnemySpawnProfile FindProfile(CharacterData characterData)
    {
        return stageData.enemySpawnProfiles.FirstOrDefault(x => x != null && x.characterData == characterData);
    }

    bool HasTag(CharacterData characterData, EnemySpawnTag tag)
    {
        var profile = FindProfile(characterData);
        return profile != null && (profile.tags & tag) != 0;
    }

    Vector2 ResolveSpawnPosition(GeneratedRoom room, List<Vector2> occupied, SpawnDistributionType distributionType, float radius)
    {
        int attempts = 24;
        for (int i = 0; i < attempts; i++)
        {
            Vector2 candidate = PickPatternedPoint(room.template, distributionType);
            if (IsValidSpawnPosition(room, candidate, occupied, radius))
                return candidate;
        }

        return room.template.playerStartPoints.Length > 0 ? room.template.playerStartPoints[0] + Vector2.up * 220f : new Vector2(0f, 160f);
    }

    Vector2 PickPatternedPoint(RoomTemplateData template, SpawnDistributionType distributionType)
    {
        var area = template.spawnAreas != null && template.spawnAreas.Count > 0
            ? template.spawnAreas[Random.Range(0, template.spawnAreas.Count)].area
            : new Rect(-(template.mapSize.x * 0.5f), -(template.mapSize.y * 0.5f), template.mapSize.x, template.mapSize.y);

        float minX = area.xMin;
        float maxX = area.xMax;
        float minY = area.yMin;
        float maxY = area.yMax;

        switch (distributionType)
        {
            case SpawnDistributionType.SplitSides:
                if (Random.value < 0.5f) maxX = Mathf.Lerp(minX, maxX, 0.4f);
                else minX = Mathf.Lerp(minX, maxX, 0.6f);
                break;
            case SpawnDistributionType.FocusOneSide:
                if (Random.value < 0.5f) maxX = Mathf.Lerp(minX, maxX, 0.35f);
                else minX = Mathf.Lerp(minX, maxX, 0.65f);
                minY = Mathf.Lerp(minY, maxY, 0.35f);
                break;
            case SpawnDistributionType.OuterRing:
                if (Random.value < 0.5f) minY = Mathf.Lerp(minY, maxY, 0.7f);
                else
                {
                    if (Random.value < 0.5f) maxX = Mathf.Lerp(minX, maxX, 0.2f);
                    else minX = Mathf.Lerp(minX, maxX, 0.8f);
                }
                break;
            case SpawnDistributionType.CenterPress:
                minX = Mathf.Lerp(minX, maxX, 0.3f);
                maxX = Mathf.Lerp(minX, maxX, 0.7f);
                minY = Mathf.Lerp(minY, maxY, 0.45f);
                break;
        }

        return new Vector2(Random.Range(minX, maxX), Random.Range(minY, maxY));
    }

    bool IsValidSpawnPosition(GeneratedRoom room, Vector2 candidate, List<Vector2> occupied, float radius)
    {
        float halfWidth = room.template.mapSize.x * 0.5f;
        float halfHeight = room.template.mapSize.y * 0.5f;
        if (candidate.x < -halfWidth + radius || candidate.x > halfWidth - radius)
            return false;
        if (candidate.y < -halfHeight + radius || candidate.y > halfHeight - radius)
            return false;

        if (room.template.playerStartPoints != null)
        {
            for (int i = 0; i < room.template.playerStartPoints.Length; i++)
            {
                if (Vector2.Distance(candidate, room.template.playerStartPoints[i]) < stageData.runConfig.minSpawnDistanceFromPlayers)
                    return false;
            }
        }

        for (int i = 0; i < occupied.Count; i++)
        {
            if (Vector2.Distance(candidate, occupied[i]) < stageData.runConfig.minSpawnDistanceBetweenEnemies)
                return false;
        }

        if (room.template.fixedObstacles != null)
        {
            float obstacleRadius = 16f + stageData.runConfig.obstacleAvoidancePadding + radius;
            for (int i = 0; i < room.template.fixedObstacles.Count; i++)
            {
                if (Vector2.Distance(candidate, room.template.fixedObstacles[i]) < obstacleRadius)
                    return false;
            }
        }

        return true;
    }

    int ComputeWaveCost(WaveData wave)
    {
        int cost = 0;
        for (int i = 0; i < wave.spawns.Count; i++)
        {
            var profile = FindProfile(wave.spawns[i].data);
            if (profile != null)
                cost += profile.cost;
        }

        return cost;
    }
}
