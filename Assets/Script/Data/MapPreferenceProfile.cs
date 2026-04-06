using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class MapFeatureBias
{
    public string key;
    public float bias;
    public int likeCount;
    public int dislikeCount;
}

[CreateAssetMenu(menuName = "Game/Map/MapPreferenceProfile")]
public class MapPreferenceProfile : ScriptableObject
{
    private const float MaxAbsBias = 2.5f;

    [Header("Learned Bias Memory")]
    public List<MapFeatureBias> learnedBias = new();

    public float GetBias(string key, float defaultValue = 0f)
    {
        if (learnedBias == null || string.IsNullOrEmpty(key))
            return defaultValue;

        for (int i = 0; i < learnedBias.Count; i++)
        {
            if (learnedBias[i].key == key)
                return learnedBias[i].bias;
        }

        return defaultValue;
    }

    public void AddFeedback(string key, bool liked, float delta)
    {
        if (string.IsNullOrEmpty(key))
            return;

        var entry = GetOrCreate(key);
        entry.bias += liked ? delta : -delta;
        entry.bias = Mathf.Clamp(entry.bias, -MaxAbsBias, MaxAbsBias);

        if (liked)
            entry.likeCount++;
        else
            entry.dislikeCount++;
    }

    public void AddFeedback(GeneratedMapMeta meta, bool liked, float deltaPerUnit = 0.15f)
    {
        if (meta == null || meta.features == null)
            return;

        for (int i = 0; i < meta.features.Count; i++)
        {
            var f = meta.features[i];
            if (f == null || string.IsNullOrEmpty(f.key))
                continue;

            float centered = ToPreferenceSignal(f.key, f.value);
            AddFeedback(f.key, liked, deltaPerUnit * centered);
        }
    }

    public void ClearMemory()
    {
        if (learnedBias == null)
            learnedBias = new List<MapFeatureBias>();
        else
            learnedBias.Clear();
    }

    private MapFeatureBias GetOrCreate(string key)
    {
        if (learnedBias == null)
            learnedBias = new List<MapFeatureBias>();

        for (int i = 0; i < learnedBias.Count; i++)
        {
            if (learnedBias[i].key == key)
                return learnedBias[i];
        }

        var created = new MapFeatureBias
        {
            key = key,
            bias = 0f,
            likeCount = 0,
            dislikeCount = 0
        };

        learnedBias.Add(created);
        return created;
    }

    private static float ToPreferenceSignal(string key, float rawValue)
    {
        if (key == "lane.count")
            return Mathf.Clamp(rawValue / 5f, 0f, 1f) - 0.5f;

        if (key == "wave.count")
            return Mathf.Clamp(rawValue / 8f, 0f, 1f) - 0.5f;

        if (key == "obstacle.centerBlock")
            return rawValue > 0.5f ? 1f : -1f;

        return Mathf.Clamp(rawValue, 0f, 1f) - 0.5f;
    }
}
