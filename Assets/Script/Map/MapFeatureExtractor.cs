using System.Collections.Generic;
using UnityEngine;

public static class MapFeatureExtractor
{
    private const float GridSize = 32f;

    public static GeneratedMapMeta Extract(MapData map)
    {
        var meta = new GeneratedMapMeta();

        if (map == null)
            return meta;

        var collisionSet = BuildCollisionSet(map);

        float lrSymmetry = CalculateLeftRightSymmetry(collisionSet);
        float tbSymmetry = CalculateTopBottomSymmetry(collisionSet);
        float noneSymmetry = Mathf.Clamp01(1f - Mathf.Max(lrSymmetry, tbSymmetry));

        meta.Set("layout.symmetry.lr", lrSymmetry);
        meta.Set("layout.symmetry.tb", tbSymmetry);
        meta.Set("layout.symmetry.none", noneSymmetry);

        meta.Set("obstacle.density", CalculateObstacleDensity(map, collisionSet));
        meta.Set("obstacle.centerBlock", HasCenterBlock(collisionSet) ? 1f : 0f);
        meta.Set("lane.count", CalculateLaneCount(map, collisionSet));
        meta.Set("start.safety", CalculateStartSafety(map));
        meta.Set("wave.count", map.waves != null ? map.waves.Count : 0f);

        CalculateSpawnTypeRatios(map, out float meleeRatio, out float rangedRatio);
        meta.Set("spawn.meleeRatio", meleeRatio);
        meta.Set("spawn.rangedRatio", rangedRatio);

        CalculateSpawnSpacingFeatures(map, out float spacingMean, out float spacingMin, out float clusterScore);
        meta.Set("spawn.spacing.mean", spacingMean);
        meta.Set("spawn.spacing.min", spacingMin);
        meta.Set("spawn.cluster.score", clusterScore);

        return meta;
    }

    private static HashSet<Vector2Int> BuildCollisionSet(MapData map)
    {
        var set = new HashSet<Vector2Int>();

        if (map == null || map.collisions == null)
            return set;

        for (int i = 0; i < map.collisions.Count; i++)
        {
            Vector2 p = map.collisions[i];
            set.Add(WorldToCell(p));
        }

        return set;
    }

    private static Vector2Int WorldToCell(Vector2 world)
    {
        int x = Mathf.RoundToInt(world.x / GridSize);
        int y = Mathf.RoundToInt(world.y / GridSize);
        return new Vector2Int(x, y);
    }

    private static float CalculateLeftRightSymmetry(HashSet<Vector2Int> collisionSet)
    {
        if (collisionSet == null || collisionSet.Count == 0)
            return 0f;

        int matched = 0;

        foreach (var cell in collisionSet)
        {
            var mirror = new Vector2Int(-cell.x, cell.y);
            if (collisionSet.Contains(mirror))
                matched++;
        }

        return (float)matched / collisionSet.Count;
    }

    private static float CalculateTopBottomSymmetry(HashSet<Vector2Int> collisionSet)
    {
        if (collisionSet == null || collisionSet.Count == 0)
            return 0f;

        int matched = 0;

        foreach (var cell in collisionSet)
        {
            var mirror = new Vector2Int(cell.x, -cell.y);
            if (collisionSet.Contains(mirror))
                matched++;
        }

        return (float)matched / collisionSet.Count;
    }

    private static float CalculateObstacleDensity(MapData map, HashSet<Vector2Int> collisionSet)
    {
        if (map == null)
            return 0f;

        int widthCells = Mathf.Max(1, Mathf.RoundToInt(map.size.x / GridSize));
        int heightCells = Mathf.Max(1, Mathf.RoundToInt(map.size.y / GridSize));
        int totalCells = Mathf.Max(1, widthCells * heightCells);

        int collisionCount = collisionSet != null ? collisionSet.Count : 0;
        return Mathf.Clamp01((float)collisionCount / totalCells);
    }

    private static bool HasCenterBlock(HashSet<Vector2Int> collisionSet)
    {
        if (collisionSet == null || collisionSet.Count == 0)
            return false;

        foreach (var cell in collisionSet)
        {
            if (Mathf.Abs(cell.x) <= 1 && Mathf.Abs(cell.y) <= 1)
                return true;
        }

        return false;
    }

    private static float CalculateLaneCount(MapData map, HashSet<Vector2Int> collisionSet)
    {
        if (map == null)
            return 1f;

        int halfW = Mathf.Max(1, Mathf.RoundToInt(map.size.x / GridSize / 2f));
        int halfH = Mathf.Max(1, Mathf.RoundToInt(map.size.y / GridSize / 2f));

        int bestLaneCount = 1;

        for (int y = -halfH; y <= halfH; y++)
        {
            int laneCount = 0;
            bool inOpenSegment = false;

            for (int x = -halfW; x <= halfW; x++)
            {
                bool blocked = collisionSet != null && collisionSet.Contains(new Vector2Int(x, y));

                if (!blocked)
                {
                    if (!inOpenSegment)
                    {
                        laneCount++;
                        inOpenSegment = true;
                    }
                }
                else
                {
                    inOpenSegment = false;
                }
            }

            if (laneCount > bestLaneCount)
                bestLaneCount = laneCount;
        }

        return bestLaneCount;
    }

    private static float CalculateStartSafety(MapData map)
    {
        if (map == null || map.playerStartPoints == null || map.playerStartPoints.Length == 0)
            return 0f;

        float diagonal = map.size.magnitude;
        if (diagonal <= 0.001f)
            diagonal = 1f;

        List<Vector2> allSpawns = new List<Vector2>();

        if (map.waves != null)
        {
            for (int i = 0; i < map.waves.Count; i++)
            {
                var wave = map.waves[i];
                if (wave == null || wave.spawns == null) continue;

                for (int j = 0; j < wave.spawns.Count; j++)
                    allSpawns.Add(wave.spawns[j].position);
            }
        }

        if (allSpawns.Count == 0)
            return 1f;

        float totalNormalized = 0f;
        int validStarts = 0;

        for (int i = 0; i < map.playerStartPoints.Length; i++)
        {
            Vector2 start = map.playerStartPoints[i];
            float minDist = float.MaxValue;

            for (int j = 0; j < allSpawns.Count; j++)
            {
                float dist = Vector2.Distance(start, allSpawns[j]);
                if (dist < minDist)
                    minDist = dist;
            }

            if (minDist < float.MaxValue)
            {
                totalNormalized += Mathf.Clamp01(minDist / diagonal);
                validStarts++;
            }
        }

        if (validStarts == 0)
            return 0f;

        return totalNormalized / validStarts;
    }

    private static void CalculateSpawnTypeRatios(MapData map, out float meleeRatio, out float rangedRatio)
    {
        meleeRatio = 0f;
        rangedRatio = 0f;

        if (map == null || map.waves == null)
            return;

        int meleeCount = 0;
        int rangedCount = 0;
        int totalTyped = 0;

        for (int i = 0; i < map.waves.Count; i++)
        {
            var wave = map.waves[i];
            if (wave == null || wave.spawns == null) continue;

            for (int j = 0; j < wave.spawns.Count; j++)
            {
                var spawn = wave.spawns[j];
                if (spawn == null || spawn.data == null) continue;

                if (spawn.data.characterType == CharacterType.Melee)
                {
                    meleeCount++;
                    totalTyped++;
                }
                else if (spawn.data.characterType == CharacterType.Ranged)
                {
                    rangedCount++;
                    totalTyped++;
                }
            }
        }

        if (totalTyped <= 0)
            return;

        meleeRatio = (float)meleeCount / totalTyped;
        rangedRatio = (float)rangedCount / totalTyped;
    }

    private static void CalculateSpawnSpacingFeatures(MapData map, out float spacingMean, out float spacingMin, out float clusterScore)
    {
        spacingMean = 0f;
        spacingMin = 0f;
        clusterScore = 0f;

        if (map == null || map.waves == null || map.waves.Count == 0)
            return;

        float diagonal = map.size.magnitude;
        if (diagonal <= 0.001f)
            diagonal = 1f;

        const float clusterThreshold = 96f;

        float distanceSum = 0f;
        float minDistance = float.MaxValue;
        int pairCount = 0;
        int clusterPairCount = 0;

        for (int i = 0; i < map.waves.Count; i++)
        {
            var wave = map.waves[i];
            if (wave == null || wave.spawns == null || wave.spawns.Count < 2) continue;

            for (int a = 0; a < wave.spawns.Count; a++)
            {
                for (int b = a + 1; b < wave.spawns.Count; b++)
                {
                    float dist = Vector2.Distance(wave.spawns[a].position, wave.spawns[b].position);

                    distanceSum += dist;
                    pairCount++;

                    if (dist < minDistance)
                        minDistance = dist;

                    if (dist <= clusterThreshold)
                        clusterPairCount++;
                }
            }
        }

        if (pairCount <= 0)
            return;

        spacingMean = Mathf.Clamp01((distanceSum / pairCount) / diagonal);
        spacingMin = Mathf.Clamp01((minDistance == float.MaxValue ? 0f : minDistance) / diagonal);
        clusterScore = Mathf.Clamp01((float)clusterPairCount / pairCount);
    }
}