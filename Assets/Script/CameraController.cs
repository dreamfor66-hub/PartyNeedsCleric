using System.Linq;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Camera")]
    public Camera cam;

    [Header("Border")]
    public float borderLeft = 2f;
    public float borderRight = 2f;
    public float borderTop = 2f;
    public float borderBottom = 2f;

    [Header("Smooth")]
    public float posLerp = 5f;
    public float sizeLerp = 5f;

    [Header("Min Size")]
    public float minSize = 10f;   // 최소 사이즈

    void LateUpdate()
    {
        var list = EntityContainer.Instance.Characters.Where(x => x.state != CharacterState.Die && x.data.targettable).ToList();
        if (list.Count == 0) return;

        Vector3 min = new Vector3(99999, 99999, 0);
        Vector3 max = new Vector3(-99999, -99999, 0);

        for (int i = 0; i < list.Count; i++)
        {
            var p = list[i].transform.position;

            if (p.x < min.x) min.x = p.x;
            if (p.y < min.y) min.y = p.y;

            if (p.x > max.x) max.x = p.x;
            if (p.y > max.y) max.y = p.y;
        }

        min.x -= borderLeft;
        max.x += borderRight;
        min.y -= borderBottom;
        max.y += borderTop;

        Vector3 center = (min + max) * 0.5f;
        center.z = transform.position.z;

        float width = max.x - min.x;
        float height = max.y - min.y;

        float targetSize = Mathf.Max(
            height * 0.5f,
            (width * 0.5f) / cam.aspect
        );

        // 최소 사이즈 적용
        targetSize = Mathf.Max(targetSize, minSize);

        cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetSize, Time.deltaTime * sizeLerp);
        transform.position = Vector3.Lerp(transform.position, center, Time.deltaTime * posLerp);
    }
}
