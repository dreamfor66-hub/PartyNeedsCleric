using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BuffIconUI
{
    public GameObject root;
    public Image icon;
    public TextMeshProUGUI stack;
    public Image duration;
    public GameObject popup;
    public TextMeshProUGUI popupName;
    public TextMeshProUGUI popupDesc;

    public BuffIconUI(GameObject go, BuffIconUIRefs r)
    {
        root = go;
        icon = r.icon;
        stack = r.stackText;
        duration = r.durationFill;

        popup = r.popup;
        popupName = r.popupName;
        popupDesc = r.popupDesc;

        popup.SetActive(false);
    }
}
