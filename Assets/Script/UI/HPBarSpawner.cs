using UnityEngine;

public class HPBarSpawner : MonoBehaviour
{
    public static HPBarSpawner Instance;

    [Header("Prefabs")]
    public HPBar worldBarPrefab;
    public CanvasHPBar uiBarPrefab;

    [Header("Parents")]
    public Transform worldParent;
    public Transform uiParent;


    void Awake()
    {
        Instance = this;
        for (int i = uiParent.childCount - 1; i >= 0; i--)
            Destroy(uiParent.GetChild(i).gameObject);
    }

    public HPBar SpawnWorldBar(CharacterBehaviour owner)
    {
        var bar = Instantiate(worldBarPrefab, worldParent);
        bar.Init(owner);
        return bar;
    }

    public CanvasHPBar SpawnUIBar(CharacterBehaviour owner)
    {
        var bar = Instantiate(uiBarPrefab, uiParent);
        bar.Init(owner);
        return bar;
    }
}
