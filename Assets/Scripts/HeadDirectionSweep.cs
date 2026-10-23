using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(100)]
public sealed class HeadDirectionSweep : MonoBehaviour
{
    [Header("Drag From Center")]
    [SerializeField] private Transform rabbitCenter;
    [SerializeField, Min(0f)] private float maxDistanceFromCenter = 2.1f;

    private Transform head;
    private Camera viewCamera;
    private float radius;
    private float height;
    
    public float CurrentAngle { get; private set; } = 90f;
    public Vector3 CurrentDirection => transform.forward;

    private void OnEnable()
    {
        head = transform.parent;
        Vector3 offset = transform.localPosition;
        radius = new Vector2(offset.x, offset.z).magnitude;
        height = offset.y - (rabbitCenter != null ? head.InverseTransformPoint(rabbitCenter.position).y : 0f);
        SetAngle(0f, rabbitCenter != null ? maxDistanceFromCenter : radius);
    }

    private void LateUpdate()
    {
        if (head == null || Mouse.current == null)
            return;

        if (viewCamera == null)
            viewCamera = Camera.main;
        if (viewCamera == null)
            return;

        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Ray ray = viewCamera.ScreenPointToRay(mousePosition);
        Vector3 centerPosition = rabbitCenter != null ? rabbitCenter.position : head.position;
        Plane headPlane = new Plane(Vector3.up, centerPosition);
        if (!headPlane.Raycast(ray, out float distance))
            return;

        Vector3 localCenter = head.InverseTransformPoint(centerPosition);
        Vector3 localDirection = head.InverseTransformPoint(ray.GetPoint(distance)) - localCenter;
        localDirection.y = 0f;
        if (localDirection.sqrMagnitude < 0.000001f)
        {
            if (rabbitCenter != null)
                transform.localPosition = localCenter + Vector3.up * height;
            return;
        }

        float mouseAngle = Mathf.Atan2(localDirection.x, localDirection.z) * Mathf.Rad2Deg;
        float distanceFromCenter = rabbitCenter != null
            ? Mathf.Min(localDirection.magnitude, maxDistanceFromCenter)
            : radius;
        SetAngle(Mathf.Clamp(mouseAngle, -90f, 90f), distanceFromCenter);
    }

    private void SetAngle(float angle, float distanceFromCenter)
    {
        CurrentAngle = angle + 90f;
        float radians = angle * Mathf.Deg2Rad;
        Vector3 localCenter = rabbitCenter != null
            ? head.InverseTransformPoint(rabbitCenter.position)
            : Vector3.zero;

        transform.localPosition = localCenter + new Vector3(
            Mathf.Sin(radians) * distanceFromCenter,
            height,
            Mathf.Cos(radians) * distanceFromCenter);
        transform.localRotation = Quaternion.Euler(0f, angle, 0f);
    }
}
