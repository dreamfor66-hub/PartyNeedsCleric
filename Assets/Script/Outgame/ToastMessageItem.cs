using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class ToastMessageItem : MonoBehaviour
{
    public Animator animator;
    public TextMeshProUGUI text;

    Coroutine co;

    public void Play(string message, float duration, Action onDone)
    {
        gameObject.SetActive(true);

        if (text != null)
            text.text = message;

        if (animator != null)
            animator.Play("Show", 0, 0f);

        if (co != null) StopCoroutine(co);
        co = StartCoroutine(CoHide(duration, onDone));
    }

    IEnumerator CoHide(float duration, Action onDone)
    {
        yield return new WaitForSeconds(duration);
        co = null;

        // 여기서 비활성화는 하되, 풀 반환은 UI 매니저가 담당
        gameObject.SetActive(false);
        onDone?.Invoke();
    }

    public void ForceStop()
    {
        if (co != null)
        {
            StopCoroutine(co);
            co = null;
        }
        gameObject.SetActive(false);
    }
}
