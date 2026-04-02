using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BuffIconUIRefs : MonoBehaviour
{
    [Header("버프표기")]
    public Image icon;
    public TextMeshProUGUI stackText;
    public Image durationFill;
    public Button button;

    [Header("누르면 나오는 팝업")]
    public GameObject popup;
    public TextMeshProUGUI popupName;
    public TextMeshProUGUI popupDesc;
}