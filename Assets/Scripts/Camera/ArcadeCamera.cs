using ChainNet.Basketball;
using ChainNet.Data;
using UnityEngine;

namespace ChainNet.Camera
{
    /// <summary>
    /// Smooth follow camera for the match scene.
    /// Tracks the ball during flights and loose-ball situations;
    /// otherwise follows whoever has possession, with a look-ahead bias
    /// toward the attack hoop.
    /// </summary>
    public class ArcadeCamera : MonoBehaviour
    {
        [Header("Auto-discovered if not assigned")]
        [SerializeField] private BallController ball;

        [Header("Position")]
        [SerializeField] private Vector3 offset = new(0f, 12f, -10f);
        [SerializeField] private float positionSmoothTime = 0.15f;
        [SerializeField] private float lookSmoothTime = 0.08f;

        [Header("Look-ahead")]
        [SerializeField] private float lookaheadDistance = 4f;

        [Header("Bounds (optional)")]
        [SerializeField] private bool useBounds;
        [SerializeField] private Vector2 boundsX = new(-24f, 24f);
        [SerializeField] private Vector2 boundsZ = new(-18f, 18f);

        // ── Runtime ────────────────────────────────────────────────────────────
        private Vector3 posVelocity;
        private Vector3 lookVelocity;
        private Vector3 smoothLookTarget;

        private void Start()
        {
            if (ball == null)
                ball = FindFirstObjectByType<BallController>();

            smoothLookTarget = GetTrackingTarget();
            // Snap camera into position immediately on Start
            transform.position = smoothLookTarget + offset;
            transform.LookAt(smoothLookTarget);
        }

        private void LateUpdate()
        {
            var target = GetTrackingTarget();

            smoothLookTarget = Vector3.SmoothDamp(smoothLookTarget, target, ref lookVelocity, lookSmoothTime);

            var desiredPos = smoothLookTarget + offset;
            if (useBounds)
            {
                desiredPos.x = Mathf.Clamp(desiredPos.x, boundsX.x, boundsX.y);
                desiredPos.z = Mathf.Clamp(desiredPos.z, boundsZ.x, boundsZ.y);
            }

            transform.position = Vector3.SmoothDamp(transform.position, desiredPos, ref posVelocity, positionSmoothTime);
            transform.LookAt(smoothLookTarget);
        }

        // ── Tracking logic ─────────────────────────────────────────────────────
        private Vector3 GetTrackingTarget()
        {
            if (ball == null) return Vector3.zero;

            // Track the ball directly when it is airborne or loose
            if (ball.State == BallState.Shooting
                || ball.State == BallState.Passing
                || ball.State == BallState.Rebounding
                || ball.State == BallState.Loose)
            {
                return ball.transform.position;
            }

            // When held, follow the holder with a small look-ahead in the
            // direction from holder to ball (proxy for movement direction)
            if (ball.HolderHand != null)
            {
                var holderPos = ball.HolderHand.position;
                var dir = (ball.transform.position - holderPos).normalized;
                return holderPos + dir * lookaheadDistance;
            }

            return ball.transform.position;
        }
    }
}
