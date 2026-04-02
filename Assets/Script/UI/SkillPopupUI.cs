using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class SkillPopupUI : MonoBehaviour
{
    public Image image;
    public TextMeshProUGUI title;
    public TextMeshProUGUI desc;
    public TextMeshProUGUI cost;
    public TextMeshProUGUI cd;
    public void Init(SkillData s)
    {
        if (s == null)
            return;

        image.sprite = s.icon;
        title.text = s.skillName;
        desc.text = s.description;
        cost.text = $"Mana {s.manaCost}";
        cd.text = $"CD {s.cooldown}";
    }
}
