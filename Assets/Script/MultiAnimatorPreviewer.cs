using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
#endif

[ExecuteInEditMode]
public class MultiAnimatorPreviewer : MonoBehaviour
{
    [System.Serializable]
    public class AnimatorEntry
    {
        public Animator animator;
        public AnimationClip clip;
        [HideInInspector]
        public float time;
    }

    [ListDrawerSettings(ShowPaging = false, DraggableItems = false, IsReadOnly = true)]
    [LabelText("Animator 목록")]
    public List<AnimatorEntry> entries = new();

    //[LabelText("에디터 재생")]
    //public bool playing = false;

    private float lastUpdateTime;

#if UNITY_EDITOR
    private void OnEnable()
    {
        EditorApplication.update += UpdatePreview;
        lastUpdateTime = Time.realtimeSinceStartup;
    }

    private void OnDisable()
    {
        EditorApplication.update -= UpdatePreview;
    }

    private void UpdatePreview()
    {
        //if (!playing) 
        //    return;

        float now = Time.realtimeSinceStartup;
        float delta = now - lastUpdateTime;
        lastUpdateTime = now;

        foreach (var entry in entries)
        {
            if (entry.animator == null || entry.clip == null) 
                continue;

            entry.time += delta;
            if (entry.time > entry.clip.length)
                entry.time %= entry.clip.length;

            entry.clip.SampleAnimation(entry.animator.gameObject, entry.time);
        }

        SceneView.RepaintAll();
    }

    [Button("하위 Animator 자동 등록")]
    private void FindAllAnimators()
    {
        entries.Clear();

        var animators = GetComponentsInChildren<Animator>(true);
        foreach (var animator in animators)
        {
            if (animator.runtimeAnimatorController is AnimatorController controller &&
                controller.animationClips.Length > 0)
            {
                entries.Add(new AnimatorEntry
                {
                    animator = animator,
                    clip = controller.animationClips[0],
                    time = 0f
                });
            }
        }

        EditorUtility.SetDirty(this);  // 인스펙터 갱신
    }
#endif
}
