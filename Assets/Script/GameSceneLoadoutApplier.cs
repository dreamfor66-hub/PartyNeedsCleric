using System.Collections;
using UnityEngine;

[DefaultExecutionOrder(-10000)]
public class GameSceneLoadoutApplier : MonoBehaviour
{
    private void Awake()
    {
        var acc = OutgameAccountManager.Instance;
        if (acc == null) return;

        var spawner = CharacterSpawner.Instance != null ? CharacterSpawner.Instance : FindFirstObjectByType<CharacterSpawner>();
        if (spawner == null) return;

        if (acc.CurrentCommander != null)
            spawner.commanderData = acc.CurrentCommander;
    }

    private void Start()
    {
        StartCoroutine(ApplyLoadoutAfterSpawn());
    }

    private IEnumerator ApplyLoadoutAfterSpawn()
    {
        var acc = OutgameAccountManager.Instance;
        if (acc == null) yield break;

        while (EntityContainer.Instance == null || EntityContainer.Instance.Commander == null)
            yield return null;

        var commander = EntityContainer.Instance.Commander;

        // equipment -> character
        var equips = acc.GetEquippedEquipmentInstancesForGame(acc.CurrentCommander);
        commander.SetEquipmentLoadout(equips);

        // skills (equipment replace Æ÷ÇÔ)
        var skills = acc.GetCurrentSkillsForGame();
        for (int i = 0; i < 6; i++)
            commander.skills[i] = skills != null ? skills[i] : null;

        commander.GetSkillObserver().SetSkills(commander.skills);
        commander.RestoreMana(0);

    }

}
