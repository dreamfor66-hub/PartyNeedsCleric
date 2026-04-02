using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OutgameDevPopup : MonoBehaviour
{
    [Header("Root")]
    public GameObject root;

    [Header("List (Spawn)")]
    public Transform gridParent;
    public EquipmentItemUI itemPrefab;

    [Header("Buttons")]
    public Button resetAccountButton;
    public Button grantAllOnceButton;
    public Button closeButton;
    private enum DevTab { Menu, Equipment, Character } // [추가]
    private DevTab currentTab;                         // [추가]

    [Header("Layouts")]                               // [추가]
    public GameObject menuLayoutRoot;                  // 1) Equipment/Character 버튼만 있는 루트
    public GameObject equipmentLayoutRoot;             // 2) 기존 장비 지급 UI 루트
    public GameObject characterLayoutRoot;             // 4) 캐릭터 지급 UI 루트

    [Header("Menu Buttons")]                           // [추가]
    public Button openEquipmentButton;
    public Button openCharacterButton;

    [Header("Back Buttons")]                           // [추가]
    public Button backFromEquipmentButton;
    public Button backFromCharacterButton;

    [Header("Character List (Spawn)")]                 // [추가]
    public Transform characterListParent;
    public OutgameCharacterItemUI characterItemPrefab;

    [Header("Character Inputs")]                       // [추가]
    public TMP_InputField characterNameInput;
    public TMP_InputField characterLevelInput;

    private readonly List<OutgameCharacterItemUI> spawnedCharacters = new(); // [추가]
    private EquipmentDataTable equipmentTable;
    private readonly List<EquipmentItemUI> spawned = new();

    public void Open(EquipmentDataTable table)
    {
        equipmentTable = table;

        if (root != null) root.SetActive(true);

        BindButtons();
        SetTab(DevTab.Menu);
    }

    public void Close()
    {
        if (root != null) root.SetActive(false);
        equipmentTable = null;

        ClearList();
        ClearCharacterList(); // [추가]
    }

    private void BindButtons()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(Close);
        }

        if (openEquipmentButton != null)
        {
            openEquipmentButton.onClick.RemoveAllListeners();
            openEquipmentButton.onClick.AddListener(() => SetTab(DevTab.Equipment));
        }

        if (openCharacterButton != null)
        {
            openCharacterButton.onClick.RemoveAllListeners();
            openCharacterButton.onClick.AddListener(() => SetTab(DevTab.Character));
        }

        if (backFromEquipmentButton != null)
        {
            backFromEquipmentButton.onClick.RemoveAllListeners();
            backFromEquipmentButton.onClick.AddListener(() => SetTab(DevTab.Menu));
        }

        if (backFromCharacterButton != null)
        {
            backFromCharacterButton.onClick.RemoveAllListeners();
            backFromCharacterButton.onClick.AddListener(() => SetTab(DevTab.Menu));
        }

        if (resetAccountButton != null)
        {
            resetAccountButton.onClick.RemoveAllListeners();
            resetAccountButton.onClick.AddListener(() =>
            {
                if (OutgameAccountManager.Instance == null) return;

                OutgameAccountManager.Instance.Dev_ResetAccount();
                OutgameUIManager.ShowToast("계정 초기화 완료");

                OutgameUIManager.Instance?.EquipmentPage?.RefreshAll();

                if (currentTab == DevTab.Equipment) RefreshList();
                else if (currentTab == DevTab.Character) RefreshCharacterList();
            });
        }

        if (grantAllOnceButton != null)
        {
            grantAllOnceButton.onClick.RemoveAllListeners();
            grantAllOnceButton.onClick.AddListener(OnClick_GrantAllOnce);
        }
    }

    private void OnClick_GrantAllOnce()
    {
        if (OutgameAccountManager.Instance == null) return;

        var all = GetAllEquipmentsFromTable(equipmentTable);

        for (int i = 0; i < all.Count; i++)
        {
            var d = all[i];
            if (d == null) continue;

            OutgameAccountManager.Instance.CreateEquipment(d.id);
        }

        OutgameAccountManager.Instance.Save();
        OutgameUIManager.ShowToast("모든 장비 1개씩 지급 완료");

        OutgameUIManager.Instance?.EquipmentPage?.RefreshAll();
        RefreshList();
    }


    private void RefreshList()
    {
        ClearList();

        var all = GetAllEquipmentsFromTable(equipmentTable);
        all.Sort((a, b) =>
        {
            if (a == null && b == null) return 0;
            if (a == null) return 1;
            if (b == null) return -1;

            int r = ((int)b.rarity).CompareTo((int)a.rarity);
            if (r != 0) return r;

            int s = ((int)a.slotType).CompareTo((int)b.slotType);
            if (s != 0) return s;

            return string.CompareOrdinal(a.id, b.id);
        });

        for (int i = 0; i < all.Count; i++)
        {
            var d = all[i];
            if (d == null) continue;

            var preview = new Equipment
            {
                instanceId = "preview_" + d.id,
                dataId = d.id,
                options = new List<EquipmentOption>()
            };
            preview.BindData();

            var ui = Instantiate(itemPrefab, gridParent);
            ui.Init(preview, false, -1, (eq, isEquipped, slotIndex) =>
            {
                if (OutgameAccountManager.Instance == null) return;
                if (eq == null) return;

                OutgameAccountManager.Instance.CreateEquipment(eq.dataId);

                OutgameAccountManager.Instance.Save();
                OutgameUIManager.ShowToast($"{(eq.Data != null ? eq.Data.equipmentName : eq.dataId)} 지급 완료");

                OutgameUIManager.Instance?.EquipmentPage?.RefreshAll();
            });

            spawned.Add(ui);
        }
    }


    private void ClearList()
    {
        for (int i = spawned.Count - 1; i >= 0; i--)
        {
            if (spawned[i] == null) continue;

            if (Application.isPlaying) Destroy(spawned[i].gameObject);
            else DestroyImmediate(spawned[i].gameObject);
        }
        spawned.Clear();
    }

    // EquipmentDataTable 구조 미제공이라, 최대한 안전하게 전체 리스트를 뽑아오는 방식
    private List<EquipmentData> GetAllEquipmentsFromTable(EquipmentDataTable table)
    {
        var result = new List<EquipmentData>();
        if (table == null || table.list == null) return result;

        for (int i = 0; i < table.list.Count; i++)
        {
            var d = table.list[i];
            if (d != null) result.Add(d);
        }

        return result;
    }
    private void SetTab(DevTab tab)
    {
        currentTab = tab;

        if (menuLayoutRoot != null) menuLayoutRoot.SetActive(tab == DevTab.Menu);
        if (equipmentLayoutRoot != null) equipmentLayoutRoot.SetActive(tab == DevTab.Equipment);
        if (characterLayoutRoot != null) characterLayoutRoot.SetActive(tab == DevTab.Character);

        if (tab == DevTab.Equipment)
            RefreshList();
        else if (tab == DevTab.Character)
            RefreshCharacterList();
    }
    private void RefreshCharacterList()
    {
        ClearCharacterList();

        var acc = OutgameAccountManager.Instance;
        if (acc == null) return;

        var table = acc.PlayableTable;
        if (table == null || table.characters == null) return;
        if (characterListParent == null || characterItemPrefab == null) return;

        var list = new List<CharacterData>();
        for (int i = 0; i < table.characters.Count; i++)
        {
            var cd = table.characters[i];
            if (cd != null) list.Add(cd);
        }

        list.Sort((a, b) =>
        {
            if (a == null && b == null) return 0;
            if (a == null) return 1;
            if (b == null) return -1;
            return string.CompareOrdinal(a.characterName, b.characterName);
        });

        for (int i = 0; i < list.Count; i++)
        {
            var cd = list[i];
            if (cd == null) continue;

            var preview = new Character
            {
                instanceId = "preview_" + cd.name,
                dataId = cd.name
            };
            preview.Init(cd, forcedNickname: cd.characterName, startLevel: 1);
            preview.BindData(table);

            var ui = Instantiate(characterItemPrefab, characterListParent);
            ui.Init(preview, false, -1, (ch, isEquipped, slotIndex) =>
            {
                var a = OutgameAccountManager.Instance;
                if (a == null) return;

                string nick = characterNameInput != null ? characterNameInput.text : "";
                if (string.IsNullOrEmpty(nick))
                    nick = cd.characterName;

                int lv = 1;
                if (characterLevelInput != null && !string.IsNullOrEmpty(characterLevelInput.text))
                {
                    if (!int.TryParse(characterLevelInput.text, out lv))
                        lv = 1;
                }

                a.Dev_GrantCharacter(cd, lv, nick);

                a.Save();
                OutgameUIManager.ShowToast($"{cd.characterName} 지급 완료");
                OutgameUIManager.Instance?.EquipmentPage?.RefreshAll();
            });

            spawnedCharacters.Add(ui);
        }
    }
    private void ClearCharacterList()
    {
        for (int i = spawnedCharacters.Count - 1; i >= 0; i--)
        {
            if (spawnedCharacters[i] == null) continue;

            if (Application.isPlaying) Destroy(spawnedCharacters[i].gameObject);
            else DestroyImmediate(spawnedCharacters[i].gameObject);
        }
        spawnedCharacters.Clear();
    }

}
