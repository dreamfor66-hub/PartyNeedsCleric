using UnityEditor;
using UnityEngine;
using Sirenix.OdinInspector.Editor;

public class SpriteSetDrawer : OdinValueDrawer<SpriteSet>
{
    protected override void DrawPropertyLayout(GUIContent label)
    {
        var value = this.ValueEntry.SmartValue;

        float viewWidth = EditorGUIUtility.currentViewWidth - 40f;
        float cellWidth = 90f;       // 1Ä­ ³Êºñ
        float padding = 10f;

        int columns = Mathf.Max(1, Mathf.FloorToInt((viewWidth + padding) / (cellWidth + padding)));

        GUILayout.BeginHorizontal();
        DrawCell("Idle Front", ref value.anim_idleFront, cellWidth);
        DrawCell("Idle Back", ref value.anim_idleBack, cellWidth);
        DrawCell("Atk Front", ref value.anim_attackFront, cellWidth);
        DrawCell("Atk Back", ref value.anim_attackBack, cellWidth);
        DrawCell("Die", ref value.anim_die, cellWidth);
        GUILayout.EndHorizontal();

        this.ValueEntry.SmartValue = value;
    }

    private void DrawCell(string label, ref Sprite sprite, float width)
    {
        GUILayout.BeginVertical(GUILayout.Width(width));

        GUILayout.Label(label, GUILayout.Width(width));
        sprite = (Sprite)EditorGUILayout.ObjectField(
            sprite,
            typeof(Sprite),
            false,
            GUILayout.Width(width),
            GUILayout.Height(width)
        );

        GUILayout.EndVertical();
    }
}
