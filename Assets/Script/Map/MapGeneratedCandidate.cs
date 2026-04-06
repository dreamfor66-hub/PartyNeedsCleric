using System.Collections.Generic;
using UnityEngine;

public class MapGeneratedCandidate
{
    public Vector2 size;
    public Vector2[] playerStartPoints = new Vector2[3];
    public List<Vector2> collisions = new();
    public List<WaveData> waves = new();

    public GeneratedMapMeta meta;
    public float qualityScore;
    public float preferenceScore;
    public float finalScore;
}