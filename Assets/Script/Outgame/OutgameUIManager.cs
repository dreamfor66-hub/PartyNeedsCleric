using UnityEngine;
using UnityEngine.UI;

public class OutgameUIManager : MonoBehaviour
{
    public static OutgameUIManager Instance { get; private set; }

    [Header("Data")]
    public PlayableDataTable playableTable;
    public EquipmentDataTable equipmentTable;

    [Header("Pages")]
    public LobbyPage LobbyPage;
    public EquipmentPage EquipmentPage;

    [Header("Page Buttons")]
    public Button lobbyPageButton;
    public Button equipmentPageButton;

    [Header("Toast")]
    public ToastMessageUI toast;

    [Header("Dev")]
    public Button devButton;
    public OutgameDevPopup devPopup;

    private void Awake()
    {
        Instance = this;

        if (OutgameAccountManager.Instance != null)
            OutgameAccountManager.Instance.Init(playableTable, equipmentTable);

        if (LobbyPage != null)
            LobbyPage.Init(playableTable);

        if (lobbyPageButton != null)
        {
            lobbyPageButton.onClick.RemoveAllListeners();
            lobbyPageButton.onClick.AddListener(() => ShowLobby());
        }

        if (equipmentPageButton != null)
        {
            equipmentPageButton.onClick.RemoveAllListeners();
            equipmentPageButton.onClick.AddListener(() => ShowEquipment());
        }

        if (devButton != null)
        {
            devButton.onClick.RemoveAllListeners();
            devButton.onClick.AddListener(() =>
            {
                if (devPopup != null)
                    devPopup.Open(equipmentTable);
            });
        }

        ShowLobby();
    }

    public void ShowLobby()
    {
        if (EquipmentPage != null) EquipmentPage.ClosePage();
        if (LobbyPage != null) LobbyPage.gameObject.SetActive(true);
    }

    public void ShowEquipment()
    {
        if (LobbyPage != null) LobbyPage.gameObject.SetActive(false);
        if (EquipmentPage != null) EquipmentPage.OpenPage();
    }

    public static void ShowToast(string msg)
    {
        if (Instance == null || Instance.toast == null) return;
        Instance.toast.Show(msg);
    }
}
