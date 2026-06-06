using UnityEngine;

namespace ChainNet.Camera
{
    /// <summary>
    /// NBA Jam-style side/diagonal arcade camera.
    /// Tracks ball and active player action with shake on impact.
    /// Full court is always visible through soft-tracking and zoom.
    /// </summary>
    public class CameraController : MonoBehaviour
    {
        [Header("Follow target")]
        [SerializeField] private Transform followTarget;
        [SerializeField] private Transform fallbackTarget;  // e.g. ball when no primary

        [Header("Position")]
        [SerializeField] private Vector3 baseOffset = new(0f, 14f, -12f);
        [SerializeField] private float followSpeed = 5f;
        [SerializeField] private float lateralClamp = 12f;   // max X drift from court centre

        [Header("Rotation")]
        [SerializeField] private float pitchAngle = 45f;
        [SerializeField] private float yawAngle = 0f;

        [Header("Zoom")]
        [SerializeField] private float normalFOV = 60f;
        [SerializeField] private float actionFOV = 52f;    // zoom in on dunks/specials
        [SerializeField] private float fovLerpSpeed = 3f;
        private float targetFOV;

        [Header("Shake")]
        [SerializeField] private float shakeMagnitude = 0.25f;
        [SerializeField] private float shakeDuration = 0.25f;
        private float shakeRemaining;
        private UnityEngine.Camera cam;

        // ─────────────────────────────────────────────────────────────────────
        private void Awake()
        {
            cam = GetComponent<UnityEngine.Camera>();
            targetFOV = normalFOV;
        }

        public void SetTarget(Transform target) => followTarget = target;

        private void LateUpdate()
        {
            var active = followTarget != null ? followTarget : fallbackTarget;
            if (active == null) return;

            // Track X only (side-scrolling style), keep Z relative to court centre
            var desiredX = Mathf.Clamp(active.position.x, -lateralClamp, lateralClamp);
            var desiredPos = new Vector3(desiredX, 0f, active.position.z) + baseOffset;

            transform.position = Vector3.Lerp(transform.position, desiredPos, Time.deltaTime * followSpeed);
            transform.rotation = Quaternion.Euler(pitchAngle, yawAngle, 0f);

            // Zoom
            if (cam != null)
                cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV, Time.deltaTime * fovLerpSpeed);

            // Shake
            if (shakeRemaining > 0f)
            {
                transform.position += Random.insideUnitSphere * shakeMagnitude;
                shakeRemaining -= Time.deltaTime;
            }
        }

        /// <summary>Trigger camera shake (dunks, specials, hard fouls).</summary>
        public void TriggerImpact(float intensity = 1f)
        {
            shakeRemaining = shakeDuration * intensity;
        }

        /// <summary>Zoom in for close action (dunk wind-up, fight).</summary>
        public void ZoomIn() => targetFOV = actionFOV;

        /// <summary>Return to normal FOV.</summary>
        public void ZoomOut() => targetFOV = normalFOV;
    }
}
