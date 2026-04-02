using System.Linq;
using System.Runtime.ConstrainedExecution;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.UI.GridLayoutGroup;

public class CommanderUI : MonoBehaviour
{
    [Header("Name / Portrait")]
    public TextMeshProUGUI nameText;
    public Image portrait;

    [Header("Mana")]
    public Image manaFill;
    public TextMeshProUGUI manaText;

    [Header("Casting")]
    public Transform castingBar;
    public Image castingFill;

    [Header("BuffIcons")]
    public BuffIconManager buffIcons;
    [SerializeField] Transform buffGridRoot;
    [SerializeField] GameObject buffIconPrefab;

    CharacterBehaviour owner;

    public void Init(CharacterData data, PlayerCommander commander)
    {
        this.owner = commander;

        nameText.text = data.name.Split('_').Last();
        portrait.sprite = data.portrait;

        UpdateMana(owner.GetCurrentMana(), owner.GetMaxMana());
        buffIcons = new BuffIconManager();
        buffIcons.Initialize(buffGridRoot, buffIconPrefab, owner, true);

    }

    public void UpdateMana(int current, int max)
    {
        float ratio = (float)current / max;
        manaFill.fillAmount = ratio;
        manaText.text = $"{current} / {max}";
    }

    void Update()
    {
        if (owner == null) return;

        buffIcons?.Update();
    }

    public void ShowCastingBar()
    {
        castingBar.gameObject.SetActive(true);
        castingFill.fillAmount = 0f;
    }

    public void UpdateCastingBar(float t)
    {
        castingFill.fillAmount = t;
    }

    public void HideCastingBar()
    {
        castingBar.gameObject.SetActive(false);
    }
}
