using System.Collections;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.UI.GridLayoutGroup;

public class HPBar : MonoBehaviour
{
    [HideInInspector]
    public CharacterBehaviour owner;
    public Vector3 offset = new Vector3(0, 50, 0);

    [Header("Images")]
    [SerializeField] Image fill;
    [SerializeField] Image delay;

    [Header("AutoSkill Cooldown (First Auto)")]
    [SerializeField] GameObject cooldownRoot;
    [SerializeField] Image characterSkillCooldown;

    [Header("BuffIcons")]
    public BuffIconManager buffIcons;       // 추가
    [SerializeField] Transform buffGridRoot;   // BuffGrid (프리팹에서 드래그)
    [SerializeField] GameObject buffIconPrefab;  // BuffIcon prefab
    public void Init(CharacterBehaviour owner)
    {
        this.owner = owner;

        // Buff아이콘 매니저 설정
        buffIcons = new BuffIconManager();
        buffIcons.Initialize(buffGridRoot, buffIconPrefab, owner, true);
    }

    void LateUpdate()
    {
        if (owner == null) return;

        // 기존 HP 위치 계산
        Vector3 worldPos = owner.transform.position + offset;
        Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);
        transform.position = screenPos;

        // Buff 아이콘 업데이트
        buffIcons?.Update();
    }


    Coroutine delayRoutine;
    public void OnHPChanged(int hp, int max)
    {
        float ratio = (float)hp / max;

        // Fill 즉시 반영
        fill.fillAmount = ratio;
        fill.color = GameVariables.Instance.hpGradient.Evaluate(ratio);

        // Delay 처리
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

        // 감속 이동
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / GameVariables.Instance.delaySpeed;
            delay.fillAmount = Mathf.Lerp(start, target, t);
            yield return null;
        }

        delay.fillAmount = target;
    }

    public void SetActive(bool v)
    {
        gameObject.SetActive(v);
    }
    public void OnCharacterSkillCooldownChanged(bool visible, float fillAmount)
    {
        if (characterSkillCooldown == null) return;

        characterSkillCooldown.gameObject.SetActive(visible);
        cooldownRoot.SetActive(visible);
        if (!visible) return;

        characterSkillCooldown.fillAmount = Mathf.Clamp01(fillAmount);
    }


}
