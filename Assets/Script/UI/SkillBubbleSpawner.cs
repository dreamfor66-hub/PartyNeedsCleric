using System.Collections.Generic;
using UnityEngine;

public class SkillBubbleSpawner : MonoBehaviour
{
    public static SkillBubbleSpawner Instance;

    [Header("Prefab")]
    public SkillBubble uiBubblePrefab;

    [Header("Parent")]
    public Transform uiParent;

    [Header("Reference")]
    public HPBarSpawner hpBarSpawner;

    [Header("Offset (World)")]
    public Vector3 extraWorldOffset = new Vector3(0, 20f, 0); // HPBar보다 더 위로

    [Header("Default")]
    public float defaultDuration = 1f;

    readonly Dictionary<CharacterBehaviour, SkillBubble> active = new();

    void Awake()
    {
        Instance = this;

        for (int i = uiParent.childCount - 1; i >= 0; i--)
            Destroy(uiParent.GetChild(i).gameObject);

        active.Clear();
    }

    public void Show(CharacterBehaviour owner, string text)
    {
        Show(owner, text, defaultDuration);
    }

    public void Show(CharacterBehaviour owner, string text, float duration)
    {
        if (!active.TryGetValue(owner, out var bubble) || bubble == null)
        {
            bubble = Instantiate(uiBubblePrefab, uiParent);
            bubble.Init(owner);
            active[owner] = bubble;
        }

        // HPBar의 "월드 오프셋"을 그대로 기준으로 사용 (고정 ScreenOffsetY 아님)
        Vector3 baseOffset = hpBarSpawner.worldBarPrefab.offset;
        bubble.Show(text, duration, baseOffset + extraWorldOffset);
    }

    public void Hide(CharacterBehaviour owner)
    {
        if (!active.TryGetValue(owner, out var bubble) || bubble == null)
            return;

        bubble.Hide();
    }
}
