using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LobbyPage : MonoBehaviour
{
    [Header("Data")]
    public PlayableDataTable playableTable;

    [Header("Buttons")]
    public Button startGameButton;

    private bool built;

    public void Init(PlayableDataTable table)
    {
        playableTable = table;

        if (!built)
        {
            if (startGameButton != null) startGameButton.onClick.AddListener(OnClick_StartGame);
            built = true;
        }
    }

    private void OnClick_StartGame()
    {
        SceneManager.LoadScene("GameScene");
    }

}
