using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public sealed class CameraZoneSwitch : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private bool activateOnStart;

    private void Awake()
    {
        GetComponent<BoxCollider>().isTrigger = true;

        // The player segments have colliders but no Rigidbody. A kinematic body
        // here makes their movement generate trigger callbacks.
        Rigidbody body = GetComponent<Rigidbody>();
        if (body == null)
            body = gameObject.AddComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
    }

    private void Start()
    {
        if (activateOnStart)
            SwitchToTargetCamera();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<InchwormLocomotion>() == null)
            return;

        SwitchToTargetCamera();
    }

    private void SwitchToTargetCamera()
    {
        if (targetCamera == null)
        {
            Debug.LogError("CameraZoneSwitch needs a target camera.", this);
            return;
        }

        // Each zone owns one camera. Find every zone, including those beneath
        // hidden boards, so adding another camera never requires editing old zones.
        CameraZoneSwitch[] zones = FindObjectsByType<CameraZoneSwitch>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (CameraZoneSwitch zone in zones)
        {
            Camera camera = zone.targetCamera;
            if (camera != null && camera != targetCamera)
                SetCameraActive(camera, false);
        }

        SetCameraActive(targetCamera, true);
    }

    private static void SetCameraActive(Camera camera, bool active)
    {
        camera.enabled = active;
        AudioListener listener = camera.GetComponent<AudioListener>();
        if (listener != null)
            listener.enabled = active;
    }
}
