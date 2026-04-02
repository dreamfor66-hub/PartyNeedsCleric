using UnityEngine;
using UnityEngine.EventSystems;

public class SkillInputController : MonoBehaviour
{
    public static SkillInputController Instance;

    bool isCastingMode = false;
    SkillData castingSkill;
    Vector3 castingPoint;
    Vector3 castingSlotScreenPos;

    void Awake()
    {
        Instance = this;
    } 

    enum State
    {
        Idle,
        PressedDown,
        Dragging
    }

    State state = State.Idle;

    [Header("Refs")]
    public RectTransform dragCursor;
    public Material lineMaterial;
    public Sprite arrowSprite;
    public float arrowDistance = 10f; // ★ 새 변수 추가

    [Header("Targeting")]
    public float pointDepth = 10f;
    public float detectRadius = 20f;
    [Header("Offset")]
    public Vector3 worldOffset = new Vector3(0, 40f, 0);

    // drag state
    SkillSlotUI currentSlot;
    int currentIndex = -1;
    SkillData currentSkill;
    Vector3 slotScreenPos;
    CharacterBehaviour currentTarget;
    Vector3 currentPoint;

    LineRenderer dragLine;
    LineRenderer rangeLine;
    LineRenderer castRangeLine;
    Transform arrowTransform;

    const int CircleSegments = 48;
    readonly Vector3[] circlePoints = new Vector3[CircleSegments + 1];

    void Start()
    {
        CreateLineRenderers();
        CreateArrow();
        CreateCastingLineRenderer();

        if (dragCursor != null)
            dragCursor.gameObject.SetActive(false);
        dragLine.enabled = false;
        rangeLine.enabled = false;
        arrowTransform.gameObject.SetActive(false);
    }

    void CreateLineRenderers()
    {
        // 드래그 라인
        var dragGo = new GameObject("SkillDragLine");
        dragGo.transform.SetParent(transform);
        dragLine = dragGo.AddComponent<LineRenderer>();
        dragLine.material = lineMaterial;
        dragLine.widthMultiplier = 3f;
        dragLine.startWidth = 3f;
        dragLine.endWidth = 6f;
        dragLine.positionCount = 2;
        dragLine.useWorldSpace = true;
        dragLine.sortingOrder = 2000;

        // 범위 라인
        var rangeGo = new GameObject("SkillRangeLine");
        rangeGo.transform.SetParent(transform);
        rangeLine = rangeGo.AddComponent<LineRenderer>();
        rangeLine.material = lineMaterial;
        rangeLine.widthMultiplier = 3f;
        rangeLine.positionCount = 0;
        rangeLine.useWorldSpace = true;
        rangeLine.sortingOrder = 1999;
    }

    void CreateCastingLineRenderer()
    {
        var go = new GameObject("CastingRangeLine");
        go.transform.SetParent(transform);
        castRangeLine = go.AddComponent<LineRenderer>();
        castRangeLine.material = lineMaterial;
        castRangeLine.widthMultiplier = 3f;
        castRangeLine.positionCount = 0;
        castRangeLine.useWorldSpace = true;
        castRangeLine.sortingOrder = 1998; // dragRange보다 뒤
    }

    void CreateArrow()
    {
        var go = new GameObject("SkillArrow");
        go.transform.SetParent(transform);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = arrowSprite;
        sr.sortingOrder = 2001;
        arrowTransform = go.transform;
    }

    // -------------------------------------------------
    // Slot Events
    // -------------------------------------------------

    public void OnSlotDown(SkillSlotUI slot)
    {
        var commander = EntityContainer.Instance.Commander;
        if (commander == null) return;

        currentSlot = slot;
        currentIndex = slot.SlotIndex;

        if (currentIndex < 0 || currentIndex >= commander.skills.Length)
        {
            ResetToIdle();
            return;
        }

        currentSkill = commander.skills[currentIndex];

        if (currentSkill == null)
        {
            ResetToIdle();
            return;
        }

        state = State.PressedDown;

        // 팝업 열기
        SkillUIManager.Instance.ShowPopup(currentSkill);
    }

    public void OnSlotExit(SkillSlotUI slot)
    {
        var commander = EntityContainer.Instance.Commander;
        if (state != State.PressedDown) return;
        if (commander == null || currentSkill == null) { ResetToIdle(); return; }

        bool onCooldown = commander.cooldowns[currentIndex] > 0f;
        bool enoughMana = commander.HasMana(currentSkill.manaCost);

        if (onCooldown)
        {
            UIMessage.Show("쿨다운 대기중입니다", 1f);
            ResetToIdle();
            return;
        }

        if (!enoughMana)
        {
            UIMessage.Show("마나가 부족합니다", 1f);
            ResetToIdle();
            return;
        }

        StartDrag();
    }

    public void OnSlotUp(SkillSlotUI slot)
    {
        if (state == State.PressedDown)
        {
            // 단순 클릭 → 팝업 유지
            ResetToIdle();
            return;
        }

        if (state == State.Dragging)
        {
            EndDrag();
        }
    }

    // -------------------------------------------------
    // Drag
    // -------------------------------------------------

    void StartDrag()
    {
        state = State.Dragging;

        slotScreenPos = currentSlot.transform.position;

        if (dragCursor != null)
        {
            dragCursor.gameObject.SetActive(true);
            dragCursor.position = slotScreenPos;
        }

        dragLine.enabled = true;
        dragLine.positionCount = 2;

        rangeLine.enabled = false;
        rangeLine.positionCount = 0;

        arrowTransform.gameObject.SetActive(true);

        SkillUIManager.Instance.ShowPopup(currentSkill);
    }

    void Update()
    {
        if (isCastingMode && castingSkill != null && castingSkill.targetType == SkillTargetType.Range)
        {
            DrawRangeCircle(castingPoint, castingSkill.radius, true);
            HighlightRange(castingPoint, castingSkill.radius, castingSkill, true);
        }

        if (state != State.Dragging)
        {
            if (SkillUIManager.Instance.popupUI.gameObject.activeSelf &&
                Input.GetMouseButtonDown(0) &&
                !EventSystem.current.IsPointerOverGameObject())
            {
                SkillUIManager.Instance.HidePopup();
            }
            return;
        }

        Vector3 mouseScreen = Input.mousePosition;
        Vector3 startWorld = ScreenToWorld(slotScreenPos);

        // ★ Raw world pos
        Vector3 endWorldRaw = ScreenToWorld(mouseScreen);

        // ★ Mobile Offset 적용된 최종 World 포인트
        Vector3 endWorld = endWorldRaw + worldOffset;

        // ★ Cursor도 offset된 위치를 기준으로 좌표 재산출
        Vector3 cursorScreen = Camera.main.WorldToScreenPoint(endWorld);
        if (dragCursor != null)
            dragCursor.position = cursorScreen;

        // ★ arrowDistance 계산
        Vector3 dir = (endWorld - startWorld).normalized;
        Vector3 endWorldShort = endWorld - dir * arrowDistance;

        // 라인 끝점을 축소된 지점으로 설정
        dragLine.SetPosition(0, startWorld);
        dragLine.SetPosition(1, endWorldShort);

        // 화살표도 축소된 지점으로 이동
        UpdateArrow(startWorld, endWorldShort);

        ClearAllOutline();
        HighlightPotentialTargets();

        // Skill drop point는 실제 endWorld
        currentPoint = endWorld;

        if (currentSkill.targetType == SkillTargetType.Range)
        {
            DrawRangeCircle(endWorld, currentSkill.radius, false);
            HighlightRange(endWorld, currentSkill.radius, currentSkill, false);
            currentTarget = null;
        }
        else
        {
            rangeLine.enabled = false;
            rangeLine.positionCount = 0;
            DetectCharacter(endWorld);
        }

        if (Input.GetMouseButtonUp(0))
        {
            EndDrag();
        }
    }


    void EndDrag()
    {
        if (dragCursor != null)
            dragCursor.gameObject.SetActive(false);

        dragLine.enabled = false;
        rangeLine.enabled = false;
        rangeLine.positionCount = 0;
        arrowTransform.gameObject.SetActive(false);

        ClearAllOutline();
        SkillUIManager.Instance.HidePopup();

        var commander = EntityContainer.Instance.Commander;
        if (commander == null || currentSkill == null)
        {
            ResetToIdle();
            return;
        }

        bool onCooldown = commander.cooldowns[currentIndex] > 0f;
        bool enoughMana = commander.HasMana(currentSkill.manaCost);

        if (onCooldown || !enoughMana)
        {
            ResetToIdle();
            return;
        }

        // ======================================================
        // SELF
        // ======================================================
        if (currentSkill.targetType == SkillTargetType.Self)
        {
            if (commander.IsCasting)
                commander.CancelCasting();

            if (currentSkill.useCasting)
            {
                commander.StartCasting(currentSkill, commander, Vector3.zero);
            }
            else
            {
                commander.TriggerSkill(currentIndex);
                SkillCaster.Instance.CastSkill(commander, currentSkill, commander);
            }

            ResetToIdle();
            return;
        }

        // ======================================================
        // SINGLE CHARACTER
        // ======================================================
        if (currentSkill.targetType == SkillTargetType.SingleCharacter)
        {
            if (currentTarget != null)
            {
                if (commander.IsCasting)
                    commander.CancelCasting();
                if (currentSkill.useCasting)
                {
                    commander.StartCasting(currentSkill, currentTarget, Vector3.zero);
                }
                else
                {
                    commander.TriggerSkill(currentIndex);
                    SkillCaster.Instance.CastSkill(commander, currentSkill, currentTarget);
                }
            }
            ResetToIdle();
            return;
        }

        // ======================================================
        // RANGE
        // ======================================================
        if (currentSkill.targetType == SkillTargetType.Range)
        {
            bool valid = false;

            foreach (var c in EntityContainer.Instance.Characters)
            {
                if (c.state == CharacterState.Die) continue;
                if (currentSkill.teamType == SkillTeamType.JustPoint)
                {
                    valid = true;
                    continue;
                }
                if (Vector2.Distance(c.transform.position, currentPoint) > currentSkill.radius) continue;

                switch (currentSkill.teamType)
                {
                    case SkillTeamType.Any:
                    case SkillTeamType.JustPoint:
                        valid = true;
                        break;
                    case SkillTeamType.PlayerTeam:
                        if (c.team == commander.team) valid = true;
                        break;
                    case SkillTeamType.EnemyTeam:
                        if (c.team != commander.team) valid = true;
                        break;
                }
                if (valid) break;
            }

            if (!currentSkill.useCasting && !valid)
            {
                ResetToIdle();
                return;
            }

            if (commander.IsCasting)
                commander.CancelCasting();
            if (currentSkill.useCasting)
            {
                commander.StartCasting(currentSkill, null, currentPoint);
            }
            else
            {
                commander.TriggerSkill(currentIndex);
                SkillCaster.Instance.CastSkill(commander, currentSkill, currentPoint);
            }

            ResetToIdle();
            return;
        }

        // 이 외의 경우
        ResetToIdle();
    }

    public void StartCastingMode(SkillData skill, CharacterBehaviour target, Vector3 point)
    {
        isCastingMode = true;
        castingSkill = skill;
        castingPoint = point;
        castingSlotScreenPos = slotScreenPos;

        if (skill.targetType == SkillTargetType.Range)
        {
            DrawRangeCircle(point, skill.radius, true);
            castRangeLine.enabled = true;
        }
    }

    public void EndCastingMode()
    {
        isCastingMode = false;
        castingSkill = null;
        castingSlotScreenPos = Vector3.zero;

        castRangeLine.enabled = false;
        castRangeLine.positionCount = 0;

        ClearAllOutline();
    }

    // -------------------------------------------------
    // Target / Range
    // -------------------------------------------------

    void DetectCharacter(Vector3 worldPos)
    {
        currentTarget = null;
        var hits = Physics2D.OverlapCircleAll(worldPos, detectRadius);

        foreach (var h in hits)
        {
            var c = h.GetComponent<CharacterBehaviour>();
            if (c == null) continue;
            if (!ValidateTeamType(c)) continue;

            currentTarget = c;
            var outline = c.GetComponentInChildren<SpriteOutline>();
            if (outline != null) outline.OutlineColor = Color.green;
            return;
        }
    }

    bool ValidateTeamType(CharacterBehaviour c)
    {
        SkillData s = isCastingMode ? castingSkill : currentSkill;

        switch (s.teamType)
        {
            case SkillTeamType.Any:
                return true;
            case SkillTeamType.PlayerTeam:
                return c.team == TeamType.Player;
            case SkillTeamType.EnemyTeam:
                return c.team == TeamType.Enemy;
            case SkillTeamType.JustPoint:
                return false;
        }
        return false;
    }


    void DrawRangeCircle(Vector3 center, float radius, bool isCasting)
    {
        LineRenderer line = isCasting ? castRangeLine : rangeLine;

        line.enabled = true;
        line.positionCount = CircleSegments + 1;

        float step = Mathf.PI * 2f / CircleSegments;
        for (int i = 0; i <= CircleSegments; i++)
        {
            float a = step * i;
            Vector3 p = center + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * radius;
            circlePoints[i] = p;
        }

        line.SetPositions(circlePoints);
    }

    void HighlightRange(Vector3 center, float radius, SkillData skill, bool isCasting)
    {
        var commander = EntityContainer.Instance.Commander;
        if (commander == null || skill == null) return;

        foreach (var c in EntityContainer.Instance.Characters)
        {
            if (c.state == CharacterState.Die) continue;

            float dist = Vector2.Distance(c.transform.position, center);
            if (dist > radius) continue;

            var o = c.GetComponentInChildren<SpriteOutline>();
            if (o == null) continue;

            switch (skill.teamType)
            {
                case SkillTeamType.Any:
                    o.OutlineColor = Color.green;
                    break;

                case SkillTeamType.PlayerTeam:
                    o.OutlineColor = (c.team == commander.team) ? Color.green : Color.clear;
                    break;

                case SkillTeamType.EnemyTeam:
                    o.OutlineColor = (c.team != commander.team) ? Color.red : Color.clear;
                    break;

                case SkillTeamType.JustPoint:
                    o.OutlineColor = Color.clear;
                    break;
            }
        }
    }

    void ClearAllOutline()
    {
        foreach (var c in EntityContainer.Instance.Characters)
        {
            if (c == null) continue;
            var o = c.GetComponentInChildren<SpriteOutline>();
            if (o == null) continue;
            o.OutlineColor = Color.clear;
        }
    }
    void HighlightPotentialTargets()
    {
        // Range는 기존 범위 방식 그대로 → 후보 표시 없음
        if (currentSkill.targetType == SkillTargetType.Range)
            return;

        foreach (var c in EntityContainer.Instance.Characters)
        {
            if (c.state == CharacterState.Die) continue;
            if (!ValidateTeamType(c)) continue;   // SkillTeamType 기반 필터

            var o = c.GetComponentInChildren<SpriteOutline>();
            if (o != null) o.OutlineColor = Color.white;  // ★ 후보군은 흰색
        }
    }

    // -------------------------------------------------
    // Utils
    // -------------------------------------------------

    Vector3 ScreenToWorld(Vector3 screenPos)
    {
        var cam = Camera.main;
        screenPos.z = pointDepth;
        return cam.ScreenToWorldPoint(screenPos);
    }

    public Vector3 GetSkillUiWorldPosition(SkillData skill, Vector3 fallbackWorldPosition)
    {
        if (skill == null)
            return fallbackWorldPosition;

        if (state == State.Dragging && currentSkill == skill)
            return ScreenToWorld(slotScreenPos);

        if (isCastingMode && castingSkill == skill)
            return ScreenToWorld(castingSlotScreenPos);

        return fallbackWorldPosition;
    }

    void UpdateArrow(Vector3 startWorld, Vector3 arrowPos)
    {
        arrowTransform.gameObject.SetActive(true);
        arrowTransform.position = arrowPos;

        Vector3 dir = arrowPos - startWorld;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        arrowTransform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    void ResetToIdle()
    {
        state = State.Idle;
        currentSlot = null;
        currentIndex = -1;
        currentSkill = null;
        currentTarget = null;
    }
}
