using System.Collections.Generic;
using UnityEngine;

public class SkillUIManager : MonoBehaviour
{
    public static SkillUIManager Instance { get; private set; }



    [Header("Slot")]
    public SkillSlotUI slotPrefab;      // PF_SkillUI
    public Transform slotParent;        // 슬롯을 깔 위치 (가로 5칸 등)

    [Header("Popup")]
    public SkillPopupUI popupUI;   // ← Init만 담당

    SkillSlotUI[] slots = new SkillSlotUI[5];

    void Awake()
    {
        Instance = this;
    }
    void Start()
    {
        BuildSlots();
        HidePopup();
    }

    void Update()
    {
        var commander = EntityContainer.Instance.Commander;
        if (commander == null) return;
        if (commander.skills == null || commander.cooldowns == null) return;

        for (int i = 0; i < slots.Length; i++)
        {
            SkillData data = (i < commander.skills.Length) ? commander.skills[i] : null;
            float cd = (i < commander.cooldowns.Length) ? Mathf.Max(0f, commander.cooldowns[i]) : 0f;

            bool hasMana = true;
            float cooldownDuration = 0f;

            if (data != null)
            {
                hasMana = commander.HasMana(data.manaCost);

                float haste = commander.GetSkillCooldownHaste(); // 기본 100
                float tagHaste = commander.SkillTagCooldownHasteSum(data.skillTags);
                float totalHaste = Mathf.Max(1f, haste + tagHaste);

                cooldownDuration = data.cooldown * 100f / totalHaste;
            }

            slots[i].UpdateState(data, cd, cooldownDuration, hasMana);
        }
    }

    void BuildSlots()
    {
        // 기존 자식 정리
        for (int i = slotParent.childCount - 1; i >= 0; i--)
            Destroy(slotParent.GetChild(i).gameObject);

        for (int i = 0; i < slots.Length; i++)
        {
            var ui = Instantiate(slotPrefab, slotParent);
            ui.SlotIndex = i;
            slots[i] = ui;
        }
    }

    public void ShowPopup(SkillData s)
    {
        popupUI.gameObject.SetActive(true);
        popupUI.Init(s);
    }

    public void HidePopup()
    {
        popupUI.gameObject.SetActive(false);
    }
}
