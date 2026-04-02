// Assets/Script/Outgame/OutgameAccountManager.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using UnityEngine;

public class OutgameAccountManager : MonoBehaviour
{
    public static OutgameAccountManager Instance { get; private set; }

    [Header("Tables (optional)")]
    [SerializeField] private PlayableDataTable playableTable;
    [SerializeField] private EquipmentDataTable equipmentTable;

    public PlayableDataTable PlayableTable => playableTable;
    public EquipmentDataTable EquipmentTable => equipmentTable;

    // ===== Runtime Current =====
    public CharacterData CurrentCommander { get; private set; }

    // OutgameEquippedInfoUI.cs가 요구하는 프로퍼티
    public SkillData[] CurrentSkills
    {
        get
        {
            Load();
            return GetCurrentSkillsForGame();
        }
    }

    // ===== Save =====
    private const string SAVE_FILE = "outgame_account.json";
    private string SavePath => Path.Combine(Application.persistentDataPath, SAVE_FILE);

    [SerializeField] private OutgameAccountSave save;

    // ===== Cache =====
    private List<Equipment> ownedEquipments = new();
    private Equipment[] equippedEquipment = new Equipment[6];
    private List<CommanderSkillPresetSave> commanderPresets = new();
    private List<Character> ownedCharacters = new();
    private Character[] equippedCharacters = new Character[3];
    public int AccountLevel
    {
        get
        {
            Load();
            return save.accountLevel;
        }
    }
    // =====================================================================
    // Unity
    // =====================================================================
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // =====================================================================
    // Init (기존 시그니처 유지 + 호출 다양성 대응)
    // =====================================================================

    public void Init(PlayableDataTable pt, EquipmentDataTable et)
    {
        playableTable = pt;
        equipmentTable = et;
        Load();
    }

    // =====================================================================
    // Load / Save
    // =====================================================================
    public void Load()
    {
        if (File.Exists(SavePath))
        {
            try
            {
                save = JsonUtility.FromJson<OutgameAccountSave>(File.ReadAllText(SavePath));
            }
            catch
            {
                save = null;
            }
        }

        if (save == null)
            save = new OutgameAccountSave();

        if (save.ownedEquipments == null)
            save.ownedEquipments = new List<Equipment>();
        if (save.equippedEquipment == null || save.equippedEquipment.Length != 6)
            save.equippedEquipment = new Equipment[6];
        if (save.commanderPresets == null)
            save.commanderPresets = new List<CommanderSkillPresetSave>();
        if (save.ownedCharacters == null)
            save.ownedCharacters = new List<Character>();
        if (save.equippedCharacters == null || save.equippedCharacters.Length != 3)
            save.equippedCharacters = new Character[3];

        ownedEquipments = save.ownedEquipments;
        equippedEquipment = save.equippedEquipment;
        commanderPresets = save.commanderPresets;
        ownedCharacters = save.ownedCharacters;
        equippedCharacters = save.equippedCharacters;

        EnsureDefaultCharacterSeed();

        BindAllEquipmentData();
        BindAllCharacterData();

        RefreshCurrentCommander();
    }

    public void Save()
    {
        if (save == null)
            save = new OutgameAccountSave();

        if (ownedEquipments == null)
            ownedEquipments = new List<Equipment>();

        if (equippedEquipment == null || equippedEquipment.Length != 6)
            equippedEquipment = new Equipment[6];

        if (commanderPresets == null)
            commanderPresets = new List<CommanderSkillPresetSave>();

        if (ownedCharacters == null)
            ownedCharacters = new List<Character>();

        if (equippedCharacters == null || equippedCharacters.Length != 3)
            equippedCharacters = new Character[3];

        save.ownedEquipments = ownedEquipments;
        save.equippedEquipment = equippedEquipment;
        save.commanderPresets = commanderPresets;
        save.currentCommanderId = CurrentCommander != null ? CurrentCommander.name : save.currentCommanderId;
        save.ownedCharacters = ownedCharacters;
        save.equippedCharacters = equippedCharacters;

        try
        {
            File.WriteAllText(SavePath, JsonUtility.ToJson(save, true));
        }
        catch (Exception e)
        {
            Debug.LogError($"OutgameAccount Save failed: {e}");
        }
    }

    private void RefreshCurrentCommander()
    {
        if (CurrentCommander != null) return;

        if (playableTable == null || playableTable.commanders == null || playableTable.commanders.Count == 0)
            return;

        if (!string.IsNullOrEmpty(save.currentCommanderId))
        {
            for (int i = 0; i < playableTable.commanders.Count; i++)
            {
                var cd = playableTable.commanders[i];
                if (cd == null) continue;

                if (cd.name == save.currentCommanderId)
                {
                    CurrentCommander = cd;
                    return;
                }
            }
        }

        CurrentCommander = playableTable.commanders[0];

        if (CurrentCommander != null && string.IsNullOrEmpty(save.currentCommanderId))
            save.currentCommanderId = CurrentCommander.name;
    }


    public void Dev_ResetAccount()
    {
        save = new OutgameAccountSave
        {
            currentCommanderId = "",
            ownedEquipments = new List<Equipment>(),
            equippedEquipment = new Equipment[6],
            commanderPresets = new List<CommanderSkillPresetSave>(),
            ownedCharacters = new List<Character>(),
            equippedCharacters = new Character[3],
        };

        ownedEquipments = save.ownedEquipments;
        equippedEquipment = save.equippedEquipment;
        commanderPresets = save.commanderPresets;
        ownedCharacters = save.ownedCharacters;
        equippedCharacters = save.equippedCharacters;

        CurrentCommander = null;
        Save();
    }

    // =====================================================================
    // Commander
    // =====================================================================
    public void SetCommander(CharacterData commander)
    {
        Load();
        CurrentCommander = commander;
        save.currentCommanderId = commander != null ? commander.name : "";
        Save();
    }

    // =====================================================================
    // Owned Equipments
    // =====================================================================
    public List<Equipment> GetOwnedEquipments()
    {
        Load();
        BindAllEquipmentData();
        return ownedEquipments;
    }

    public Equipment[] GetEquippedEquipments()
    {
        Load();
        BindAllEquipmentData();

        return equippedEquipment;
    }
    public Equipment GetEquippedEquipmentBySlot(int slotIndex)
    {
        Load();
        if (slotIndex < 0) return null;

        BindAllEquipmentData();
        return equippedEquipment[slotIndex];
    }

    public void AddOwnedEquipment(Equipment equipment)
    {
        Load();
        if (equipment == null) return;

        equipment.EnsureIds();

        equipment.BindData();

        // 소유 등록(=획득) 시점에만 굴림 확정
        equipment.RollAtAcquire();

        ownedEquipments.Add(equipment);

        Save();
    }

    public Equipment FindOwnedEquipmentByInstanceId(string instanceId)
    {
        if (string.IsNullOrEmpty(instanceId)) return null;
        for (int i = 0; i < ownedEquipments.Count; i++)
        {
            var e = ownedEquipments[i];
            if (e != null && e.instanceId == instanceId)
                return e;
        }
        return null;
    }

    // =====================================================================
    // Create Equipment (DevPopup 요구)
    // =====================================================================
    public Equipment CreateEquipment(string dataId)
    {
        Load();

        var e = new Equipment
        {
            instanceId = Guid.NewGuid().ToString("N"),
            dataId = dataId,
            options = new List<EquipmentOption>()
        };

        e.BindData();

        // 생성(=지급/획득) 시점에만 굴림 확정
        e.RollAtAcquire();

        AddOwnedEquipment(e);

        return e;
    }
    public void Dev_GrantCharacter(CharacterData cd, int level, string nickname)
    {
        if (cd == null) return;

        Load();

        var ch = new Character
        {
            instanceId = Guid.NewGuid().ToString("N"),
            dataId = cd.name,
            level = Mathf.Max(1, level),
            nickname = nickname
        };

        RollRandomCompanionLoadout(ch, cd);

        ownedCharacters.Add(ch);
        Save();
    }


    // =====================================================================
    // Equip / Unequip (기존 호출 패턴 전부 수용)
    // =====================================================================
    public enum EquipResult { Failed = 0, Success = 1, NeedAccessoryChoice = 2 }

    // EquipmentPopup.cs / EquipmentPage.cs에서 (Equipment, int)로 호출하는 케이스
    public EquipResult EquipEquipment(Equipment equipment, int slotIndex)
    {
        Load();

        if (equipment == null) return EquipResult.Failed;

        if (equipment.Data == null)
            equipment.BindData();

        if (equipment.Data == null) return EquipResult.Failed;

        if (equippedEquipment == null || equippedEquipment.Length != 6)
            equippedEquipment = new Equipment[6];

        if (equipment.Data.slotType != EquipmentSlotType.Ring)
        {
            int idx = SlotIndexFromType(equipment.Data.slotType);
            if (idx < 0) return EquipResult.Failed;

            equippedEquipment[idx] = equipment;
            return EquipResult.Success;
        }

        if (slotIndex == 4 || slotIndex == 5)
        {
            equippedEquipment[slotIndex] = equipment;
            return EquipResult.Success;
        }

        if (equippedEquipment[4] == null || string.IsNullOrEmpty(equippedEquipment[4].instanceId))
        {
            equippedEquipment[4] = equipment;
            return EquipResult.Success;
        }

        if (equippedEquipment[5] == null || string.IsNullOrEmpty(equippedEquipment[5].instanceId))
        {
            equippedEquipment[5] = equipment;
            return EquipResult.Success;
        }

        return EquipResult.NeedAccessoryChoice;
    }


    public void UnequipEquipment(int slotIndex)
    {
        Load();
        if (slotIndex < 0 || slotIndex >= equippedEquipment.Length)
            return;
        equippedEquipment[slotIndex] = null;
        Save();
    }


    // =====================================================================
    // Skills (GameSceneLoadoutApplier 요구)
    // =====================================================================
    public SkillData[] GetCurrentSkillsForGame()
    {
        Load();

        var skills = GetPreset(CurrentCommander);
        return skills;
    }

    public SkillData[] GetPreset(CharacterData commander)
    {
        var arr = new SkillData[6];

        if (commander == null)
            return arr;

        // 프리셋이 "존재"하면 무조건 프리셋 사용 (한 번이라도 저장된 기록이 있으면 그걸 우선)
        var preset = commanderPresets.Find(p => p != null && p.commanderId == commander.name);
        if (preset != null && preset.skillIds != null && preset.skillIds.Length == 6)
        {
            if (playableTable == null || playableTable.skills == null || playableTable.skills.Count == 0)
                return arr;

            for (int i = 0; i < 6; i++)
            {
                var id = preset.skillIds[i];
                if (string.IsNullOrEmpty(id))
                {
                    arr[i] = null;
                    continue;
                }

                SkillData found = null;
                for (int j = 0; j < playableTable.skills.Count; j++)
                {
                    var sd = playableTable.skills[j];
                    if (sd == null) continue;

                    if (sd.skillId == id)
                    {
                        found = sd;
                        break;
                    }
                }

                arr[i] = found;
            }

            return arr;
        }

        // 프리셋이 없으면 BasicSkills 사용
        if (commander.BasicSkills != null)
        {
            for (int i = 0; i < 6; i++)
            {
                arr[i] = (i < commander.BasicSkills.Count) ? commander.BasicSkills[i] : null;
            }
        }

        return arr;
    }


    public void ApplySelection(CharacterData commander, SkillData[] selectedSkills)
    {
        Load();
        if (commander == null) return;
        if (selectedSkills == null || selectedSkills.Length != 5) return;

        var preset = commanderPresets.Find(p => p != null && p.commanderId == commander.name);
        if (preset == null)
        {
            preset = new CommanderSkillPresetSave { commanderId = commander.name, skillIds = new string[5] };
            commanderPresets.Add(preset);
        }

        for (int i = 0; i < 6; i++)
            preset.skillIds[i] = selectedSkills[i] != null ? selectedSkills[i].skillId : "";

        Save();
    }

    // =====================================================================
    // Equipments for Game
    // =====================================================================
    public List<Equipment> GetEquippedEquipmentInstancesForGame(CharacterData commander)
    {
        Load();
        BindAllEquipmentData();

        var list = new List<Equipment>();
        for (int i = 0; i < equippedEquipment.Length; i++)
        {
            var eq = equippedEquipment[i];
            if (eq == null) continue;

            eq.BindData();
            list.Add(eq);
        }
        return list;
    }

    // =====================================================================
    // Character API
    // =====================================================================

    public enum CharacterEquipResult { Failed = 0, Success = 1, NeedPartySlotChoice = 2 }

    public List<Character> GetOwnedCharacters()
    {
        Load();
        BindAllCharacterData();
        return ownedCharacters;
    }

    public Character[] GetEquippedCharacters()
    {
        Load();
        BindAllCharacterData();
        return equippedCharacters;
    }

    public Character GetEquippedCharacterBySlot(int slotIndex)
    {
        Load();
        BindAllCharacterData();
        return equippedCharacters[slotIndex];
    }

    public void AddOwnedCharacter(Character ch)
    {
        Load();
        if (ch == null) return;

        ch.EnsureIds();
        ch.BindData(playableTable);

        ownedCharacters.Add(ch);
        Save();
    }

    public Character CreateCharacter(string dataId)
    {
        Load();

        var cd = ResolveCharacterData(dataId);

        var ch = new Character
        {
            instanceId = Guid.NewGuid().ToString("N"),
            dataId = dataId
        };

        ch.Init(cd, forcedNickname: null, startLevel: 1);

        RollRandomCompanionLoadout(ch, cd);

        AddOwnedCharacter(ch);
        return ch;
    }

    public CharacterEquipResult EquipCharacter(Character ch, int slotIndex)
    {
        Load();
        if (ch == null) return CharacterEquipResult.Failed;

        ch.EnsureIds();
        ch.BindData(playableTable);

        if (equippedCharacters == null || equippedCharacters.Length != 3)
            equippedCharacters = new Character[3];

        // 직접 슬롯 지정(교체)
        if (slotIndex >= 0 && slotIndex < 3)
        {
            equippedCharacters[slotIndex] = ch;
            return CharacterEquipResult.Success;
        }

        // 빈 슬롯 우선
        for (int i = 0; i < 3; i++)
        {
            if (equippedCharacters[i] == null || string.IsNullOrEmpty(equippedCharacters[i].instanceId))
            {
                equippedCharacters[i] = ch;
                return CharacterEquipResult.Success;
            }
        }

        return CharacterEquipResult.NeedPartySlotChoice;
    }

    public void UnequipCharacter(int slotIndex)
    {
        Load();
        if (slotIndex < 0 || slotIndex >= 3) return;

        equippedCharacters[slotIndex] = null;
        Save();
    }



    // =====================================================================
    // Internal
    // =====================================================================

    private int SlotIndexFromType(EquipmentSlotType t)
    {
        switch (t)
        {
            case EquipmentSlotType.Weapon: return 0;
            case EquipmentSlotType.Head: return 1;
            case EquipmentSlotType.Body: return 2;
            case EquipmentSlotType.Gloves: return 3;
            case EquipmentSlotType.Ring: return 4;
        }
        return -1;
    }

    private void BindAllEquipmentData()
    {
        if (equipmentTable == null)
            return;

        for (int i = 0; i < ownedEquipments.Count; i++)
        {
            var e = ownedEquipments[i];
            if (e == null)
                continue;

            if (e.Data == null && !string.IsNullOrEmpty(e.dataId))
                e.Data = equipmentTable.Resolve(e.dataId);

            if (e.Data == null)
                e.BindData();
        }
    }
    private void BindAllCharacterData()
    {
        if (playableTable == null) return;

        if (ownedCharacters != null)
        {
            for (int i = 0; i < ownedCharacters.Count; i++)
            {
                var ch = ownedCharacters[i];
                if (ch == null) continue;
                ch.BindData(playableTable);
            }
        }

        if (equippedCharacters != null)
        {
            for (int i = 0; i < equippedCharacters.Length; i++)
            {
                var ch = equippedCharacters[i];
                if (ch == null) continue;
                ch.BindData(playableTable);
            }
        }
    }

    private CharacterData ResolveCharacterData(string id)
    {
        if (playableTable == null) return null;
        if (string.IsNullOrEmpty(id)) return null;

        if (playableTable.characters != null)
        {
            for (int i = 0; i < playableTable.characters.Count; i++)
            {
                var cd = playableTable.characters[i];
                if (cd != null && cd.name == id) return cd;
            }
        }

        if (playableTable.commanders != null)
        {
            for (int i = 0; i < playableTable.commanders.Count; i++)
            {
                var cd = playableTable.commanders[i];
                if (cd != null && cd.name == id) return cd;
            }
        }

        return null;
    }
    private void EnsureDefaultCharacterSeed()
    {
        if (ownedCharacters.Count > 0) return;

        var cd = playableTable.characters.First();
        var cd2 = playableTable.characters[1];

        var ch = new Character
        {
            instanceId = Guid.NewGuid().ToString("N"),
            dataId = cd.name,
            level = 1,
            nickname = "전사",
        };
        var ch2 = new Character
        {
            instanceId = Guid.NewGuid().ToString("N"),
            dataId = cd2.name,
            level = 1,
            nickname = "궁수",
        };

        ownedCharacters.Add(ch);
        save.equippedCharacters[0] = ch;
        ownedCharacters.Add(ch2);
        save.equippedCharacters[1] = ch2;

        Save();
    }
    private static readonly EquipmentSlotType[] CompanionSlotTypes =
{
    EquipmentSlotType.Weapon,
    EquipmentSlotType.Head,
    EquipmentSlotType.Body,
    EquipmentSlotType.Gloves,
    EquipmentSlotType.Ring,
    EquipmentSlotType.Ring
};

    private bool IsCompanion(CharacterData cd)
    {
        if (cd == null) return false;
        if (cd.isBoss) return false;
        return cd.characterType != CharacterType.Commander;
    }

    // CharacterPopupUI / CharacterSpawner에서 공통으로 쓰기 위한 Resolve
    public SkillData ResolveSkillById(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        if (playableTable == null) return null;

        // 1) 공용/지휘관 스킬
        if (playableTable.skills != null)
        {
            for (int i = 0; i < playableTable.skills.Count; i++)
            {
                var s = playableTable.skills[i];
                if (s != null && s.skillId == id) return s;
            }
        }

        // 2) 동료 Active
        if (playableTable.companionActiveSkills != null)
        {
            for (int i = 0; i < playableTable.companionActiveSkills.Count; i++)
            {
                var s = playableTable.companionActiveSkills[i];
                if (s != null && s.skillId == id) return s;
            }
        }

        // 3) 동료 Passive
        if (playableTable.companionPassiveSkills != null)
        {
            for (int i = 0; i < playableTable.companionPassiveSkills.Count; i++)
            {
                var s = playableTable.companionPassiveSkills[i];
                if (s != null && s.skillId == id) return s;
            }
        }

        return null;
    }
    private void RollRandomCompanionLoadout(Character ch, CharacterData cd)
    {
        if (ch == null) return;
        if (!IsCompanion(cd)) return;
        if (playableTable == null) return;

        // ---------- Equipments (6) ----------
        if (ch.equippedEquipments == null || ch.equippedEquipments.Length != 6)
            ch.equippedEquipments = new Equipment[6];

        var eqPool = playableTable.companionEquipmentPool;

        for (int i = 0; i < 6; i++)
        {
            if (UnityEngine.Random.value < 0.5f)
            {
                ch.equippedEquipments[i] = null;
                continue;
            }

            if (eqPool == null || eqPool.Count == 0)
            {
                ch.equippedEquipments[i] = null;
                continue;
            }

            var want = CompanionSlotTypes[i];

            int candidateCount = 0;
            for (int k = 0; k < eqPool.Count; k++)
            {
                var d = eqPool[k];
                if (d == null) continue;
                if (d.slotType != want) continue;
                candidateCount++;
            }

            if (candidateCount == 0)
            {
                ch.equippedEquipments[i] = null;
                continue;
            }

            int pick = UnityEngine.Random.Range(0, candidateCount);
            EquipmentData pickedEqData = null;

            for (int k = 0; k < eqPool.Count; k++)
            {
                var d = eqPool[k];
                if (d == null) continue;
                if (d.slotType != want) continue;

                if (pick == 0) { pickedEqData = d; break; }
                pick--;
            }

            ch.equippedEquipments[i] = (pickedEqData != null) ? new Equipment(pickedEqData) : null;
        }

        // ---------- Skills (3) ----------
        if (ch.skillIds == null || ch.skillIds.Length != 3)
            ch.skillIds = new string[3];

        ch.skillIds[0] = "";
        ch.skillIds[1] = "";
        ch.skillIds[2] = "";

        // 1) Active 사용할지 먼저 판단(0/1)
        bool wantActive = UnityEngine.Random.value < 0.5f;

        // 2) 후보 수집(풀 분리 + usableClasses + skillId 유효 + useType 검증)
        var activePool = playableTable.companionActiveSkills;
        var passivePool = playableTable.companionPassiveSkills;

        var activeCandidates = new List<SkillData>();
        if (activePool != null)
        {
            for (int i = 0; i < activePool.Count; i++)
            {
                var s = activePool[i];
                if (s == null) continue;
                if (string.IsNullOrEmpty(s.skillId)) continue;
                if (!IsSkillUsableBy(s, cd)) continue;
                if (s.useType == SkillUseType.Passive) continue; // 풀 오염 방지
                activeCandidates.Add(s);
            }
        }

        var passiveCandidates = new List<SkillData>();
        if (passivePool != null)
        {
            for (int i = 0; i < passivePool.Count; i++)
            {
                var s = passivePool[i];
                if (s == null) continue;
                if (string.IsNullOrEmpty(s.skillId)) continue;
                if (!IsSkillUsableBy(s, cd)) continue;
                if (s.useType != SkillUseType.Passive) continue; // 풀 오염 방지
                passiveCandidates.Add(s);
            }
        }

        var pickedSkillIds = new HashSet<string>();
        var pickedOrdered = new List<SkillData>(3);

        // 3) Active 0~1개 선택(선택되면 항상 0번 슬롯)
        SkillData pickedActive = null;
        if (wantActive && activeCandidates.Count > 0)
        {
            pickedActive = activeCandidates[UnityEngine.Random.Range(0, activeCandidates.Count)];
            pickedSkillIds.Add(pickedActive.skillId);
            pickedOrdered.Add(pickedActive);
        }

        // 4) Passive 최대치 결정
        int passiveMax = (pickedActive != null) ? 2 : 3;

        // 5) Passive는 0~passiveMax, 각 “추가 시도”마다 50%로 비움(결과적으로 앞에서부터 채워짐)
        for (int p = 0; p < passiveMax; p++)
        {
            if (pickedOrdered.Count >= 3) break;

            if (UnityEngine.Random.value < 0.5f)
                continue;

            SkillData chosen = null;
            int attempts = Mathf.Max(10, passiveCandidates.Count * 2);

            for (int a = 0; a < attempts; a++)
            {
                if (passiveCandidates.Count == 0) break;

                var s = passiveCandidates[UnityEngine.Random.Range(0, passiveCandidates.Count)];
                if (s == null) continue;
                if (pickedSkillIds.Contains(s.skillId)) continue;

                chosen = s;
                break;
            }

            if (chosen == null) continue;

            pickedSkillIds.Add(chosen.skillId);
            pickedOrdered.Add(chosen);
        }

        // 6) 앞에서부터 채우기(Active가 있으면 0번에 이미 있음)
        for (int i = 0; i < pickedOrdered.Count && i < 3; i++)
            ch.skillIds[i] = pickedOrdered[i].skillId;
    }


    private bool IsSkillUsableBy(SkillData skill, CharacterData cd)
    {
        if (skill == null || cd == null) return false;

        var list = skill.usableClasses;

        // usableClasses 비어있으면 공용(누구나 가능)
        if (list == null || list.Count == 0) return true;

        // 엄격 적용: 레퍼런스가 동일한 캐릭터만 허용 (name fallback 제거)
        return list.Contains(cd);
    }


}
