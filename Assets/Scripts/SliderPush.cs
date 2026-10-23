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
        if (!handleCollider.enabled || !handleCollider.gameObject.activeInHierarchy)
            return;

        Bounds trackBounds = track.bounds;
        Bounds handleBounds = handleCollider.bounds;
        float lowerLimit = trackBounds.min.z + handleBounds.extents.z;
        float upperLimit = trackBounds.max.z - handleBounds.extents.z;
        if (lowerLimit > upperLimit)
            return;

        float handleZ = handle.position.z;
        float downTarget = handleZ;
        float upTarget = handleZ;

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
            if (current.x + radius < handleBounds.min.x ||
                current.x - radius > handleBounds.max.x)
                continue;

            float movement = current.z - previous.z;
            if (movement < -0.0001f && previous.z >= handleZ &&
                current.z - radius < handleBounds.max.z)
            {
                downTarget = Mathf.Min(downTarget,
                    current.z - radius - handleBounds.extents.z);
            }
            else if (movement > 0.0001f && previous.z <= handleZ &&
                     current.z + radius > handleBounds.min.z)
            {
                upTarget = Mathf.Max(upTarget,
                    current.z + radius + handleBounds.extents.z);
            }
        }

        float target = handleZ - downTarget > upTarget - handleZ ? downTarget : upTarget;
        target = Mathf.Clamp(target, lowerLimit, upperLimit);
        handle.position = new Vector3(handle.position.x, handle.position.y, target);
    }

    private static Vector3 SegmentCenter(SphereCollider segment)
    {
        return segment.transform.TransformPoint(segment.center);
    }
}
