using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class MapFeatureValue
{
    public string key;
    public float value;
}

[Serializable]
public class GeneratedMapMeta
{
    public string generatorVersion = "v1";
    public List<MapFeatureValue> features = new();

    public void Set(string key, float value)
    {
        if (features == null)
            features = new List<MapFeatureValue>();

        for (int i = 0; i < features.Count; i++)
        {
            if (features[i].key == key)
            {
                features[i].value = value;
                return;
            }
        }

        features.Add(new MapFeatureValue
        {
            key = key,
            value = value
        });
    }

    public float Get(string key, float defaultValue = 0f)
    {
        if (features == null)
            return defaultValue;

        for (int i = 0; i < features.Count; i++)
        {
            if (features[i].key == key)
                return features[i].value;
        }

        return defaultValue;
    }

    public void Clear()
    {
        if (features == null)
            features = new List<MapFeatureValue>();
        else
            features.Clear();
    }
}