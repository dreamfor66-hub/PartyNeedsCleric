using System.Text;
using UnityEngine;

public static class MapQualityEvaluator
{
    public static float Evaluate(GeneratedMapMeta meta)
    {
        if (meta == null)
            return 0f;

        float score = 0f;

        float symmetryNone = meta.Get("layout.symmetry.none");
        float symmetryLR = meta.Get("layout.symmetry.lr");
        float obstacleDensity = meta.Get("obstacle.density");
        float centerBlock = meta.Get("obstacle.centerBlock");
        float laneCount = meta.Get("lane.count");
        float startSafety = meta.Get("start.safety");
        float waveCount = meta.Get("wave.count");
        float spacingMean = meta.Get("spawn.spacing.mean");
        float spacingMin = meta.Get("spawn.spacing.min");
        float clusterScore = meta.Get("spawn.cluster.score");

        score += symmetryNone * 0.8f;
        score -= symmetryLR * 0.35f;

        score += ScoreRange(obstacleDensity, 0.08f, 0.22f, 1.2f);
        score -= centerBlock * 0.15f;

        score += ScoreRange(laneCount, 2f, 3f, 0.9f);
        score += startSafety * 1.8f;
        score += ScoreRange(waveCount, 2f, 4f, 0.5f);

        score += spacingMean * 0.8f;
        score += spacingMin * 1.1f;
        score -= clusterScore * 0.9f;

        return score;
    }

    public static bool PassHardRules(GeneratedMapMeta meta)
    {
        if (meta == null)
            return false;

        float startSafety = meta.Get("start.safety");
        float spacingMin = meta.Get("spawn.spacing.min");
        float obstacleDensity = meta.Get("obstacle.density");

        if (startSafety < 0.08f)
            return false;

        if (spacingMin < 0.02f)
            return false;

        if (obstacleDensity > 0.45f)
            return false;

        return true;
    }

    public static string BuildDebugLog(GeneratedMapMeta meta)
    {
        if (meta == null)
            return "Meta: null";

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("=== Map Quality Score ===");

        Append(sb, "layout.symmetry.none", meta.Get("layout.symmetry.none"));
        Append(sb, "layout.symmetry.lr", meta.Get("layout.symmetry.lr"));
        Append(sb, "layout.symmetry.tb", meta.Get("layout.symmetry.tb"));
        Append(sb, "obstacle.density", meta.Get("obstacle.density"));
        Append(sb, "obstacle.centerBlock", meta.Get("obstacle.centerBlock"));
        Append(sb, "lane.count", meta.Get("lane.count"));
        Append(sb, "start.safety", meta.Get("start.safety"));
        Append(sb, "wave.count", meta.Get("wave.count"));
        Append(sb, "spawn.meleeRatio", meta.Get("spawn.meleeRatio"));
        Append(sb, "spawn.rangedRatio", meta.Get("spawn.rangedRatio"));
        Append(sb, "spawn.spacing.mean", meta.Get("spawn.spacing.mean"));
        Append(sb, "spawn.spacing.min", meta.Get("spawn.spacing.min"));
        Append(sb, "spawn.cluster.score", meta.Get("spawn.cluster.score"));

        sb.AppendLine($"PassHardRules = {PassHardRules(meta)}");
        sb.AppendLine($"QualityScore = {Evaluate(meta):0.###}");

        return sb.ToString();
    }

    private static float ScoreRange(float value, float min, float max, float reward)
    {
        if (value < min)
            return -Mathf.Abs(min - value) * reward;

        if (value > max)
            return -Mathf.Abs(value - max) * reward;

        return reward;
    }

    private static void Append(StringBuilder sb, string key, float value)
    {
        sb.AppendLine($"{key} = {value:0.###}");
    }
}