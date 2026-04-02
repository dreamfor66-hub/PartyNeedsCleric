using Sirenix.OdinInspector;
using UnityEngine;

public enum VfxEndBy { Manual, Time }
public enum VfxFollowType { None, FollowT, FollowTR }
public enum SpawnFollowType { None, FollowT, FollowTR }

public class VfxObject : MonoBehaviour
{
    [OnValueChanged(nameof(OnEndByChanged))]
    public VfxEndBy endBy = VfxEndBy.Manual;

    [ShowIf(nameof(UseDuration))]
    public float duration = 1f;

    [Header("Spawn / Runtime Follow")]
    public SpawnFollowType spawnFollowType = SpawnFollowType.None;
    public VfxFollowType followType = VfxFollowType.None;

    private Transform followTarget;
    private Vector3 initOffset;
    private float timer = 0f;

    private BuffInstance linkedBuff;

    private Quaternion initLocalRotation;

    private bool useBasisOffset = false;
    private Vector3 basisRight;
    private Vector3 basisForward;
    private Vector2 basisOffset2;

    bool paused = false;
    ParticleSystem[] particleSystems;
    Animator[] animators;

    private bool UseDuration() => endBy == VfxEndBy.Time;

    private void OnEndByChanged()
    {
        if (endBy == VfxEndBy.Manual)
            duration = 0f;
        else if (duration <= 0f)
            duration = 1f;
    }

    void Awake()
    {
        particleSystems = GetComponentsInChildren<ParticleSystem>(true);
        animators = GetComponentsInChildren<Animator>(true);
    }

    public void Init(Transform target, VfxFollowType followType, float duration, BuffInstance linkedBuff = null)
    {
        this.followTarget = target;
        this.followType = followType;
        this.duration = duration;
        this.linkedBuff = linkedBuff;

        useBasisOffset = false;

        if (target != null)
        {
            initOffset = transform.position - target.position;
            initLocalRotation = Quaternion.Inverse(target.rotation) * transform.rotation;
        }

        ApplySpawnFollow();

        timer = 0f;
        paused = false;
    }

    public void Init(Transform target, VfxFollowType followType, float duration, Vector2 fixedForward, Vector2 offset2, BuffInstance linkedBuff = null)
    {
        this.followTarget = target;
        this.followType = followType;
        this.duration = duration;
        this.linkedBuff = linkedBuff;

        Vector2 f2 = fixedForward.sqrMagnitude > 0.0001f ? fixedForward.normalized : Vector2.right;
        Vector2 r2 = new Vector2(-f2.y, f2.x);

        useBasisOffset = true;
        basisForward = new Vector3(f2.x, f2.y, 0f);
        basisRight = new Vector3(r2.x, r2.y, 0f);
        basisOffset2 = offset2;

        initOffset = Vector3.zero;
        initLocalRotation = Quaternion.identity;

        ApplySpawnFollow();

        timer = 0f;
        paused = false;
    }
    public void SetPaused(bool pause)
    {
        if (paused == pause)
            return;

        paused = pause;

        if (animators != null)
        {
            foreach (var anim in animators)
            {
                if (anim == null) continue;
                anim.enabled = !pause;
            }
        }

        if (particleSystems != null)
        {
            foreach (var ps in particleSystems)
            {
                if (ps == null) continue;

                if (pause)
                    ps.Pause(true);
                else
                    ps.Play(true);
            }
        }
    }

    void Update()
    {
        if (paused)
            return;

        ApplyRuntimeFollow();

        if (endBy == VfxEndBy.Time)
        {
            timer += Time.deltaTime;
            if (timer >= duration)
                Release();
        }
        else if (endBy == VfxEndBy.Manual)
        {
            if (linkedBuff == null) return;
            if (!linkedBuff.Owner.Buffs.Contains(linkedBuff))
                Release();
        }
    }

    private void ApplySpawnFollow()
    {
        if (followTarget == null)
            return;

        Vector3 posOffset = GetFollowOffset();

        switch (spawnFollowType)
        {
            case SpawnFollowType.None:
                break;

            case SpawnFollowType.FollowT:
                transform.position = followTarget.position + posOffset;
                break;

            case SpawnFollowType.FollowTR:
                transform.position = followTarget.position + posOffset;
                transform.rotation = followTarget.rotation * initLocalRotation;
                break;
        }
    }
    private void ApplyRuntimeFollow()
    {
        if (followTarget == null)
            return;

        Vector3 posOffset = GetFollowOffset();

        switch (followType)
        {
            case VfxFollowType.None:
                break;

            case VfxFollowType.FollowT:
                transform.position = followTarget.position + posOffset;
                break;

            case VfxFollowType.FollowTR:
                transform.position = followTarget.position + posOffset;
                transform.rotation = followTarget.rotation * initLocalRotation;
                break;
        }
    }

    private Vector3 GetFollowOffset()
    {
        if (useBasisOffset)
            return basisRight * basisOffset2.x + basisForward * basisOffset2.y;

        return initOffset;
    }

    public void Release()
    {
        Destroy(gameObject);
    }
}