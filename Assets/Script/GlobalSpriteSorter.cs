using System.Collections.Generic;
using UnityEngine;

public class GlobalSpriteSorter : MonoBehaviour
{
    public static GlobalSpriteSorter Instance { get; private set; }

    // 정렬 대상 SpriteRenderer
    readonly List<SpriteRenderer> list = new List<SpriteRenderer>();

    void Awake()
    {
        Instance = this;
    }

    public void RegisterRenderer(SpriteRenderer sr)
    {
        if (sr == null) return;
        if (!list.Contains(sr))
            list.Add(sr);
    }

    public void UnregisterRenderer(SpriteRenderer sr)
    {
        if (sr == null) return;
        list.Remove(sr);
    }

    void LateUpdate()
    {
        // 1) null 제거
        list.RemoveAll(x => x == null);

        // 2) 월드 y 기준으로 오름차순 정렬(뒤→앞)
        list.Sort((a, b) =>
        {
            float ay = a.transform.position.y;
            float by = b.transform.position.y;
            return by.CompareTo(ay); // 내림차순: y 큰 게 먼저, y 작은 게 나중
        });

        // 3) index 기반 sortingOrder 부여
        for (int i = 0; i < list.Count; i++)
            list[i].sortingOrder = i;
    }
}
