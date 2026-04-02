using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OutgamePartySlotUI : MonoBehaviour
{
    public Button button;

    public Image image;               // √ﬂ∞°
    public float swapPeriod = 0.35f;  // √ﬂ∞°

    public TextMeshProUGUI levelNameText; // "Lv.xx {CharacterData.characterName}"
    public TextMeshProUGUI nickNameText; // "{Character.nickname}"
    public GameObject equippedRoot;
    public GameObject unEquippedRoot;

    public int SlotIndex { get; private set; }
    public Character Character { get; private set; }

    private Action<int> onClick;

    // Idle swap cache (√ﬂ∞°)
    private Sprite a;
    private Sprite b;
    private float t;

    public void Init(int slotIndex, Action<int> onClickSlot)
    {
        SlotIndex = slotIndex;
        onClick = onClickSlot;

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClick?.Invoke(SlotIndex));
        }
    }

    // OutgamePartySlotUI.cs / Set(Character ch) ∏ﬁº≠µÂ ±≥√º

    public void Set(Character ch)
    {
        Character = ch;

        // ∞°¿Â ≈´ ∫–±‚: ¿Â¬¯ / πÃ¿Â¬¯
        if (ch.dataId != "")
        {
            // ===== ¿Â¬¯ ΩΩ∑‘ =====
            if (equippedRoot != null) equippedRoot.SetActive(true);
            if (unEquippedRoot != null) unEquippedRoot.SetActive(false);

            if (levelNameText != null)
                levelNameText.text = $"Lv.{ch.level} {ch.Data.characterName}";

            if (nickNameText != null)
                nickNameText.text = ch.nickname;

            a = ch.Data.sprites.anim_idleFront;
            b = ch.Data.sprites.anim_idleBack;

            if (image != null)
            {
                image.enabled = (a != null || b != null);
                image.sprite = a != null ? a : b;
            }

            t = 0f;
        }
        else
        {
            // ===== πÃ¿Â¬¯(∫Û ΩΩ∑‘) =====
            if (equippedRoot != null) equippedRoot.SetActive(false);
            if (unEquippedRoot != null) unEquippedRoot.SetActive(true);

            if (image != null)
            {
                image.sprite = null;
                image.enabled = false;
            }

            if (levelNameText != null) levelNameText.text = "";
            if (nickNameText != null) nickNameText.text = "";

            a = null;
            b = null;
            t = 0f;
        }
    }


}
