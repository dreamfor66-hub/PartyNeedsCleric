using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Linq;

public class CanvasHPBar : MonoBehaviour
{
    [Header("HP Bar")]
    [SerializeField] Image fill;
    [SerializeField] Image delay;

    [Header("Info UI")]
    [SerializeField] Image portrait;          // 캐릭터 이미지
    [SerializeField] TextMeshProUGUI nameText;
    [SerializeField] TextMeshProUGUI hpText;   // "120 / 200" 이런 표기

    [Header("BuffIcons")]
    public BuffIconManager buffIcons;
    [SerializeField] Transform buffGridRoot;
    [SerializeField] GameObject buffIconPrefab;

    CharacterBehaviour owner;
    Coroutine delayRoutine;

    // -----------------------------------------------------------
    // 초기 셋업
    // -----------------------------------------------------------
    public void Init(CharacterBehaviour owner)
    {
        this.owner = owner;

        // 캐릭터 ID 이미지
        portrait.sprite = owner.data.portrait;     // idleFront 이미지로 바꿔도 됨

        // 이름
        nameText.text = owner.data.name.Split('_').Last();

        // HP 숫자 갱신
        hpText.text = $"{owner.GetCurrentHealth()} / {owner.GetMaxHealth()}";

        // 초기 바
        float ratio = (float)owner.GetCurrentHealth() / owner.GetMaxHealth();
        fill.fillAmount = ratio;
        fill.color = GameVariables.Instance.hpGradient.Evaluate(ratio);
        delay.fillAmount = ratio;

        buffIcons = new BuffIconManager();
        buffIcons.Initialize(buffGridRoot, buffIconPrefab, owner, false);
    }
    void Update()
    {
        if (owner == null) return;

        buffIcons?.Update();
    }

    // -----------------------------------------------------------
    // HP 변화 시 갱신
    // -----------------------------------------------------------
    public void OnHPChanged(int cur, int max)
    {
        float ratio = (float)cur / max;

        hpText.text = $"{cur} / {max}";

        fill.fillAmount = ratio;
        fill.color = GameVariables.Instance.hpGradient.Evaluate(ratio);

        if (delayRoutine != null)
            StopCoroutine(delayRoutine);

        delayRoutine = StartCoroutine(DelayRoutine(ratio));
    }

    IEnumerator DelayRoutine(float target)
    {
        float hold = GameVariables.Instance.delayHold;
        float start = delay.fillAmount;

        // 유지
        while (hold > 0f)
        {
            hold -= Time.deltaTime;
            yield return null;
        }

        // 감속
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / GameVariables.Instance.delaySpeed;
            float v = Mathf.Lerp(start, target, t);
            delay.fillAmount = v;
            yield return null;
        }

        delay.fillAmount = target;
    }
}
