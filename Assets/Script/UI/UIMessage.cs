using UnityEngine;
using TMPro;

public class UIMessage : MonoBehaviour
{
    public static UIMessage Instance;

    [SerializeField] TextMeshProUGUI text;
    float timer = 0f;

    void Awake()
    {
        Instance = this;
        text.gameObject.SetActive(false);
    }

    void Update()
    {
        if (timer > 0f)
        {
            timer -= Time.deltaTime;
            if (timer <= 0f)
                text.gameObject.SetActive(false);
        }
    }

    public static void Show(string msg, float duration)
    {
        if (Instance == null) return;

        Instance.text.text = msg;
        Instance.text.gameObject.SetActive(true);
        Instance.timer = duration;
    }
}
