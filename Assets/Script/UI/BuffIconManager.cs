using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BuffIconManager
{
    Transform root;
    GameObject prefab;
    CharacterBehaviour owner;

    bool isPopupReversed = true;

    Dictionary<BuffInstance, BuffIconUI> icons = new();

    public void Initialize(Transform root, GameObject prefab, CharacterBehaviour owner, bool isPopupReversed)
    {
        this.root = root;
        this.prefab = prefab;
        this.owner = owner;
        this.isPopupReversed = isPopupReversed;
    }

    public void Update()
    {
        if (owner == null) return;

        var list = owner.Buffs.GetActiveInstances();

        // 제거된 아이콘 정리
        foreach (var inst in icons.Keys.ToList())
        {
            if (!list.Contains(inst))
            {
                GameObject.Destroy(icons[inst].root);
                icons.Remove(inst);
            }
        }

        // 새로운 버프 생성
        foreach (var inst in list)
        {
            if (!inst.Data.useVisual) continue;

            if (!icons.ContainsKey(inst))
                icons[inst] = Create(inst);
        }

        // DurationFill / Stack 갱신
        foreach (var inst in list)
        {
            if (!icons.ContainsKey(inst)) continue;
            UpdateIcon(inst, icons[inst]);
        }

        // 외부 클릭 시 팝업 닫기
        if (Input.GetMouseButtonDown(0))
        {
            foreach (var ui in icons.Values)
                ui.popup.SetActive(false);
        }
    }

    BuffIconUI Create(BuffInstance inst)
    {
        var go = GameObject.Instantiate(prefab, root);
        var refs = go.GetComponent<BuffIconUIRefs>();

        var ui = new BuffIconUI(go, refs);
        ui.icon.sprite = inst.Data.icon;
        ui.stack.text = "";

        // 팝업 세팅
        ui.popupName.text = inst.Data.buffName;
        ui.popupDesc.text = inst.Data.buffDesc;
        var popupRect = refs.popup.GetComponent<RectTransform>();
        popupRect.anchorMin = new Vector2(0, isPopupReversed? 1 : 0);
        popupRect.anchorMax = new Vector2(0, isPopupReversed ? 1 : 0);
        popupRect.pivot = new Vector2(0, isPopupReversed ? 0 : 1);

        // 버튼 클릭 → 팝업 토글
        refs.button.onClick.AddListener(() =>
        {
            ui.popup.SetActive(!ui.popup.activeSelf);
        });

        return ui;
    }

    void UpdateIcon(BuffInstance inst, BuffIconUI ui)
    {
        // 스택
        ui.stack.text = inst.Stacks > 1 ? inst.Stacks.ToString() : "";

        // Duration
        if (inst.Data.removeCondition == BuffRemoveCondition.Duration)
        {
            float t = Mathf.Clamp01(inst.TimeLeft / inst.Data.duration);
            ui.duration.fillAmount = t;
        }
        else
        {
            ui.duration.fillAmount = 0f;
        }
    }
}
