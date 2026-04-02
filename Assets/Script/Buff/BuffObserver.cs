using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

[DefaultExecutionOrder(-9999)]
public class BuffObserver : MonoBehaviour
{
    private static BuffObserver _instance;
    public static BuffObserver Instance
    {
        get
        {
            if (_instance == null)
                CreateInstance();
            return _instance;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoInit() => CreateInstance();

    private static void CreateInstance()
    {
        if (_instance != null) return;
        var go = new GameObject("BuffObserver");
        _instance = go.AddComponent<BuffObserver>();
        DontDestroyOnLoad(go);
    }

    [System.Serializable]
    public class ObservedBuff
    {
        [TableColumnWidth(120, Resizable = false)]
        [ReadOnly] public CharacterBehaviour owner;

        [InlineEditor(InlineEditorObjectFieldModes.Foldout)]
        [ReadOnly] public BuffData buffData;

        [TableColumnWidth(60, Resizable = false)]
        [ReadOnly] public int stacks;
    }

    [TableList(IsReadOnly = true)]
    [SerializeField] private List<ObservedBuff> activeBuffs = new();

    void Update()
    {
        activeBuffs.Clear();
        if (EntityContainer.Instance == null) return;

        foreach (var c in EntityContainer.Instance.Characters)
        {
            if (c == null || c.Buffs == null) continue;

            var list = c.Buffs.GetActiveInstances();
            foreach (var b in list)
            {
                if (b == null || b.Data == null) continue;
                activeBuffs.Add(new ObservedBuff
                {
                    owner = c,
                    buffData = b.Data,
                    stacks = b.Stacks
                });
            }
        }
    }
}
