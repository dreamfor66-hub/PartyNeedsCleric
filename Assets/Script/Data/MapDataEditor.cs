#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Sirenix.OdinInspector.Editor;

public enum MapEditMode
{
    None,
    StartPointMove,
    Monster,
    Collision
}

[CustomEditor(typeof(MapData))]
public class MapDataEditor : OdinEditor
{
    MapEditMode editMode = MapEditMode.None;
    int selectedWaveIndex = 0;

    CharacterData selectedMonster;
    bool draggingStartPoint = false;
    int draggingStartIndex = -1;

    bool draggingSpawn = false;
    SpawnEntry draggingSpawnEntry;

    static List<CharacterData> enemyCharacters;

    const float GRID_SIZE = 32f;

    protected override void OnEnable()
    {
        base.OnEnable();
        RefreshEnemyCharacters();

        var map = target as MapData;
        if (map != null)
        {
            map.RebuildGeneratedMetaIfNeeded();
            EditorUtility.SetDirty(map);
        }
    }

    public override void OnInspectorGUI()
    {
        var map = (MapData)target;

        if (map.playerStartPoints == null || map.playerStartPoints.Length != 3)
        {
            Undo.RecordObject(map, "Fix StartPoint Count");
            map.playerStartPoints = new Vector2[3]
            {
            map.playerStartPoints != null && map.playerStartPoints.Length > 0 ? map.playerStartPoints[0] : Vector2.zero,
            map.playerStartPoints != null && map.playerStartPoints.Length > 1 ? map.playerStartPoints[1] : Vector2.zero,
            map.playerStartPoints != null && map.playerStartPoints.Length > 2 ? map.playerStartPoints[2] : Vector2.zero
            };
            map.RebuildGeneratedMetaIfNeeded();
            EditorUtility.SetDirty(map);
        }

        EditorGUI.BeginChangeCheck();

        base.OnInspectorGUI();

        if (EditorGUI.EndChangeCheck())
        {
            map.RebuildGeneratedMetaIfNeeded();
            EditorUtility.SetDirty(map);
        }

        GUILayout.Space(10);
        DrawEditModeToolbar();

        GUILayout.Space(5);
        DrawWaveToolbar(map);

        GUILayout.Space(5);
        Rect simRect = DrawSimulationSpaceBackground(map);

        GUILayout.Space(10);
        DrawGenerationToolbar(map);

        GUILayout.Space(10);
        DrawMetaToolbar(map);

        GUILayout.Space(10);
        DrawScorePanel(map);

  
        DrawGrid(map, simRect);
        DrawCollisions(map, simRect);
        DrawStartPoints(map, simRect);
        DrawMonsters(map, simRect);

        HandleSimulationEvents(map, simRect);

        if (GUI.changed)
        {
            map.RebuildGeneratedMetaIfNeeded();
            EditorUtility.SetDirty(map);
        }
    }
    void DrawMetaToolbar(MapData map)
    {
        GUILayout.Label("Generated Meta", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Rebuild Meta", EditorStyles.miniButton))
        {
            map.RebuildGeneratedMetaIfNeeded();
            EditorUtility.SetDirty(map);
        }

        if (GUILayout.Button("Log Meta", EditorStyles.miniButton))
        {
            if (map.generatedMeta != null && map.generatedMeta.features != null)
            {
                for (int i = 0; i < map.generatedMeta.features.Count; i++)
                {
                    var f = map.generatedMeta.features[i];
                    Debug.Log($"{f.key} = {f.value}");
                }
            }
        }

        EditorGUILayout.EndHorizontal();
    }

    void DrawScorePanel(MapData map)
    {
        GUILayout.Label("Map Evaluation", EditorStyles.boldLabel);

        var profile = GameVariables.Instance != null ? GameVariables.Instance.mapPreferenceProfile : null;
        var meta = map.generatedMeta;

        bool passHardRules = MapQualityEvaluator.PassHardRules(meta);
        float qualityScore = MapQualityEvaluator.Evaluate(meta);
        float preferenceScore = MapCandidateEvaluator.EvaluatePreference(meta, profile);
        float finalScore = MapCandidateEvaluator.Evaluate(meta, profile);

        EditorGUILayout.BeginVertical("box");

        EditorGUILayout.LabelField("Hard Rules", passHardRules ? "PASS" : "FAIL");
        EditorGUILayout.LabelField("Quality Score", qualityScore.ToString("0.###"));
        EditorGUILayout.LabelField("Preference Score", preferenceScore.ToString("0.###"));
        EditorGUILayout.LabelField("Final Score", finalScore.ToString("0.###"));

        if (profile == null)
            EditorGUILayout.HelpBox("GameVariables.mapPreferenceProfile is null.", MessageType.Warning);

        EditorGUILayout.BeginHorizontal();

        GUI.enabled = profile != null;
        if (GUILayout.Button("Like"))
        {
            profile.AddFeedback(meta, true);
            EditorUtility.SetDirty(profile);
        }

        if (GUILayout.Button("Dislike"))
        {
            profile.AddFeedback(meta, false);
            EditorUtility.SetDirty(profile);
        }
        GUI.enabled = true;

        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("Log Evaluation"))
        {
            Debug.Log(MapCandidateEvaluator.BuildDebugLog(meta, profile));
        }

        EditorGUILayout.EndVertical();
    }

    void DrawEditModeToolbar()
    {
        GUILayout.Label("Edit Mode", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();

        editMode = DrawModeToggle(editMode, MapEditMode.None, "None");
        editMode = DrawModeToggle(editMode, MapEditMode.StartPointMove, "Start Move");
        editMode = DrawModeToggle(editMode, MapEditMode.Monster, "Monster");
        editMode = DrawModeToggle(editMode, MapEditMode.Collision, "Collision");

        EditorGUILayout.EndHorizontal();

        if (editMode == MapEditMode.Monster)
            DrawMonsterPalette();
    }

    MapEditMode DrawModeToggle(MapEditMode current, MapEditMode targetMode, string label)
    {
        bool on = current == targetMode;
        bool pressed = GUILayout.Toggle(on, label, EditorStyles.miniButton);
        if (pressed && !on)
            return targetMode;
        if (!pressed && on)
            return MapEditMode.None;
        return current;
    }

    void DrawWaveToolbar(MapData map)
    {
        GUILayout.Label("Waves", EditorStyles.boldLabel);

        if (map.waves == null || map.waves.Count == 0)
        {
            EditorGUILayout.HelpBox("No Waves.", MessageType.Info);
            return;
        }

        EditorGUILayout.BeginHorizontal();
        for (int i = 0; i < map.waves.Count; i++)
        {
            bool isOn = selectedWaveIndex == i;
            string name = $"Wave {i + 1}";
            bool pressed = GUILayout.Toggle(isOn, name, EditorStyles.miniButton);
            if (pressed && !isOn)
                selectedWaveIndex = i;
        }
        EditorGUILayout.EndHorizontal();

        selectedWaveIndex = Mathf.Clamp(selectedWaveIndex, 0, map.waves.Count - 1);
    }

    void DrawMonsterPalette()
    {
        GUILayout.Space(5);
        GUILayout.Label("Monsters (Enemy Character)", EditorStyles.boldLabel);

        if (enemyCharacters == null || enemyCharacters.Count == 0)
        {
            EditorGUILayout.HelpBox("No Enemy CharacterData found.", MessageType.Info);
            if (GUILayout.Button("Refresh"))
                RefreshEnemyCharacters();
            return;
        }

        EditorGUILayout.BeginHorizontal();

        foreach (var ch in enemyCharacters)
        {
            var spr = ch.sprites != null ? ch.sprites.anim_idleFront : null;
            Texture2D tex = spr != null ? spr.texture : null;

            GUIStyle squareBtn = new GUIStyle("Button");
            squareBtn.margin = new RectOffset(4, 4, 4, 4);
            squareBtn.padding = new RectOffset(0, 0, 0, 0);
            squareBtn.fixedWidth = 64;
            squareBtn.fixedHeight = 64;

            bool isSel = selectedMonster == ch;

            Rect r = GUILayoutUtility.GetRect(64, 64, squareBtn);

            if (GUI.Toggle(r, isSel, GUIContent.none, squareBtn))
                selectedMonster = ch;

            if (tex != null)
                GUI.DrawTexture(r, tex, ScaleMode.ScaleToFit, true);
            else
                EditorGUI.DrawRect(r, new Color(0.2f, 0.2f, 0.4f));
        }

        EditorGUILayout.EndHorizontal();
    }

    void RefreshEnemyCharacters()
    {
        enemyCharacters = new List<CharacterData>();
        string[] guids = AssetDatabase.FindAssets("t:CharacterData", new[] { "Assets/Data/Character" });

        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var ch = AssetDatabase.LoadAssetAtPath<CharacterData>(path);
            if (ch != null && ch.name.Contains("Enemy"))
                enemyCharacters.Add(ch);
        }

        enemyCharacters = enemyCharacters.OrderBy(c => c.name).ToList();
    }

    Rect DrawSimulationSpaceBackground(MapData map)
    {
        var borderSize = 32;
        float maxH = 600f + borderSize * 2;
        float maxW = 900f + borderSize * 2;

        float worldW = map.size.x;
        float worldH = map.size.y;
        float worldRatio = worldW / worldH;

        float targetH = maxH;
        float targetW = targetH * worldRatio;

        if (targetW > maxW)
        {
            targetW = maxW;
            targetH = targetW / worldRatio;
        }

        Rect r = GUILayoutUtility.GetRect(targetW, targetH, GUILayout.ExpandWidth(false));
        EditorGUI.DrawRect(r, new Color(0.12f, 0.12f, 0.12f));
        return r;
    }

    void DrawGrid(MapData map, Rect rect)
    {
        Handles.BeginGUI();

        float cell = GetCellSize(map.size, rect);
        if (cell <= 0f)
        {
            Handles.EndGUI();
            return;
        }

        Vector2 center = rect.center;

        float gridX = map.size.x / GRID_SIZE;
        float gridY = map.size.y / GRID_SIZE;

        int halfX = Mathf.CeilToInt(gridX / 2f);
        int halfY = Mathf.CeilToInt(gridY / 2f);

        Color major = new Color(0.25f, 0.25f, 0.25f);
        Color minor = new Color(0.18f, 0.18f, 0.18f);

        for (int x = -halfX; x <= halfX; x++)
        {
            float sx = center.x + x * cell;
            Handles.color = (x == 0) ? major : minor;
            Handles.DrawLine(new Vector3(sx, rect.yMin, 0), new Vector3(sx, rect.yMax, 0));
        }

        for (int y = -halfY; y <= halfY; y++)
        {
            float sy = center.y - y * cell;
            Handles.color = (y == 0) ? major : minor;
            Handles.DrawLine(new Vector3(rect.xMin, sy, 0), new Vector3(rect.xMax, sy, 0));
        }

        Handles.EndGUI();
    }

    void DrawStartPoints(MapData map, Rect rect)
    {
        if (map.playerStartPoints == null || map.playerStartPoints.Length < 3)
            return;

        Handles.BeginGUI();

        float cell = GetCellSize(map.size, rect);

        for (int i = 0; i < 3; i++)
        {
            Vector2 world = map.playerStartPoints[i];
            Vector2 screen = WorldToScreen(world, map.size, rect);

            Rect r = new Rect(screen.x - cell * 0.4f, screen.y - cell * 0.4f, cell * 0.8f, cell * 0.8f);

            Color col = i switch
            {
                0 => Color.cyan,
                1 => Color.green,
                _ => Color.yellow
            };

            EditorGUI.DrawRect(r, col);
            GUI.Label(r, (i + 1).ToString(), EditorStyles.boldLabel);
        }

        Handles.EndGUI();
    }

    void DrawCollisions(MapData map, Rect rect)
    {
        if (map.collisions == null)
            return;

        Handles.BeginGUI();

        float cell = GetCellSize(map.size, rect);

        foreach (var pos in map.collisions)
        {
            Vector2 screen = WorldToScreen(pos, map.size, rect);
            Rect r = new Rect(screen.x - cell / 2f, screen.y - cell / 2f, cell, cell);
            EditorGUI.DrawRect(r, new Color(0.4f, 0.2f, 0.2f));
        }

        Handles.EndGUI();
    }

    void DrawMonsters(MapData map, Rect rect)
    {
        if (map.waves == null || map.waves.Count == 0)
            return;

        selectedWaveIndex = Mathf.Clamp(selectedWaveIndex, 0, map.waves.Count - 1);
        var wave = map.waves[selectedWaveIndex];
        if (wave.spawns == null)
            return;

        Handles.BeginGUI();
        float cell = GetCellSize(map.size, rect);

        foreach (var s in wave.spawns)
        {
            if (s == null || s.data == null) continue;

            Vector2 screen = WorldToScreen(s.position, map.size, rect);
            var spr = s.data.sprites != null ? s.data.sprites.anim_idleFront : null;
            Texture2D tex = spr != null ? spr.texture : null;

            float worldW = spr != null ? spr.rect.width : 32;
            float worldH = spr != null ? spr.rect.height : 32;

            float gridW = worldW / GRID_SIZE;
            float gridH = worldH / GRID_SIZE;

            float drawW = gridW * cell;
            float drawH = gridH * cell;

            Rect r = new Rect(screen.x - drawW / 2f, screen.y - drawH / 2f, drawW, drawH);

            if (tex != null)
                GUI.DrawTexture(r, tex, ScaleMode.ScaleToFit, true);
            else
                EditorGUI.DrawRect(r, new Color(0.3f, 0.3f, 0.5f));
        }

        Handles.EndGUI();
    }
    void DrawGenerationToolbar(MapData map)
    {
        GUILayout.Label("Generation", EditorStyles.boldLabel);

        var profile = GameVariables.Instance != null ? GameVariables.Instance.mapPreferenceProfile : null;

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Generate Random", EditorStyles.miniButton))
        {
            var candidate = MapRandomGenerator.GenerateBestFull(map, profile);
            ApplyCandidateToMap(map, candidate, "Generate Random Map");
        }

        if (GUILayout.Button("Reroll Layout", EditorStyles.miniButton))
        {
            var candidate = MapRandomGenerator.GenerateBestLayoutOnly(map, profile);
            ApplyCandidateToMap(map, candidate, "Reroll Layout");
        }

        if (GUILayout.Button("Reroll Waves", EditorStyles.miniButton))
        {
            var candidate = MapRandomGenerator.GenerateBestWavesOnly(map, profile);
            ApplyCandidateToMap(map, candidate, "Reroll Waves");
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();

        GUILayout.FlexibleSpace();

        GUI.enabled = profile != null;
        if (GUILayout.Button("Clear Preference Memory", EditorStyles.miniButton, GUILayout.Width(160f)))
        {
            bool confirmed = EditorUtility.DisplayDialog(
                "Clear Preference Memory",
                "정말로 학습된 맵 선호 데이터를 전부 초기화할까요?\n이 작업은 되돌리기 어렵습니다.",
                "초기화",
                "취소"
            );

            if (confirmed)
            {
                profile.ClearMemory();
                EditorUtility.SetDirty(profile);
                AssetDatabase.SaveAssets();
            }
        }
        GUI.enabled = true;

        EditorGUILayout.EndHorizontal();
    }
    void HandleSimulationEvents(MapData map, Rect rect)
    {
        Event e = Event.current;
        if (e == null) return;
        if (!rect.Contains(e.mousePosition))
            return;

        Vector2 gridPos = SnapToGrid(ScreenToWorld(e.mousePosition, map.size, rect));

        switch (editMode)
        {
            case MapEditMode.StartPointMove:
                HandleStartPointEvents(map, rect, e, gridPos);
                break;

            case MapEditMode.Monster:
                HandleMonsterEvents(map, e, gridPos);
                break;

            case MapEditMode.Collision:
                HandleCollisionEvents(map, e, gridPos);
                break;
        }
    }

    void HandleStartPointEvents(MapData map, Rect rect, Event e, Vector2 gridPos)
    {
        if (map.playerStartPoints == null || map.playerStartPoints.Length < 3)
            return;

        if (e.type == EventType.MouseDown && e.button == 0)
        {
            for (int i = 0; i < 3; i++)
            {
                Vector2 sp = map.playerStartPoints[i];
                Vector2 spScreen = WorldToScreen(sp, map.size, rect);
                if (Vector2.Distance(spScreen, e.mousePosition) <= 12f)
                {
                    draggingStartPoint = true;
                    draggingStartIndex = i;
                    e.Use();
                    break;
                }
            }
        }
        else if (e.type == EventType.MouseDrag && draggingStartPoint)
        {
            MarkDirty(map, "Move StartPoint");
            map.playerStartPoints[draggingStartIndex] = gridPos;
            e.Use();
        }
        else if (e.type == EventType.MouseUp)
        {
            draggingStartPoint = false;
        }
    }

    void HandleMonsterEvents(MapData map, Event e, Vector2 gridPos)
    {
        if (map.waves == null || map.waves.Count == 0)
        {
            Undo.RecordObject(map, "Add Wave");
            map.waves = new List<WaveData>();
            map.waves.Add(new WaveData
            {
                holdType = WaveHoldType.HoldUntilCount,
                value = 0,
                spawns = new List<SpawnEntry>()
            });

            selectedWaveIndex = 0;
            map.RebuildGeneratedMetaIfNeeded();
            EditorUtility.SetDirty(map);
        }

        var wave = map.waves[selectedWaveIndex];
        if (wave.spawns == null)
            wave.spawns = new List<SpawnEntry>();

        SpawnEntry existing = FindSpawnAt(wave, gridPos);

        if (e.type == EventType.MouseDown)
        {
            if (e.button == 1)
            {
                if (existing != null)
                {
                    MarkDirty(map, "Remove Monster");
                    wave.spawns.Remove(existing);
                }
                e.Use();
            }
            else if (e.button == 0)
            {
                if (existing != null)
                {
                    draggingSpawn = true;
                    draggingSpawnEntry = existing;
                    e.Use();
                }
                else if (selectedMonster != null)
                {
                    MarkDirty(map, "Add Monster");
                    wave.spawns.Add(new SpawnEntry
                    {
                        data = selectedMonster,
                        position = gridPos
                    });
                    e.Use();
                }
            }
        }
        else if (e.type == EventType.MouseDrag && draggingSpawn)
        {
            if (FindSpawnAt(wave, gridPos) == null || FindSpawnAt(wave, gridPos) == draggingSpawnEntry)
            {
                MarkDirty(map, "Move Monster");
                draggingSpawnEntry.position = gridPos;
            }
            e.Use();
        }
        else if (e.type == EventType.MouseUp)
        {
            draggingSpawn = false;
        }
    }

    void HandleCollisionEvents(MapData map, Event e, Vector2 gridPos)
    {
        if (map.collisions == null)
            map.collisions = new List<Vector2>();

        int idx = FindCollisionAt(map, gridPos);

        if (e.type == EventType.MouseDown)
        {
            if (e.button == 0)
            {
                if (idx < 0)
                {
                    MarkDirty(map, "Add Collision");
                    map.collisions.Add(gridPos);
                }
                e.Use();
            }
            else if (e.button == 1)
            {
                if (idx >= 0)
                {
                    MarkDirty(map, "Remove Collision");
                    map.collisions.RemoveAt(idx);
                }
                e.Use();
            }
        }
    }

    void MarkDirty(MapData map, string undoName)
    {
        Undo.RecordObject(map, undoName);
        map.RebuildGeneratedMetaIfNeeded();
        EditorUtility.SetDirty(map);
    }

    float GetCellSize(Vector2 mapSize, Rect rect)
    {
        float gridX = mapSize.x / GRID_SIZE;
        float gridY = mapSize.y / GRID_SIZE;

        float cellX = rect.width / gridX;
        float cellY = rect.height / gridY;

        return Mathf.Min(cellX, cellY);
    }

    Vector2 WorldToScreen(Vector2 world, Vector2 mapSize, Rect rect)
    {
        float cell = GetCellSize(mapSize, rect);
        Vector2 center = rect.center;

        float sx = center.x + (world.x / GRID_SIZE) * cell;
        float sy = center.y - (world.y / GRID_SIZE) * cell;
        return new Vector2(sx, sy);
    }

    Vector2 ScreenToWorld(Vector2 screen, Vector2 mapSize, Rect rect)
    {
        float cell = GetCellSize(mapSize, rect);
        Vector2 center = rect.center;

        float wx = (screen.x - center.x) / cell * GRID_SIZE;
        float wy = (center.y - screen.y) / cell * GRID_SIZE;
        return new Vector2(wx, wy);
    }

    Vector2 SnapToGrid(Vector2 pos)
    {
        pos.x = Mathf.Round(pos.x / GRID_SIZE) * GRID_SIZE;
        pos.y = Mathf.Round(pos.y / GRID_SIZE) * GRID_SIZE;
        return pos;
    }

    SpawnEntry FindSpawnAt(WaveData wave, Vector2 gridPos)
    {
        foreach (var s in wave.spawns)
        {
            if (Mathf.RoundToInt(s.position.x) == Mathf.RoundToInt(gridPos.x) &&
                Mathf.RoundToInt(s.position.y) == Mathf.RoundToInt(gridPos.y))
                return s;
        }
        return null;
    }

    int FindCollisionAt(MapData map, Vector2 gridPos)
    {
        for (int i = 0; i < map.collisions.Count; i++)
        {
            var p = map.collisions[i];
            if (Mathf.RoundToInt(p.x) == Mathf.RoundToInt(gridPos.x) &&
                Mathf.RoundToInt(p.y) == Mathf.RoundToInt(gridPos.y))
                return i;
        }
        return -1;
    }

    void ApplyCandidateToMap(MapData map, MapGeneratedCandidate candidate, string undoName)
    {
        if (map == null || candidate == null)
        {
            Debug.LogWarning("Map generation failed. Candidate is null.");
            return;
        }

        Undo.RecordObject(map, undoName);

        map.size = candidate.size;

        map.playerStartPoints = new Vector2[candidate.playerStartPoints.Length];
        for (int i = 0; i < candidate.playerStartPoints.Length; i++)
            map.playerStartPoints[i] = candidate.playerStartPoints[i];

        map.collisions = new List<Vector2>();
        for (int i = 0; i < candidate.collisions.Count; i++)
            map.collisions.Add(candidate.collisions[i]);

        map.waves = new List<WaveData>();
        for (int i = 0; i < candidate.waves.Count; i++)
        {
            var srcWave = candidate.waves[i];
            var newWave = new WaveData
            {
                holdType = srcWave.holdType,
                value = srcWave.value,
                spawns = new List<SpawnEntry>()
            };

            if (srcWave.spawns != null)
            {
                for (int j = 0; j < srcWave.spawns.Count; j++)
                {
                    newWave.spawns.Add(new SpawnEntry
                    {
                        data = srcWave.spawns[j].data,
                        position = srcWave.spawns[j].position
                    });
                }
            }

            map.waves.Add(newWave);
        }

        map.generatedMeta = candidate.meta ?? MapFeatureExtractor.Extract(map);

        selectedWaveIndex = 0;

        EditorUtility.SetDirty(map);
    }
}
#endif