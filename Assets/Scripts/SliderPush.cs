using UnityEngine;

[DefaultExecutionOrder(200)]
public sealed class SliderPush : MonoBehaviour
{
    [SerializeField] private SpriteRenderer track;
    [SerializeField] private BoxCollider handleCollider;
    [SerializeField] private InchwormLocomotion player;

    private SphereCollider[] playerSegments;
    private Vector3[] previousPositions;
    private Transform handle;
    private bool interactionEnabled;

    public void SetInteractionEnabled(bool value)
    {
        interactionEnabled = value;
        SyncPlayerPositions();
    }

    public float NormalizedDownPosition
    {
        get
        {
            if (!TryGetTravelRange(out Vector3 axis, out float lowerLimit, out float upperLimit))
                return 0f;

            float handlePosition = Vector3.Dot(HandleCenter(), axis);
            return Mathf.Clamp01((upperLimit - handlePosition) /
                (upperLimit - lowerLimit));
        }
    }

    private void Start()
    {
        if (player == null)
            player = FindFirstObjectByType<InchwormLocomotion>();

        if (track == null || handleCollider == null || player == null)
        {
            Debug.LogError("SliderPush needs a track, handle collider, and player.", this);
            enabled = false;
            return;
        }

        playerSegments = player.GetComponentsInChildren<SphereCollider>();
        if (playerSegments.Length == 0)
        {
            Debug.LogError("SliderPush needs SphereColliders on the player.", this);
            enabled = false;
            return;
        }

        handle = handleCollider.transform;
        previousPositions = new Vector3[playerSegments.Length];
        for (int i = 0; i < playerSegments.Length; i++)
            previousPositions[i] = SegmentCenter(playerSegments[i]);
    }

    private void LateUpdate()
    {
        if (!interactionEnabled)
        {
            SyncPlayerPositions();
            return;
        }

        if (!handleCollider.enabled || !handleCollider.gameObject.activeInHierarchy)
            return;

        if (!TryGetTravelRange(out Vector3 axis, out float lowerLimit, out float upperLimit))
            return;

        Vector3 handleCenter = HandleCenter();
        float handlePosition = Vector3.Dot(handleCenter, axis);
        float handleHalfLength = ColliderHalfExtentAlong(axis);
        Vector3 widthAxis = Vector3.ProjectOnPlane(track.transform.TransformVector(Vector3.right), axis).normalized;
        float handleHalfWidth = ColliderHalfExtentAlong(widthAxis);
        float downTarget = handlePosition;
        float upTarget = handlePosition;

        for (int i = 0; i < playerSegments.Length; i++)
        {
            SphereCollider segment = playerSegments[i];
            Vector3 previous = previousPositions[i];
            Vector3 current = SegmentCenter(segment);
            previousPositions[i] = current;

            if (!segment.enabled || !segment.gameObject.activeInHierarchy)
                continue;

            float radius = segment.radius * Mathf.Max(
                Mathf.Abs(segment.transform.lossyScale.x),
                Mathf.Abs(segment.transform.lossyScale.y),
                Mathf.Abs(segment.transform.lossyScale.z));
            if (Mathf.Abs(Vector3.Dot(current - handleCenter, widthAxis)) > handleHalfWidth + radius)
                continue;

            float previousPosition = Vector3.Dot(previous, axis);
            float currentPosition = Vector3.Dot(current, axis);
            float movement = currentPosition - previousPosition;
            if (movement < -0.0001f && previousPosition >= handlePosition &&
                currentPosition - radius < handlePosition + handleHalfLength)
            {
                downTarget = Mathf.Min(downTarget,
                    currentPosition - radius - handleHalfLength);
            }
            else if (movement > 0.0001f && previousPosition <= handlePosition &&
                     currentPosition + radius > handlePosition - handleHalfLength)
            {
                upTarget = Mathf.Max(upTarget,
                    currentPosition + radius + handleHalfLength);
            }
        }

        float target = handlePosition - downTarget > upTarget - handlePosition ? downTarget : upTarget;
        target = Mathf.Clamp(target, lowerLimit, upperLimit);
        handle.position += axis * (target - handlePosition);
    }

    private void SyncPlayerPositions()
    {
        if (playerSegments == null || previousPositions == null)
            return;

        for (int i = 0; i < playerSegments.Length; i++)
            previousPositions[i] = SegmentCenter(playerSegments[i]);
    }

    private bool TryGetTravelRange(out Vector3 axis, out float lowerLimit, out float upperLimit)
    {
        axis = Vector3.zero;
        lowerLimit = upperLimit = 0f;
        if (track == null || handleCollider == null)
            return false;

        // The track's local Y axis follows its long edge, regardless of how the
        // whole slider is rotated or scaled in the scene.
        Bounds localBounds = track.localBounds;
        Vector3 top = track.transform.TransformPoint(localBounds.center + Vector3.up * localBounds.extents.y);
        Vector3 bottom = track.transform.TransformPoint(localBounds.center - Vector3.up * localBounds.extents.y);
        Vector3 travel = top - bottom;
        if (travel.sqrMagnitude < 0.000001f)
            return false;

        axis = travel.normalized;
        float handleHalfLength = ColliderHalfExtentAlong(axis);
        lowerLimit = Vector3.Dot(bottom, axis) + handleHalfLength;
        upperLimit = Vector3.Dot(top, axis) - handleHalfLength;
        return upperLimit - lowerLimit > 0.0001f;
    }

    private Vector3 HandleCenter()
    {
        return handleCollider.transform.TransformPoint(handleCollider.center);
    }

    private float ColliderHalfExtentAlong(Vector3 direction)
    {
        Vector3 halfSize = handleCollider.size * 0.5f;
        Transform colliderTransform = handleCollider.transform;
        return Mathf.Abs(Vector3.Dot(direction, colliderTransform.TransformVector(new Vector3(halfSize.x, 0f, 0f))))
            + Mathf.Abs(Vector3.Dot(direction, colliderTransform.TransformVector(new Vector3(0f, halfSize.y, 0f))))
            + Mathf.Abs(Vector3.Dot(direction, colliderTransform.TransformVector(new Vector3(0f, 0f, halfSize.z))));
    }

    private static Vector3 SegmentCenter(SphereCollider segment)
    {
        return segment.transform.TransformPoint(segment.center);
    }
}
