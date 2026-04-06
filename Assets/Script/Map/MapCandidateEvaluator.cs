using System.Text;

public static class MapCandidateEvaluator
{
    public static float Evaluate(GeneratedMapMeta meta, MapPreferenceProfile profile)
    {
        if (meta == null)
            return float.MinValue;

        if (!MapQualityEvaluator.PassHardRules(meta))
            return -999999f;

        float qualityScore = MapQualityEvaluator.Evaluate(meta);
        float preferenceScore = EvaluatePreference(meta, profile);

        return qualityScore + preferenceScore;
    }

    public static float EvaluatePreference(GeneratedMapMeta meta, MapPreferenceProfile profile)
    {
        if (meta == null || meta.features == null || profile == null)
            return 0f;

        float score = 0f;

        for (int i = 0; i < meta.features.Count; i++)
        {
            var feature = meta.features[i];
            if (feature == null || string.IsNullOrEmpty(feature.key))
                continue;

            float bias = profile.GetBias(feature.key, 0f);
            score += feature.value * bias;
        }

        return score;
    }

    public static string BuildDebugLog(GeneratedMapMeta meta, MapPreferenceProfile profile)
    {
        StringBuilder sb = new StringBuilder();

        if (meta == null)
        {
            sb.AppendLine("Meta: null");
            return sb.ToString();
        }

        sb.AppendLine(MapQualityEvaluator.BuildDebugLog(meta));

        float preferenceTotal = 0f;
        sb.AppendLine("=== Learned Preference Score ===");

        if (meta.features != null && profile != null)
        {
            for (int i = 0; i < meta.features.Count; i++)
            {
                var feature = meta.features[i];
                if (feature == null || string.IsNullOrEmpty(feature.key))
                    continue;

                float bias = profile.GetBias(feature.key, 0f);
                float contribution = feature.value * bias;
                preferenceTotal += contribution;

                sb.AppendLine(
                    $"{feature.key} | value={feature.value:0.###} | bias={bias:0.###} | contrib={contribution:0.###}"
                );
            }
        }

        float final = Evaluate(meta, profile);

        sb.AppendLine($"PreferenceScore = {preferenceTotal:0.###}");
        sb.AppendLine($"FinalScore = {final:0.###}");

        return sb.ToString();
    }
}