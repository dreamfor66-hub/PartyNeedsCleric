using UnityEngine;
using System.Collections.Generic;

[DefaultExecutionOrder(-1000)]
public class CharacterSpawner : MonoBehaviour
{
    public static CharacterSpawner Instance { get; private set; }
    void Awake()
    {
        Instance = this;
    }

    [Header("프리팹")]
    public CharacterBehaviour characterPrefab;
    public PlayerCommander commanderPrefab;

    [Header("생성할 캐릭터 목록")]
    public CharacterData commanderData;
    public CharacterData[] playerTeam;
    public CharacterData[] enemyWave;

    [Header("Outgame 연동")]
    public bool useOutgameEquippedParty = true;

    void Start()
    {
        // ----- Player Team -----
        if (useOutgameEquippedParty && OutgameAccountManager.Instance != null)
        {
            var acc = OutgameAccountManager.Instance;
            var party = acc.GetEquippedCharacters(); // 3칸

            int spawned = 0;

            for (int i = 0; i < party.Length; i++)
            {
                var ch = party[i];
                if (ch == null || ch.Data == null) continue;

                var c = Instantiate(characterPrefab, new Vector3(-150 + spawned * 50f, -100, 0), Quaternion.identity);
                c.Init(ch.Data, TeamType.Player);

                // 장비 적용 (스킬 쿨다운/태그 가속 반영을 위해 먼저 적용)
                var eqList = new List<Equipment>();
                if (ch.equippedEquipments != null)
                {
                    for (int e = 0; e < ch.equippedEquipments.Length; e++)
                    {
                        var eq = ch.equippedEquipments[e];
                        if (eq == null) continue;
                        eq.BindData();
                        eqList.Add(eq);
                    }
                }
                c.SetEquipmentLoadout(eqList);

                // 스킬 적용(3개만 0~2에 넣고 나머지 null)
                var arr = new SkillData[CharacterSkillObserver.MaxSkills];
                if (ch.skillIds != null)
                {
                    for (int s = 0; s < 3 && s < ch.skillIds.Length; s++)
                        arr[s] = acc.ResolveSkillById(ch.skillIds[s]);
                }
                c.GetSkillObserver().SetSkills(arr);
                c.GetSkillObserver().ApplyPassiveOnce();

                spawned++;
            }

            // party가 비어있으면 기존 inspector 세팅으로 fallback
            if (spawned == 0)
                SpawnPlayerTeamFromInspector();
        }
        else
        {
            SpawnPlayerTeamFromInspector();
        }

        // ----- Enemy -----
        for (int i = 0; i < enemyWave.Length; i++)
        {
            var c = Instantiate(characterPrefab, new Vector3(-50 + i * 50f, 100, 0), Quaternion.identity);
            c.Init(enemyWave[i], TeamType.Enemy);
        }

        // ----- Commander -----
        var commander = Instantiate(commanderPrefab, Vector3.zero, Quaternion.identity);
        commander.Init(commanderData, TeamType.Player);
    }

    private void SpawnPlayerTeamFromInspector()
    {
        for (int i = 0; i < playerTeam.Length; i++)
        {
            var c = Instantiate(characterPrefab, new Vector3(-150 + i * 50f, -100, 0), Quaternion.identity);
            c.Init(playerTeam[i], TeamType.Player);
        }
    }

}
