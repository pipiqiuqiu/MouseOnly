using UnityEngine;
using UnityEngine.InputSystem;

public sealed class InchwormLocomotion : MonoBehaviour
{
    [Header("Segments")]
    [SerializeField] private Transform head;
    [SerializeField] private Transform middleFront;
    [SerializeField] private Transform middleBack;
    [SerializeField] private Transform tail;
    [SerializeField] private HeadDirectionSweep directionIndicator;

    [Header("Board")]
    [SerializeField] private Transform gridBoard;

    [Header("Floor Detection")]
    [SerializeField, Min(0.1f)] private float floorProbeDistance = 4f;

    [Header("Motion")]
    [SerializeField, Min(0.05f)] private float gatherDuration = 0.6f;
    [SerializeField, Min(0.05f)] private float stretchDuration = 0.8f;
    [SerializeField, Range(0.1f, 0.95f)] private float compressedLengthFraction = 0.7f;
    [SerializeField, Min(0f)] private float archHeight = 0.35f;

    [Header("Mouse Stop")]
    [SerializeField, Min(0f)] private float stopDistancePixels = 30f;

    private enum Phase { Gather, Stretch }

    private Phase phase;
    private float phaseTime;
    private bool waitingAtUnitBoundary;
    private bool hasCompletedStretch;
    private float restLength;
    private float frontFraction;
    private float backFraction;
    private Vector3 headStart;
    private Vector3 headTarget;
    private Vector3 tailStart;
    private Vector3 tailTarget;
    private Quaternion headRotationStart;
    private Quaternion headRotationTarget;
    private SphereCollider headCollider;
    private SphereCollider tailCollider;
    private readonly RaycastHit[] floorHits = new RaycastHit[32];
    private BoxCollider[] floorColliders;
    private FloorBreak[] floorBreaks;
    private FloorBreak currentFloor;

    private void OnEnable()
    {
        //Cursor.visible = false;
        
        if (head == null || middleFront == null || middleBack == null || tail == null ||
            directionIndicator == null || head.parent != transform)
        {
            Debug.LogError("InchwormLocomotion needs the segments and direction indicator.", this);
            enabled = false;
            return;
        }

        headCollider = head.GetComponent<SphereCollider>();
        tailCollider = tail.GetComponent<SphereCollider>();
        if (headCollider == null || tailCollider == null)
        {
            Debug.LogError("The worm's head and tail need SphereColliders.", this);
            enabled = false;
            return;
        }

        if (gridBoard != null && !CacheFloors())
        {
            Debug.LogWarning("Grid floor break effects need BoxColliders and FloorBreak components.", this);
            gridBoard = null;
        }

        Vector3 initialHead = head.position;
        Vector3 initialFront = middleFront.position;
        Vector3 initialBack = middleBack.position;
        Vector3 initialTail = tail.position;

        transform.position = initialHead;
        head.position = initialHead;
        middleFront.position = initialFront;
        middleBack.position = initialBack;
        tail.position = initialTail;

        Vector3 body = initialTail - initialHead;
        body.y = 0f;
        restLength = body.magnitude;
        if (restLength < 0.001f)
        {
            Debug.LogError("The inchworm's head and tail must start apart.", this);
            enabled = false;
            return;
        }

        float lengthSquared = restLength * restLength;
        frontFraction = Mathf.Clamp01(Vector3.Dot(initialFront - initialHead, body) / lengthSquared);
        backFraction = Mathf.Clamp01(Vector3.Dot(initialBack - initialHead, body) / lengthSquared);
        currentFloor = null;
        if (gridBoard != null)
        {
            currentFloor = FindFloorAt(head.position);
            if (currentFloor != null)
                currentFloor.PlayBreak();
        }

        waitingAtUnitBoundary = false;
        hasCompletedStretch = false;
        BeginGather();
    }

    private void LateUpdate()
    {
        // A floor can be replaced or disabled while the game is running.
        if (!IsSupportedByTaggedFloor(head.position, headCollider) ||
            !IsSupportedByTaggedFloor(tail.position, tailCollider))
            return;

        if (waitingAtUnitBoundary)
        {
            if (IsMouseNearDirectionIndicator())
                return;

            waitingAtUnitBoundary = false;
            BeginStretch();
        }

        float duration = Mathf.Max(0.05f, phase == Phase.Gather ? gatherDuration : stretchDuration);
        float nextPhaseTime = Mathf.Min(phaseTime + Time.deltaTime, duration);
        float t = nextPhaseTime / duration;
        float smoothT = t * t * (3f - 2f * t);

        if (phase == Phase.Gather &&
            !IsSupportedByTaggedFloor(Vector3.Lerp(tailStart, tailTarget, smoothT), tailCollider))
            return;
        if (phase == Phase.Stretch &&
            !IsSupportedByTaggedFloor(Vector3.Lerp(headStart, headTarget, smoothT), headCollider))
        {
            BeginStretch();
            return;
        }

        phaseTime = nextPhaseTime;

        if (phase == Phase.Gather)
        {
            transform.position = headStart;
            tail.position = Vector3.Lerp(tailStart, tailTarget, smoothT);
            PositionMiddleSegments(smoothT);
        }
        else
        {
            transform.position = Vector3.Lerp(headStart, headTarget, smoothT);
            tail.position = tailStart;
            head.rotation = Quaternion.Slerp(headRotationStart, headRotationTarget, smoothT);
            PositionMiddleSegments(1f - smoothT);
            if (gridBoard != null)
                UpdateCurrentFloor();
        }

        if (phaseTime >= duration)
        {
            // A movement unit ends with the gather after a stretch.
            if (phase == Phase.Gather)
            {
                if (hasCompletedStretch && IsMouseNearDirectionIndicator())
                    waitingAtUnitBoundary = true;
                else
                    BeginStretch();
            }
            else
            {
                hasCompletedStretch = true;
                BeginGather();
            }
        }
    }

    private bool IsMouseNearDirectionIndicator()
    {
        if (stopDistancePixels <= 0f || Mouse.current == null)
            return false;

        Camera viewCamera = Camera.main;
        if (viewCamera == null)
            return false;

        Vector3 indicatorScreenPosition = viewCamera.WorldToScreenPoint(directionIndicator.transform.position);
        if (indicatorScreenPosition.z <= 0f)
            return false;

        Vector2 distance = Mouse.current.position.ReadValue() -
            new Vector2(indicatorScreenPosition.x, indicatorScreenPosition.y);
        return distance.sqrMagnitude < stopDistancePixels * stopDistancePixels;
    }

    private void BeginGather()
    {
        phase = Phase.Gather;
        phaseTime = 0f;
        headStart = head.position;
        tailStart = tail.position;

        Vector3 towardHead = headStart - tailStart;
        towardHead.y = 0f;
        float currentLength = towardHead.magnitude;
        float travel = Mathf.Max(0f, currentLength - restLength * compressedLengthFraction);
        Vector3 direction = towardHead.normalized;
        travel = MaxTravelOnTaggedFloors(tailStart, direction, travel, tailCollider);
        tailTarget = tailStart + direction * travel;
    }

    private void BeginStretch()
    {
        phase = Phase.Stretch;
        phaseTime = 0f;
        headStart = head.position;
        tailStart = tail.position;

        Vector3 heading = directionIndicator.CurrentDirection;
        heading.y = 0f;
        if (heading.sqrMagnitude < 0.000001f)
            heading = headStart - tailStart;
        heading.Normalize();

        Vector3 fromTail = headStart - tailStart;
        fromTail.y = 0f;
        float alongHeading = Vector3.Dot(fromTail, heading);
        float discriminant = alongHeading * alongHeading + restLength * restLength - fromTail.sqrMagnitude;
        float travel = Mathf.Max(0f, -alongHeading + Mathf.Sqrt(Mathf.Max(0f, discriminant)));
        travel = MaxTravelOnTaggedFloors(headStart, heading, travel, headCollider);
        headTarget = headStart + heading * travel;

        headRotationStart = head.rotation;
        Vector3 finalBodyDirection = headTarget - tailStart;
        finalBodyDirection.y = 0f;
        headRotationTarget = Quaternion.LookRotation(finalBodyDirection.normalized, Vector3.up);
    }

    private void PositionMiddleSegments(float archAmount)
    {
        Vector3 body = tail.position - head.position;
        middleFront.position = head.position + body * frontFraction +
            Vector3.up * (Mathf.Sin(Mathf.PI * frontFraction) * archHeight * archAmount);
        middleBack.position = head.position + body * backFraction +
            Vector3.up * (Mathf.Sin(Mathf.PI * backFraction) * archHeight * archAmount);
    }

    private bool CacheFloors()
    {
        floorBreaks = gridBoard.GetComponentsInChildren<FloorBreak>();
        if (floorBreaks.Length == 0)
            return false;

        floorColliders = new BoxCollider[floorBreaks.Length];
        for (int i = 0; i < floorBreaks.Length; i++)
        {
            floorColliders[i] = floorBreaks[i].GetComponentInParent<BoxCollider>();
            if (floorColliders[i] == null)
                return false;
        }

        return true;
    }

    private float MaxTravelOnTaggedFloors(Vector3 origin, Vector3 direction, float travel, SphereCollider segment)
    {
        if (travel <= 0f || !IsSupportedByTaggedFloor(origin, segment))
            return 0f;

        float step = Mathf.Max(0.02f, segment.bounds.extents.x * 0.25f);
        float lastSafe = 0f;
        for (float distance = step; distance < travel; distance += step)
        {
            if (!IsSupportedByTaggedFloor(origin + direction * distance, segment))
                return lastSafe;
            lastSafe = distance;
        }

        return IsSupportedByTaggedFloor(origin + direction * travel, segment) ? travel : lastSafe;
    }

    private bool IsSupportedByTaggedFloor(Vector3 position, SphereCollider segment)
    {
        float radius = segment.radius * Mathf.Max(
            Mathf.Abs(segment.transform.lossyScale.x),
            Mathf.Abs(segment.transform.lossyScale.y),
            Mathf.Abs(segment.transform.lossyScale.z));

        if (!HasTaggedFloorBelow(position))
            return false;

        // Probe the edge of the segment as well, so its center cannot leave part of it off the floor.
        for (int i = 0; i < 8; i++)
        {
            float angle = i * Mathf.PI * 0.25f;
            Vector3 sample = position + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
            if (!HasTaggedFloorBelow(sample))
                return false;
        }

        return true;
    }

    private bool HasTaggedFloorBelow(Vector3 position)
    {
        Ray ray = new Ray(position + Vector3.up * 0.5f, Vector3.down);
        int count = Physics.RaycastNonAlloc(ray, floorHits, floorProbeDistance, ~0, QueryTriggerInteraction.Collide);
        for (int i = 0; i < count; i++)
        {
            if (floorHits[i].collider.CompareTag("floor"))
                return true;
        }

        // RaycastNonAlloc can fill its buffer before returning every overlapping floor.
        if (count == floorHits.Length)
        {
            RaycastHit[] allHits = Physics.RaycastAll(ray, floorProbeDistance, ~0, QueryTriggerInteraction.Collide);
            foreach (RaycastHit hit in allHits)
            {
                if (hit.collider.CompareTag("floor"))
                    return true;
            }
        }

        return false;
    }

    private void UpdateCurrentFloor()
    {
        FloorBreak nextFloor = FindFloorAt(head.position);
        if (nextFloor == null || nextFloor == currentFloor)
            return;

        currentFloor = nextFloor;
        currentFloor.PlayBreak();
    }

    private FloorBreak FindFloorAt(Vector3 position)
    {
        FloorBreak nearest = null;
        float nearestDistance = float.PositiveInfinity;

        for (int i = 0; i < floorColliders.Length; i++)
        {
            BoxCollider floor = floorColliders[i];
            FloorBreak floorBreak = floorBreaks[i];
            if (floor == null || !floor.enabled || !floor.gameObject.activeInHierarchy ||
                floorBreak == null || (floorBreak.IsBreaking && floorBreak != currentFloor))
                continue;

            Bounds bounds = floor.bounds;
            if (position.x < bounds.min.x || position.x >= bounds.max.x ||
                position.z < bounds.min.z || position.z >= bounds.max.z)
                continue;

            if (!Physics.ComputePenetration(
                    headCollider, position, head.rotation,
                    floor, floor.transform.position, floor.transform.rotation,
                    out _, out _))
                continue;

            float dx = position.x - bounds.center.x;
            float dz = position.z - bounds.center.z;
            float distance = dx * dx + dz * dz;
            if (distance >= nearestDistance)
                continue;

            nearest = floorBreak;
            nearestDistance = distance;
        }

        return nearest;
    }
}
