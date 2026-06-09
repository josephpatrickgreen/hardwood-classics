using System.Collections;
using ChainNet.Basketball;
using ChainNet.Characters;
using ChainNet.Core;
using ChainNet.Events;
using ChainNet.Gameplay;
using UnityEngine;

namespace ChainNet.Camera
{
    /// <summary>
    /// NBA Jam-style side/diagonal arcade camera.
    /// Tracks ball and active player action with shake on impact.
    /// Full court is always visible through soft-tracking and zoom.
    /// Subscribes to gameplay events to trigger ZoomIn/ZoomOut/TriggerImpact automatically.
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
        [SerializeField] private float zoomInDuration = 1.5f;  // seconds before returning to normal
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

        private void OnEnable()
        {
            SpecialController.OnSpecialUsed += OnSpecialUsed;
            FightEventController.OnFightResolved += OnFightResolved;
            MatchManager.OnScore += OnScore;
            UIManager.OnMaxHypeReached += OnMaxHypeReached;
        }

        private void OnDisable()
        {
            SpecialController.OnSpecialUsed -= OnSpecialUsed;
            FightEventController.OnFightResolved -= OnFightResolved;
            MatchManager.OnScore -= OnScore;
            UIManager.OnMaxHypeReached -= OnMaxHypeReached;
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

        // ── Game event handlers ───────────────────────────────────────────────

        private void OnSpecialUsed(PlayerRuntime _)
        {
            StopAllCoroutines();
            ZoomIn();
            StartCoroutine(ZoomOutAfterDelay(zoomInDuration));
            TriggerImpact(0.6f);
        }

        private void OnFightResolved(bool _)
        {
            TriggerImpact(2f);
            StopAllCoroutines();
            ZoomIn();
            StartCoroutine(ZoomOutAfterDelay(zoomInDuration));
        }

        private void OnScore(TeamRuntime _) => TriggerImpact(0.4f);

        private void OnMaxHypeReached()
        {
            TriggerImpact(1f);
            StopAllCoroutines();
            ZoomIn();
            StartCoroutine(ZoomOutAfterDelay(zoomInDuration));
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Trigger camera shake (dunks, specials, hard fouls).</summary>
        public void TriggerImpact(float intensity = 1f)
        {
            shakeRemaining = shakeDuration * intensity;
        }

        /// <summary>Zoom in for close action (dunk wind-up, fight).</summary>
        public void ZoomIn() => targetFOV = actionFOV;

        /// <summary>Return to normal FOV.</summary>
        public void ZoomOut() => targetFOV = normalFOV;

        // ── Helpers ───────────────────────────────────────────────────────────
        private IEnumerator ZoomOutAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            ZoomOut();
        }
    }
}
