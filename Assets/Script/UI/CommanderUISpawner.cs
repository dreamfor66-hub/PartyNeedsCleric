using UnityEngine;

public class CommanderUISpawner : MonoBehaviour
{
    public static CommanderUISpawner Instance;

    public CommanderUI commanderUIPrefab;
    public Transform uiRoot;

    void Awake()
    {
        Instance = this;
    }

    public CommanderUI Spawn(PlayerCommander commander)
    {
        for (int i = uiRoot.childCount - 1; i >= 0; i--)
            Destroy(uiRoot.GetChild(i).gameObject);

        var ui = Instantiate(commanderUIPrefab, uiRoot);
        ui.Init(commander.data, commander);
        return ui;
    }
}
