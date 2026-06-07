using System.Collections.Generic;
using ChainNet.Basketball;
using ChainNet.Data;
using ChainNet.Events;
using ChainNet.Gameplay;
using UnityEngine;

namespace ChainNet.Characters
{
    /// <summary>
    /// Human player controller. Arcade-responsive movement + action buttons.
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        // ── Config ────────────────────────────────────────────────────────────
        [SerializeField] private float moveSpeed = 6f;
        [SerializeField] private float sprintMultiplier = 1.35f;

        // ── Runtime references (set by MatchBootstrapper) ──────────────────
        private PlayerRuntime runtime;
        private BallController ball;
        private MatchManager matchManager;
        private FoulManager foulManager;
        private HypeManager hypeManager;
        private HeatManager heatManager;
        private DirtyPlayManager dirtyPlayManager;
        private Transform attackHoop;  // hoop the player is attacking

        private CharacterController controller;
        private FoulWindowManager foulWindowManager;
        private bool dirtyModifierHeld;
        private bool hasBall;

        public void SetRuntimeData(PlayerRuntime rt, BallController b, MatchManager mm,
            FoulManager fm, HypeManager hypm, HeatManager heatm,
            DirtyPlayManager dpm, Transform hoop)
        {
            runtime = rt;
            ball = b;
            matchManager = mm;
            foulManager = fm;
            hypeManager = hypm;
            heatManager = heatm;
            dirtyPlayManager = dpm;
            attackHoop = hoop;
        }

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            foulWindowManager = FindFirstObjectByType<Basketball.FoulWindowManager>();
        }

        private void Update()
        {
            Move();

            dirtyModifierHeld = Input.GetKey(KeyCode.LeftControl);

            if (Input.GetKeyDown(KeyCode.Space)) TryShoot();
            if (Input.GetKeyDown(KeyCode.E)) TryPass();
            if (Input.GetKeyDown(KeyCode.Q)) UseDribbleMove();
            if (Input.GetKeyDown(KeyCode.R)) UseSpecial();
            if (Input.GetKeyDown(KeyCode.F)) TryCallFoul();
            if (Input.GetKeyDown(KeyCode.C)) TrySteal();
            if (Input.GetKeyDown(KeyCode.V)) TryBlock();

            // Update stamina drain
            if (runtime != null && Input.GetKey(KeyCode.LeftShift))
                runtime.stamina = Mathf.Max(0f, runtime.stamina - Time.deltaTime * 6f);
            else if (runtime != null)
                runtime.stamina = Mathf.Min(100f, runtime.stamina + Time.deltaTime * 4f);
        }

        // ── Movement ──────────────────────────────────────────────────────────
        private void Move()
        {
            var move = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical")).normalized;
            var sprint = Input.GetKey(KeyCode.LeftShift);
            var speedMult = sprint ? sprintMultiplier : 1f;
            controller?.Move(move * (moveSpeed * speedMult * Time.deltaTime));
        }

        // ── Actions ───────────────────────────────────────────────────────────
        private void TryShoot()
        {
            if (ball == null || ball.State != BallState.Held) return;
            if (ball.HolderRuntime != runtime) return;
            if (attackHoop == null) return;

            var dist = Vector3.Distance(transform.position, attackHoop.position);
            var distPenalty = Mathf.Clamp01((dist - 3f) / 15f) * 0.35f;
            matchManager?.AttemptShot(runtime, ball, attackHoop, 0f, distPenalty, hypeManager);
        }

        private void TryPass()
        {
            if (ball == null || ball.State != BallState.Held) return;
            if (ball.HolderRuntime != runtime) return;
            // Find nearest teammate
            var teammate = FindNearestTeammate();
            if (teammate == null) return;

            ball.LaunchPass(runtime, teammate.transform, GetRuntimeForTransform(teammate));
        }

        private void UseDribbleMove()
        {
            // Dribble move: small momentum burst in movement direction
            if (runtime == null) return;
            var move = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical")).normalized;
            controller?.Move(move * (moveSpeed * 0.6f));
        }

        private void UseSpecial()
        {
            if (runtime == null) return;
            var sc = FindFirstObjectByType<SpecialController>();
            sc?.UseSpecial(runtime);
        }

        private void TryCallFoul()
        {
            if (foulManager == null || matchManager == null) return;
            var heat = heatManager != null ? heatManager.currentHeat / 100f * 0.3f : 0f;
            var court = matchManager.activeCourt;
            var valid = foulManager.CallFoul(runtime, GetNearestEnemy(), court, heat);
            Debug.Log($"Foul call: {(valid ? "VALID" : "INVALID")}");
            if (foulManager.LastInvalidCallCancelledShot)
                Debug.Log(">>> Shot CANCELLED by bad foul call.");
        }

        private void TrySteal()
        {
            var nearest = GetNearestEnemyWithBall();
            if (nearest == null) return;

            if (dirtyModifierHeld && dirtyPlayManager != null)
            {
                var result = dirtyPlayManager.ResolveDirtyPlay(runtime, nearest, false);
                // Open foul window so the player can immediately call a foul
                foulWindowManager?.RegisterContact(runtime, nearest, result.heatGenerated / 10f,
                    true, false);
                if (result.success)
                    ball?.BecomeLoose(Random.insideUnitSphere * 3f);
            }
            else
            {
                // Clean steal: swipe stat vs handle
                var chance = 0.1f + runtime.currentStats.swipe * 0.02f - nearest.currentStats.handle * 0.015f;
                if (Random.value < chance)
                    ball?.AttachToHolder(transform, runtime);
            }
        }

        private void TryBlock()
        {
            if (ball == null || ball.State != BallState.Shooting) return;
            var chance = 0.1f + runtime.currentStats.bounce * 0.025f;
            if (Random.value < chance)
            {
                var shooter = ball.HolderRuntime ?? GetNearestEnemy();
                ball?.BecomeLoose(Vector3.up * 2f + Random.insideUnitSphere);
                hypeManager?.AddHype(matchManager.playerTeam, 8f);
                // Register contact so the shooter can call a foul on the block
                foulWindowManager?.RegisterContact(runtime, shooter, 0.3f, false, true);
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        private Transform FindNearestTeammate()
        {
            Transform best = null;
            var bestDist = float.MaxValue;
            foreach (var ctrl in FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
            {
                if (ctrl == this) continue;
                if (ctrl.runtime == null) continue;
                var d = Vector3.Distance(transform.position, ctrl.transform.position);
                if (d < bestDist) { bestDist = d; best = ctrl.transform; }
            }

            return best;
        }

        private PlayerRuntime GetRuntimeForTransform(Transform t)
        {
            var ctrl = t.GetComponent<PlayerController>();
            return ctrl?.runtime;
        }

        private PlayerRuntime GetNearestEnemy()
        {
            PlayerRuntime nearest = null;
            var bestDist = float.MaxValue;
            foreach (var ai in FindObjectsByType<EnemyAI>(FindObjectsSortMode.None))
            {
                var d = Vector3.Distance(transform.position, ai.transform.position);
                if (d < bestDist) { bestDist = d; nearest = ai.RuntimePlayer; }
            }

            return nearest;
        }

        private PlayerRuntime GetNearestEnemyWithBall()
        {
            if (ball == null || ball.State != BallState.Held) return null;
            var holder = ball.HolderRuntime;
            if (holder == null) return null;
            var isEnemy = matchManager?.enemyTeam?.players.Contains(holder) ?? false;
            return isEnemy ? holder : null;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    public class SpecialController : MonoBehaviour
    {
        public static event System.Action<PlayerRuntime> OnSpecialUsed;

        private readonly Dictionary<PlayerRuntime, float> cooldowns = new();

        public bool CanUseSpecial(PlayerRuntime player)
        {
            if (player?.data?.special == null) return false;
            return !cooldowns.TryGetValue(player, out var cd) || cd <= 0f;
        }

        public void UseSpecial(PlayerRuntime player)
        {
            if (!CanUseSpecial(player)) return;

            cooldowns[player] = player.data.special.cooldownSeconds;
            player.specialCooldownRemaining = player.data.special.cooldownSeconds;
            ApplyPrototypeSpecialEffect(player);
            OnSpecialUsed?.Invoke(player);
        }

        public void TickCooldowns(float deltaTime)
        {
            var keys = new List<PlayerRuntime>(cooldowns.Keys);
            foreach (var player in keys)
            {
                cooldowns[player] = Mathf.Max(0f, cooldowns[player] - deltaTime);
                player.specialCooldownRemaining = cooldowns[player];
            }
        }

        public void ReduceCooldown(PlayerRuntime player, float amount)
        {
            if (!cooldowns.TryGetValue(player, out _)) return;
            cooldowns[player] = Mathf.Max(0f, cooldowns[player] - amount);
            player.specialCooldownRemaining = cooldowns[player];
        }

        private static void ApplyPrototypeSpecialEffect(PlayerRuntime player)
        {
            switch (player.data.special.specialType)
            {
                case SpecialType.YoYoPass:
                case SpecialType.BoomBoxBounce:
                    player.currentStats.vision += 1;
                    break;
                case SpecialType.PosterChild:
                case SpecialType.FenceRattler:
                    player.currentStats.finish += 1;
                    break;
                case SpecialType.ChainNetSniper:
                    player.currentStats.jumper += 1;
                    break;
                case SpecialType.HometownWhistle:
                    player.currentStats.swagger += 2;
                    break;
                case SpecialType.PocketCheck:
                    player.currentStats.swipe += 2;
                    break;
                case SpecialType.NoBloodNoFoul:
                    player.currentStats.frame += 1;
                    break;
                case SpecialType.ElbowRoom:
                    player.currentStats.edge += 1;
                    break;
            }
        }
    }
}
