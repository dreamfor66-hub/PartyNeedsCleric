using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class DamageUIManager : MonoBehaviour
{
    public static DamageUIManager Instance { get; private set; }

    [SerializeField] GameObject damageUIPrefab;
    [SerializeField] int poolCount = 20;

    Queue<DamageUI> pool = new Queue<DamageUI>();

    void Awake()
    {
        Instance = this;

        for (int i = 0; i < poolCount; i++)
        {
            var obj = Instantiate(damageUIPrefab, transform);
            obj.SetActive(false);
            pool.Enqueue(obj.GetComponent<DamageUI>());
        }
    }

    DamageUI Get()
    {
        if (pool.Count > 0)
            return pool.Dequeue();

        var obj = Instantiate(damageUIPrefab, transform);
        return obj.GetComponent<DamageUI>();
    }

    public void Show(int value, Vector3 worldPos, Color color, float scale)
    {
        DamageUI ui = Get();

        ui.gameObject.SetActive(true);
        ui.Setup(value, worldPos, color, scale, () => pool.Enqueue(ui));
    }
}
