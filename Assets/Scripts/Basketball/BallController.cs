using System.Collections;
using ChainNet.Data;
using ChainNet.Gameplay;
using UnityEngine;

namespace ChainNet.Basketball
{
    /// <summary>
    /// Full-featured ball controller: arc movement, shot resolution, loose-ball physics.
    /// </summary>
    public class BallController : MonoBehaviour
    {
        // ── State ─────────────────────────────────────────────────────────────
        public BallState State { get; private set; }
        public Transform HolderHand { get; private set; }
        public PlayerRuntime HolderRuntime { get; private set; }

        // ── Inspector ─────────────────────────────────────────────────────────
        [SerializeField] private float arcHeight = 5f;
        [SerializeField] private float travelTime = 0.6f;
        [SerializeField] private Rigidbody rb;
        [SerializeField] private Transform hoopTransform;

        // ── Events ────────────────────────────────────────────────────────────
        public event System.Action<PlayerRuntime, bool> OnShotResolved;  // (shooter, madeShot)
        public event System.Action<PlayerRuntime, PlayerRuntime> OnPassCompleted; // (from, to)
        public event System.Action OnBallLoose;

        // ─────────────────────────────────────────────────────────────────────
        public void AttachToHolder(Transform hand, PlayerRuntime runtime)
        {
            StopAllCoroutines();
            if (rb != null) rb.isKinematic = true;
            HolderHand = hand;
            HolderRuntime = runtime;
            State = BallState.Held;
            transform.SetParent(hand, false);
            transform.localPosition = Vector3.zero;
        }

        public void BecomeLoose(Vector3 velocity = default)
        {
            StopAllCoroutines();
            HolderHand = null;
            HolderRuntime = null;
            State = BallState.Loose;
            transform.SetParent(null, true);
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.linearVelocity = velocity;
            }

            OnBallLoose?.Invoke();
        }

        /// <summary>Launch a shot arc toward the hoop.</summary>
        public void LaunchShot(PlayerRuntime shooter, float shotChance, Vector3 hoopPosition)
        {
            if (State != BallState.Held) return;

            StopAllCoroutines();
            transform.SetParent(null, true);
            if (rb != null) rb.isKinematic = true;
            State = BallState.Shooting;
            HolderHand = null;
            HolderRuntime = null;

            StartCoroutine(ShotArc(shooter, shotChance, hoopPosition));
        }

        /// <summary>Launch a pass arc toward a receiver's hand.</summary>
        public void LaunchPass(PlayerRuntime from, Transform receiverHand, PlayerRuntime toRuntime)
        {
            if (State != BallState.Held) return;

            StopAllCoroutines();
            transform.SetParent(null, true);
            if (rb != null) rb.isKinematic = true;
            State = BallState.Passing;
            HolderHand = null;
            HolderRuntime = null;

            StartCoroutine(PassArc(from, receiverHand, toRuntime));
        }

        // ── Private coroutines ────────────────────────────────────────────────
        private IEnumerator ShotArc(PlayerRuntime shooter, float shotChance, Vector3 hoopPosition)
        {
            var start = transform.position;
            var mid = Vector3.Lerp(start, hoopPosition, 0.5f) + Vector3.up * arcHeight;
            var elapsed = 0f;

            while (elapsed < travelTime)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / travelTime);
                transform.position = QuadraticBezier(start, mid, hoopPosition, t);
                yield return null;
            }

            transform.position = hoopPosition;
            var made = Random.value <= shotChance;
            State = made ? BallState.Scored : BallState.Rebounding;
            OnShotResolved?.Invoke(shooter, made);

            if (!made)
            {
                BecomeLoose(Vector3.right * Random.Range(-3f, 3f) + Vector3.up * 2f);
            }
        }

        private IEnumerator PassArc(PlayerRuntime from, Transform receiverHand, PlayerRuntime toRuntime)
        {
            var start = transform.position;
            Vector3 end;
            var elapsed = 0f;
            var passTime = travelTime * 0.75f;

            while (elapsed < passTime)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / passTime);
                // Track receiver as they move
                end = receiverHand != null ? receiverHand.position : transform.position;
                var mid = Vector3.Lerp(start, end, 0.5f) + Vector3.up * (arcHeight * 0.5f);
                transform.position = QuadraticBezier(start, mid, end, t);
                yield return null;
            }

            if (receiverHand != null)
            {
                AttachToHolder(receiverHand, toRuntime);
                OnPassCompleted?.Invoke(from, toRuntime);
            }
            else
            {
                BecomeLoose();
            }
        }

        private static Vector3 QuadraticBezier(Vector3 p0, Vector3 p1, Vector3 p2, float t)
        {
            var a = Vector3.Lerp(p0, p1, t);
            var b = Vector3.Lerp(p1, p2, t);
            return Vector3.Lerp(a, b, t);
        }
    }
}
