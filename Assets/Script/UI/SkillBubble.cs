using TMPro;
using UnityEngine;

public class SkillBubble : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] RectTransform rect;
    [SerializeField] TextMeshProUGUI text;

    CharacterBehaviour owner;

    float duration;
    float timer;
    Vector3 worldOffset;
    bool playing;

    public void Init(CharacterBehaviour owner)
    {
        this.owner = owner;
        playing = false;
        timer = 0f;

        if (rect == null)
            rect = GetComponent<RectTransform>();

        gameObject.SetActive(false);
    }

    public void Show(string msg, float duration, Vector3 worldOffset)
    {
        text.text = msg;

        this.duration = duration;
        this.worldOffset = worldOffset;

        timer = 0f;
        playing = true;

        gameObject.SetActive(true);
        UpdatePosition();
    }

    public void Hide()
    {
        playing = false;
        gameObject.SetActive(false);
    }

    void Update()
    {
        if (!playing) return;

        timer += Time.deltaTime;
        UpdatePosition();

        if (timer >= duration)
            Hide();
    }

    void UpdatePosition()
    {
        Vector3 worldPos = owner.transform.position + worldOffset;
        Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);
        rect.position = screenPos;
    }
}
