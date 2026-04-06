#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class MapRandomGenerator
{
    private const float GridSize = 32f;
    private const float StartSafeRadius = 96f;
    private const float SpawnCollisionRadius = 32f;
    private const int DefaultCandidateCount = 12;
    private const float CandidateTieBreakNoise = 0.025f;

    public static MapGeneratedCandidate GenerateBestFull(MapData source, MapPreferenceProfile profile, int candidateCount = DefaultCandidateCount)
    {
        return GenerateBest(source, profile, candidateCount, rerollLayout: true, rerollWaves: true);
    }

    public static MapGeneratedCandidate GenerateBestLayoutOnly(MapData source, MapPreferenceProfile profile, int candidateCount = DefaultCandidateCount)
    {
        return GenerateBest(source, profile, candidateCount, rerollLayout: true, rerollWaves: false);
    }

    public static MapGeneratedCandidate GenerateBestWavesOnly(MapData source, MapPreferenceProfile profile, int candidateCount = DefaultCandidateCount)
    {
        return GenerateBest(source, profile, candidateCount, rerollLayout: false, rerollWaves: true);
    }

    private static MapGeneratedCandidate GenerateBest(MapData source, MapPreferenceProfile profile, int candidateCount, bool rerollLayout, bool rerollWaves)
    {
        if (source == null)
            return null;

        candidateCount = Mathf.Max(1, candidateCount);

        var enemyPool = LoadEnemyCharacters();
        if (enemyPool.Count == 0)
            return null;

        MapGeneratedCandidate best = null;
        float bestScore = float.MinValue;
        MapGeneratedCandidate bestPassing = null;
        float bestPassingScore = float.MinValue;

        for (int i = 0; i < candidateCount; i++)
        {
            var candidate = GenerateCandidate(source, enemyPool, rerollLayout, rerollWaves);
            ScoreCandidate(candidate, profile);

            if (best == null || candidate.finalScore > bestScore)
            {
                best = candidate;
                bestScore = candidate.finalScore;
            }

            if (candidate.finalScore > MapCandidateEvaluator.HardRuleFailScore)
            {
                if (bestPassing == null || candidate.finalScore > bestPassingScore)
                {
                    bestPassing = candidate;
                    bestPassingScore = candidate.finalScore;
                }
            }
        }

        return bestPassing ?? best;
    }

    private static MapGeneratedCandidate GenerateCandidate(
        MapData source,
        List<CharacterData> enemyPool,
        bool rerollLayout,
        bool rerollWaves)
    {
        var candidate = new MapGeneratedCandidate();
        candidate.size = source.size;

        if (rerollLayout)
        {
            candidate.playerStartPoints = GenerateStartPoints(source.size);
            candidate.collisions = GenerateCollisionPattern(source.size, candidate.playerStartPoints);
        }
        else
        {
            candidate.playerStartPoints = CloneStartPoints(source.playerStartPoints);
            candidate.collisions = CloneCollisions(source.collisions);
        }

        if (rerollWaves)
        {
            candidate.waves = GenerateWaves(source.size, candidate.playerStartPoints, candidate.collisions, enemyPool);
        }
        else
        {
            candidate.waves = CloneWaves(source.waves);
        }

        candidate.meta = BuildMeta(candidate);
        return candidate;
    }

    private static void ScoreCandidate(MapGeneratedCandidate candidate, MapPreferenceProfile profile)
    {
        if (candidate == null)
            return;

        candidate.qualityScore = MapQualityEvaluator.Evaluate(candidate.meta);
        candidate.preferenceScore = MapCandidateEvaluator.EvaluatePreference(candidate.meta, profile);
        candidate.finalScore = MapCandidateEvaluator.Evaluate(candidate.meta, profile);

        if (candidate.finalScore > MapCandidateEvaluator.HardRuleFailScore)
            candidate.finalScore += Random.Range(-CandidateTieBreakNoise, CandidateTieBreakNoise);
    }

    private static GeneratedMapMeta BuildMeta(MapGeneratedCandidate candidate)
    {
        var temp = ScriptableObject.CreateInstance<MapData>();
        temp.size = candidate.size;
        temp.playerStartPoints = CloneStartPoints(candidate.playerStartPoints);
        temp.collisions = CloneCollisions(candidate.collisions);
        temp.waves = CloneWaves(candidate.waves);

        var meta = MapFeatureExtractor.Extract(temp);
        Object.DestroyImmediate(temp);

        return meta;
    }

    private static Vector2[] GenerateStartPoints(Vector2 size)
    {
        float halfW = size.x * 0.5f;
        float halfH = size.y * 0.5f;

        float baseY = -halfH + GridSize * 2f;
        float yJitter = Random.Range(-GridSize * 0.5f, GridSize * 1.5f);
        float y = baseY + yJitter;

        float centerX = Random.Range(-halfW * 0.1f, halfW * 0.1f);
        float laneGap = Random.Range(halfW * 0.22f, halfW * 0.36f);
        float sideJitter = Random.Range(-GridSize, GridSize);
        float asymmetry = Random.Range(-GridSize * 0.5f, GridSize * 0.5f);

        float leftX = centerX - laneGap + sideJitter;
        float midX = centerX + Random.Range(-GridSize * 0.5f, GridSize * 0.5f);
        float rightX = centerX + laneGap + asymmetry;

        leftX = Mathf.Clamp(leftX, -halfW + GridSize, halfW - GridSize);
        midX = Mathf.Clamp(midX, -halfW + GridSize, halfW - GridSize);
        rightX = Mathf.Clamp(rightX, -halfW + GridSize, halfW - GridSize);
        y = Mathf.Clamp(y, -halfH + GridSize, -halfH + GridSize * 4f);

        return new[]
        {
            Snap(new Vector2(leftX, y)),
            Snap(new Vector2(midX, y)),
            Snap(new Vector2(rightX, y))
        };
    }

    private static List<Vector2> GenerateCollisionPattern(Vector2 size, Vector2[] startPoints)
    {
        var result = new List<Vector2>();
        int pattern = Random.Range(0, 8);

        switch (pattern)
        {
            case 0:
                break;

            case 1:
                AddRect(result, Vector2.zero, 3, 3);
                break;

            case 2:
                AddRect(result, new Vector2(-64f, 32f), 2, 4);
                AddRect(result, new Vector2(96f, -32f), 2, 3);
                break;

            case 3:
                AddRect(result, new Vector2(-128f, 32f), 2, 5);
                AddRect(result, new Vector2(128f, 32f), 2, 5);
                break;

            case 4:
                AddRect(result, new Vector2(-96f, 96f), 3, 2);
                AddRect(result, new Vector2(64f, -32f), 2, 4);
                AddRect(result, new Vector2(0f, 160f), 2, 2);
                break;

            case 5:
                AddRect(result, new Vector2(-96f, 64f), 2, 3);
                AddRect(result, new Vector2(0f, 128f), 1, 4);
                AddRect(result, new Vector2(96f, 64f), 2, 3);
                break;

            case 6:
                AddRect(result, new Vector2(-128f, -32f), 2, 2);
                AddRect(result, new Vector2(-32f, 96f), 3, 2);
                AddRect(result, new Vector2(96f, 160f), 2, 2);
                break;

            case 7:
                AddRect(result, new Vector2(0f, 64f), 4, 1);
                AddRect(result, new Vector2(-96f, 160f), 2, 2);
                AddRect(result, new Vector2(128f, 0f), 1, 3);
                break;
        }

        AddRandomCollisionScatter(result, size, startPoints);
        RemoveNearStarts(result, startPoints, StartSafeRadius);
        return result.Distinct().ToList();
    }

    private static void AddRandomCollisionScatter(List<Vector2> result, Vector2 size, Vector2[] startPoints)
    {
        int extraCount = Random.Range(2, 9);
        float halfW = size.x * 0.5f;
        float halfH = size.y * 0.5f;

        for (int i = 0; i < extraCount; i++)
        {
            Vector2 p = new Vector2(
                Random.Range(-halfW + GridSize, halfW - GridSize),
                Random.Range(-halfH + GridSize * 2f, halfH - GridSize));
            p = Snap(p);

            if (IsNearStarts(p, startPoints, StartSafeRadius))
                continue;

            result.Add(p);
        }
    }

    private static List<WaveData> GenerateWaves(
        Vector2 size,
        Vector2[] startPoints,
        List<Vector2> collisions,
        List<CharacterData> enemyPool)
    {
        var waves = new List<WaveData>();

        int waveCount = Random.Range(2, 7);
        for (int i = 0; i < waveCount; i++)
        {
            var wave = new WaveData();
            wave.holdType = Random.value < 0.75f ? WaveHoldType.HoldUntilCount : WaveHoldType.HoldUntilTime;
            wave.value = wave.holdType == WaveHoldType.HoldUntilCount
                ? Mathf.Round(Random.Range(0f, 3f))
                : Random.Range(2f, 6f);

            wave.spawns = new List<SpawnEntry>();

            int spawnCount = Random.Range(3, 10);
            for (int s = 0; s < spawnCount; s++)
            {
                var enemy = enemyPool[Random.Range(0, enemyPool.Count)];
                Vector2 pos = FindSpawnPosition(size, startPoints, collisions, wave.spawns);

                wave.spawns.Add(new SpawnEntry
                {
                    data = enemy,
                    position = pos
                });
            }

            waves.Add(wave);
        }

        return waves;
    }

    private static Vector2 FindSpawnPosition(
        Vector2 size,
        Vector2[] startPoints,
        List<Vector2> collisions,
        List<SpawnEntry> currentSpawns)
    {
        float halfW = size.x * 0.5f;
        float halfH = size.y * 0.5f;

        for (int i = 0; i < 100; i++)
        {
            float x = Random.Range(-halfW + GridSize, halfW - GridSize);
            float y = Random.Range(-halfH * 0.1f, halfH - GridSize);

            Vector2 pos = Snap(new Vector2(x, y));

            if (IsNearStarts(pos, startPoints, StartSafeRadius))
                continue;

            if (IsBlockedByCollision(pos, collisions))
                continue;

            if (IsTooCloseToSpawns(pos, currentSpawns, SpawnCollisionRadius))
                continue;

            return pos;
        }

        return Snap(new Vector2(0f, halfH * 0.25f));
    }

    private static bool IsNearStarts(Vector2 pos, Vector2[] startPoints, float radius)
    {
        if (startPoints == null)
            return false;

        for (int i = 0; i < startPoints.Length; i++)
        {
            if (Vector2.Distance(pos, startPoints[i]) < radius)
                return true;
        }

        return false;
    }

    private static bool IsBlockedByCollision(Vector2 pos, List<Vector2> collisions)
    {
        if (collisions == null)
            return false;

        for (int i = 0; i < collisions.Count; i++)
        {
            if (Vector2.Distance(pos, collisions[i]) < GridSize * 0.5f)
                return true;
        }

        return false;
    }

    private static bool IsTooCloseToSpawns(Vector2 pos, List<SpawnEntry> spawns, float minDistance)
    {
        if (spawns == null)
            return false;

        for (int i = 0; i < spawns.Count; i++)
        {
            if (Vector2.Distance(pos, spawns[i].position) < minDistance)
                return true;
        }

        return false;
    }

    private static void RemoveNearStarts(List<Vector2> collisions, Vector2[] startPoints, float radius)
    {
        if (collisions == null || startPoints == null)
            return;

        collisions.RemoveAll(c =>
        {
            for (int i = 0; i < startPoints.Length; i++)
            {
                if (Vector2.Distance(c, startPoints[i]) < radius)
                    return true;
            }
            return false;
        });
    }

    private static void AddRect(List<Vector2> list, Vector2 center, int widthCells, int heightCells)
    {
        int halfW = widthCells / 2;
        int halfH = heightCells / 2;

        for (int y = -halfH; y <= halfH; y++)
        {
            for (int x = -halfW; x <= halfW; x++)
            {
                list.Add(Snap(center + new Vector2(x * GridSize, y * GridSize)));
            }
        }
    }

    private static Vector2 Snap(Vector2 pos)
    {
        pos.x = Mathf.Round(pos.x / GridSize) * GridSize;
        pos.y = Mathf.Round(pos.y / GridSize) * GridSize;
        return pos;
    }

    private static Vector2[] CloneStartPoints(Vector2[] source)
    {
        if (source == null)
            return new Vector2[3];

        var arr = new Vector2[source.Length];
        for (int i = 0; i < source.Length; i++)
            arr[i] = source[i];
        return arr;
    }

    private static List<Vector2> CloneCollisions(List<Vector2> source)
    {
        var list = new List<Vector2>();
        if (source == null)
            return list;

        for (int i = 0; i < source.Count; i++)
            list.Add(source[i]);

        return list;
    }

    private static List<WaveData> CloneWaves(List<WaveData> source)
    {
        var list = new List<WaveData>();
        if (source == null)
            return list;

        for (int i = 0; i < source.Count; i++)
        {
            var src = source[i];
            var wave = new WaveData
            {
                holdType = src.holdType,
                value = src.value,
                spawns = new List<SpawnEntry>()
            };

            if (src.spawns != null)
            {
                for (int j = 0; j < src.spawns.Count; j++)
                {
                    wave.spawns.Add(new SpawnEntry
                    {
                        data = src.spawns[j].data,
                        position = src.spawns[j].position
                    });
                }
            }

            list.Add(wave);
        }

        return list;
    }

    private static List<CharacterData> LoadEnemyCharacters()
    {
        var result = new List<CharacterData>();
        string[] guids = AssetDatabase.FindAssets("t:CharacterData", new[] { "Assets/Data/Character" });

        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var ch = AssetDatabase.LoadAssetAtPath<CharacterData>(path);

            if (ch == null)
                continue;

            if (!ch.name.Contains("Enemy"))
                continue;

            result.Add(ch);
        }

        return result;
    }
}
#endif
