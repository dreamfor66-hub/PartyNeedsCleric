using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class StageManager : MonoBehaviour
{
    public static StageManager Instance;

    public StageData stageData;
    int currentFloor = 0;
    MapData currentMap;
    GameObject mapRoot;

    // ========== Visual Settings (Inspector 설정) ==========
    [Header("Map Visual Settings")]
    public Color wallColor = new Color(0, 0, 0, 0.40f);
    public Color collisionColor = new Color(0.2f, 0f, 0f, 0.35f);
    public Color floorColor = new Color(0, 0, 0, 0.22f);
    public Sprite baseSprite;   // 바닥, 벽, Collision에 쓸 Sprite


    // === FullRect 타일링 전용 스프라이트 ===
    private Sprite tileSprite;

    void Awake()
    {
        Instance = this;
        CreateTileSprite();
    }

    // ------------------------------------------------------------
    // (1) FullRect White Sprite 생성
    // ------------------------------------------------------------
    void CreateTileSprite()
    {
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();

        tileSprite = Sprite.Create(
            tex,
            new Rect(0, 0, tex.width, tex.height),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect   // ★ FullRect 강제
        );
    }

    void Start()
    {
        StartCoroutine(RunStage());
    }

    // ============================================================
    // STAGE LOOP
    // ============================================================

    IEnumerator RunStage()
    {
        for (currentFloor = 0; currentFloor < stageData.floors.Count; currentFloor++)
        {
            var floor = stageData.floors[currentFloor];
            currentMap = floor.maps[Random.Range(0, floor.maps.Count)];

            yield return StartCoroutine(LoadMap(currentMap));
            yield return StartCoroutine(RunMap(currentMap));

            if (IsAllPlayersDead())
            {
                ShowFail();
                yield break;
            }
        }

        ShowClear();
    }

    // ============================================================
    // LOAD MAP
    // ============================================================

    IEnumerator LoadMap(MapData map)
    {
        if (mapRoot != null)
            Destroy(mapRoot);

        mapRoot = new GameObject("MapRoot");

        CreateMapVisual(map.size);
        CreateMapBounds(map.size);
        CreateCollisions(map);     // 내부 장애물 생성

        yield return null;

        MovePlayersToStartPoints(map.playerStartPoints);
    }

    // ============================================================
    // VISUAL BOX
    // ============================================================

    void CreateMapVisual(Vector2 size)
    {
        var go = new GameObject("MapVisual");
        go.transform.SetParent(mapRoot.transform);
        go.transform.position = Vector3.zero;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = baseSprite;
        sr.color = floorColor;
        sr.drawMode = SpriteDrawMode.Simple;   // ★ 절대 Tiled/Sliced 사용하지 않음
        sr.sortingOrder = 20000;

        // 스프라이트 기본 크기(1 unit) 기준으로 스케일링
        go.transform.localScale = new Vector3(size.x, size.y, 1f);
    }

    // ============================================================
    // MAP BOUNDS (WALLS)
    // ============================================================

    void CreateMapBounds(Vector2 mapSize)
    {
        float w = mapSize.x;
        float h = mapSize.y;
        float t = 32;  // 인스펙터에서 지정할 벽 두께

        // 좌 우 벽: 세로 h, 두께 t
        Vector2 wallSizeV = new Vector2(t, h + t * 2);
        // 상 하 벽: 가로 w, 두께 t
        Vector2 wallSizeH = new Vector2(w + t * 2, t);

        // 좌/우 벽 위치
        float leftX = -(w * 0.5f) - (t * 0.5f);
        float rightX = +(w * 0.5f) + (t * 0.5f);

        // 상/하 벽 위치
        float topY = +(h * 0.5f) + (t * 0.5f);
        float bottomY = -(h * 0.5f) - (t * 0.5f);

        MakeWall("LeftWall", new Vector2(leftX, 0), wallSizeV, t);
        MakeWall("RightWall", new Vector2(rightX, 0), wallSizeV, t);
        MakeWall("TopWall", new Vector2(0, topY), wallSizeH, t);
        MakeWall("BottomWall", new Vector2(0, bottomY), wallSizeH, t);
    }

    void MakeWall(string name, Vector2 pos, Vector2 size, float thickness)
    {
        var go = new GameObject(name);
        go.transform.SetParent(mapRoot.transform);

        // size = 길이, thickness = 두께
        // pos 는 이미 thickness 반영된 위치
        go.transform.position = new Vector3(pos.x, pos.y, 0);
        go.transform.localScale = new Vector3(size.x, size.y, 1);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = baseSprite;
        sr.color = wallColor;
        sr.drawMode = SpriteDrawMode.Simple;
        sr.sortingOrder = -1999;

        // Collider는 항상 (1,1) 유지 → scale로 크기 결정됨
        var col = go.AddComponent<BoxCollider2D>();
        col.size = Vector2.one;
    }



    // ============================================================
    // INTERNAL COLLISIONS
    // ============================================================

    void CreateCollisions(MapData map)
    {
        if (map.collisions == null) return;

        foreach (var pos in map.collisions)
        {
            var go = new GameObject("Collision");
            go.transform.SetParent(mapRoot.transform);
            go.transform.position = new Vector3(pos.x, pos.y, 0);

            // ★ 32x32는 localScale로 결정
            go.transform.localScale = new Vector3(32f, 32f, 1);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = baseSprite;
            sr.color = collisionColor;
            sr.drawMode = SpriteDrawMode.Simple;
            sr.sortingOrder = 20000;

            // Collider2D는 항상 1x1 상태에서 localScale로 실제 크기가 만들어짐
            var col = go.AddComponent<BoxCollider2D>();
            col.size = Vector2.one;   // ★ 기본값 유지
        }
    }


    // ============================================================
    // WAVE SPAWNING
    // ============================================================

    void SpawnWave(WaveData wave)
    {
        foreach (var s in wave.spawns)
        {
            var c = Instantiate(CharacterSpawner.Instance.characterPrefab);
            c.Init(s.data, TeamType.Enemy);
            c.transform.position = s.position;
        }
    }

    IEnumerator RunMap(MapData map)
    {
        foreach (var wave in map.waves)
        {
            SpawnWave(wave);

            if (wave.holdType == WaveHoldType.HoldUntilCount)
                yield return StartCoroutine(WaitUntilCount(wave.value));
            else
                yield return new WaitForSeconds(wave.value);
        }

        yield return StartCoroutine(WaitUntilAllEnemiesDead());
    }

    IEnumerator WaitUntilCount(float remain)
    {
        while (true)
        {
            int alive = 0;
            foreach (var c in EntityContainer.Instance.Characters)
            {
                if (c.team == TeamType.Enemy && c.state != CharacterState.Die)
                    alive++;
            }

            if (alive <= remain)
                break;

            yield return null;
        }

        yield return new WaitForSeconds(0.5f);
    }

    IEnumerator WaitUntilAllEnemiesDead()
    {
        while (true)
        {
            bool any = false;
            foreach (var c in EntityContainer.Instance.Characters)
            {
                if (c.team == TeamType.Enemy && c.state != CharacterState.Die)
                {
                    any = true;
                    break;
                }
            }

            if (!any) break;
            yield return null;
        }
    }

    // ============================================================
    // PLAYER MANAGEMENT
    // ============================================================

    void MovePlayersToStartPoints(Vector2[] pts)
    {
        var players = EntityContainer.Instance.Characters;
        int idx = 0;

        foreach (var p in players)
        {
            if (p.team != TeamType.Player) continue;
            if (idx >= pts.Length) break;

            p.transform.position = pts[idx];
            idx++;
        }
    }

    bool IsAllPlayersDead()
    {
        foreach (var c in EntityContainer.Instance.Characters)
        {
            if (c.team == TeamType.Player && c.state != CharacterState.Die)
                return false;
        }
        return true;
    }

    // ============================================================
    // RESULT
    // ============================================================

    void ShowClear()
    {
        Debug.Log("CLEAR!");
    }

    void ShowFail()
    {
        Debug.Log("FAIL!");
    }
}
