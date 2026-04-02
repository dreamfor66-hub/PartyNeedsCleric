using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class SkillSlotUI : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [Header("UI")]
    public Image icon;
    public Image cooldownMask;
    public Image costMask;
    public TextMeshProUGUI cooldownText;

    public int SlotIndex { get; set; }

    SkillData skill;
    float currentCooldown;
    bool hasMana;

    public void UpdateState(SkillData data, float cooldownRemaining, float cooldownDuration, bool hasManaNow)
    {
        skill = data;
        currentCooldown = cooldownRemaining;
        hasMana = hasManaNow;

        if (skill == null)
        {
            icon.gameObject.SetActive(false);
            cooldownMask.gameObject.SetActive(false);
            costMask.gameObject.SetActive(false);
            cooldownText.text = "";
            return;
        }

        icon.gameObject.SetActive(true);
        icon.sprite = skill.icon;

        // 쿨다운 (감소된 cooldownDuration 기준)
        if (currentCooldown > 0f && cooldownDuration > 0f)
        {
            cooldownMask.gameObject.SetActive(true);
            float ratio = Mathf.Clamp01(currentCooldown / cooldownDuration);
            cooldownMask.fillAmount = ratio;
            cooldownText.text = Mathf.CeilToInt(currentCooldown).ToString();
        }
        else
        {
            cooldownMask.gameObject.SetActive(false);
            cooldownText.text = "";
        }

        // 마나 부족 마스크 (단순 on/off)
        costMask.gameObject.SetActive(!hasMana);
    }

    public SkillData GetSkill() => skill;
    public float GetCooldown() => currentCooldown;
    public bool HasMana() => hasMana;

    public void OnPointerDown(PointerEventData eventData)
    {
        SkillInputController.Instance.OnSlotDown(this);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SkillInputController.Instance.OnSlotExit(this);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        SkillInputController.Instance.OnSlotUp(this);
    }
}
