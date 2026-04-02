using UnityEngine;
using TMPro;
using System;
using UnityEngine.UI;

public class DamageUI : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI text;
    Action onFinished;

    public void Setup(int value, Vector3 worldPos, Color color, float scale, Action cb)
    {
        onFinished = cb;

        Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);
        transform.position = screenPos;

        text.text = value.ToString();
        text.color = color;
        transform.localScale = Vector3.one * scale;

        StopAllCoroutines();
        StartCoroutine(PlayAnim());
    }

    System.Collections.IEnumerator PlayAnim()
    {
        float t = 0f;
        Vector3 start = transform.position;
        Vector3 end = start + new Vector3(0, 80f, 0);

        while (t < 1f)
        {
            t += Time.deltaTime * 1.5f;
            transform.position = Vector3.Lerp(start, end, t);
            yield return null;
        }

        gameObject.SetActive(false);
        onFinished?.Invoke();
    }
}
