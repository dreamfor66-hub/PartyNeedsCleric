using System.Collections.Generic;
using UnityEngine;

public class ToastMessageUI : MonoBehaviour
{
    [Header("Item Prefab (Project prefab: UI_ToastMessage)")]
    public ToastMessageItem itemPrefab;

    [Header("Root (default: self)")]
    public Transform root;

    [Header("Settings")]
    public float duration = 1.2f;

    [Tooltip("처음에 미리 생성해 둘 개수")]
    public int prewarmCount = 3;

    readonly Stack<ToastMessageItem> pool = new Stack<ToastMessageItem>();

    void Awake()
    {
        if (root == null) root = transform;

        // 이미 하위에 만들어진 Clone들(비활성 포함)을 풀에 넣어 재사용
        var existing = root.GetComponentsInChildren<ToastMessageItem>(true);
        for (int i = 0; i < existing.Length; i++)
        {
            var item = existing[i];
            if (item == null) continue;
            if (item.transform == root) continue;

            item.ForceStop();
            item.gameObject.SetActive(false);
            pool.Push(item);
        }

        // 프리팹이 없으면 풀에서 하나를 템플릿으로 사용(없으면 에러)
        if (itemPrefab == null)
        {
            if (pool.Count > 0)
            {
                itemPrefab = pool.Peek(); // 씬 오브젝트 템플릿도 Instantiate 가능
            }
            else
            {
                Debug.LogError("[ToastMessageUI] itemPrefab이 비어있고, 하위에 ToastMessageItem도 없습니다.");
                return;
            }
        }

        // 프리워밍: prewarmCount 만큼 "추가로" 확보 (이미 existing을 풀에 넣었으니 부족분만 채움)
        int need = Mathf.Max(0, prewarmCount - pool.Count);
        for (int i = 0; i < need; i++)
        {
            var item = CreateNew();
            pool.Push(item);
        }
    }

    ToastMessageItem CreateNew()
    {
        var item = Instantiate(itemPrefab, root);
        item.name = itemPrefab.name + "_Pooled";
        item.ForceStop();
        item.gameObject.SetActive(false);
        return item;
    }

    ToastMessageItem RentOrCreate()
    {
        if (pool.Count > 0) return pool.Pop();
        // 모두 실행 중이면 추가 생성
        return CreateNew();
    }

    void Return(ToastMessageItem item)
    {
        if (item == null) return;
        item.ForceStop();
        item.gameObject.SetActive(false);
        pool.Push(item);
    }

    public void Show(string message)
    {
        if (string.IsNullOrEmpty(message)) return;

        var item = RentOrCreate();

        // (요청 스펙 순서) 활성화 -> text 변경 -> 애니 실행은 ToastMessageItem.Play 내부에서 처리
        item.Play(message, duration, () =>
        {
            Return(item);
        });
    }
}
