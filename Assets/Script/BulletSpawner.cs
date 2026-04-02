using UnityEngine;

public class BulletSpawner : MonoBehaviour
{
    public static BulletSpawner Instance { get; private set; }

    public BulletBehaviour bulletPrefab;

    void Awake()
    {
        Instance = this;
    }
}
