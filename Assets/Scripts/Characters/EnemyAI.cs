using System.Collections;
using System.Collections.Generic;
using ChainNet.Basketball;
using ChainNet.Data;
using ChainNet.Events;
using ChainNet.Gameplay;
using UnityEngine;

namespace ChainNet.Characters
{
    /// <summary>
    /// Simple FSM-based enemy AI for prototype matches.
    /// States: Idle → FindBall / Guard → Approach → Shoot / Pass / Steal
    /// </summary>
    public class EnemyAI : MonoBehaviour
    {
        // ── State machine ─────────────────────────────────────────────────────
        private enum AIState { Idle, SeekBall, Offense, Defense, Fighting }

        [SerializeField] private float moveSpeed = 4.5f;
        [SerializeField] private float decisionInterval = 0.6f;

        private AIState state = AIState.Idle;
        private PlayerRuntime runtime;
        private TeamRuntime myTeam;
        private List<PlayerRuntime> enemies;
        private BallController ball;
        private MatchManager matchManager;
        private HypeManager hypeManager;
        private HeatManager heatManager;
        private Transform attackHoop;

        private float nextDecision;
        private Transform moveTarget;
        private CharacterController controller;

        public PlayerRuntime RuntimePlayer => runtime;

        public void SetRuntimeData(PlayerRuntime rt, TeamRuntime team,
            List<PlayerRuntime> enemyRuntimes, BallController b,
            MatchManager mm, HypeManager hyp, HeatManager heat, Transform hoop)
        {
            runtime = rt;
            myTeam = team;
            enemies = enemyRuntimes;
            ball = b;
            matchManager = mm;
            hypeManager = hyp;
            heatManager = heat;
            attackHoop = hoop;
        }

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
        }

        private void Update()
        {
            if (matchManager != null && !matchManager.MatchActive) return;
            if (runtime == null) return;

            if (Time.time >= nextDecision)
            {
                nextDecision = Time.time + decisionInterval + Random.Range(0f, 0.3f);
                Decide();
            }

            ExecuteMovement();
        }

        // ── Decision tree ──────────────────────────────────────────────────────
        private void Decide()
        {
            var weHaveBall = ball != null && ball.State == BallState.Held
                             && myTeam?.players.Contains(ball.HolderRuntime) == true;

            if (weHaveBall)
            {
                if (ball.HolderRuntime == runtime)
                    DecideWithBall();
                else
                    DecideOffBall();
            }
            else
            {
                DecideDefense();
            }
        }

        private void DecideWithBall()
        {
            if (attackHoop == null) { state = AIState.Idle; return; }

            var dist = Vector3.Distance(transform.position, attackHoop.position);

            // Close enough to shoot
            if (dist < 6f)
            {
                state = AIState.Offense;
                var shootChance = 0.45f + runtime.currentStats.finish * 0.02f;
                if (Random.value < 0.7f)
                    StartCoroutine(DelayedShoot(shootChance));
                else
                    PassToTeammate();
            }
            else if (dist < 12f)
            {
                var shootChance = 0.3f + runtime.currentStats.jumper * 0.02f;
                if (Random.value < 0.45f)
                    StartCoroutine(DelayedShoot(shootChance));
                else
                {
                    // Drive toward hoop
                    state = AIState.Offense;
                    moveTarget = attackHoop;
                }
            }
            else
            {
                state = AIState.Offense;
                moveTarget = attackHoop;
            }
        }

        private void DecideOffBall()
        {
            // Move to an open spot
            state = AIState.Offense;
            var randomOffset = new Vector3(Random.Range(-6f, 6f), 0f, Random.Range(-4f, 4f));
            var tempGo = new GameObject("_target");
            tempGo.transform.position = (attackHoop != null ? attackHoop.position : Vector3.zero) + randomOffset;
            moveTarget = tempGo.transform;
            Destroy(tempGo, 2f);
        }

        private void DecideDefense()
        {
            state = AIState.Defense;
            // Guard nearest enemy with ball or just track ball
            if (ball != null && ball.State == BallState.Held)
            {
                var ballHolder = ball.HolderRuntime;
                if (ballHolder != null)
                {
                    foreach (var ctrl in FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
                    {
                        if (ctrl.GetComponent<EnemyAI>() != null) continue;
                        moveTarget = ctrl.transform;
                        break;
                    }
                }
            }
            else if (ball != null)
            {
                // Chase loose ball
                state = AIState.SeekBall;
                var bgo = new GameObject("_btarget");
                bgo.transform.position = ball.transform.position;
                moveTarget = bgo.transform;
                Destroy(bgo, 2f);
            }

            // Attempt steal if close
            if (moveTarget != null)
            {
                var dist = Vector3.Distance(transform.position, moveTarget.position);
                if (dist < 2f && ball?.HolderRuntime != null)
                {
                    var stealChance = 0.08f + runtime.currentStats.swipe * 0.018f;
                    if (Random.value < stealChance)
                    {
                        ball.AttachToHolder(transform, runtime);
                        hypeManager?.AddHype(matchManager.enemyTeam, 5f);
                    }
                }
            }
        }

        private void ExecuteMovement()
        {
            if (moveTarget == null) return;

            var dir = (moveTarget.position - transform.position);
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.25f) return;

            controller?.Move(dir.normalized * (moveSpeed * Time.deltaTime));
        }

        private void PassToTeammate()
        {
            if (ball == null || ball.HolderRuntime != runtime) return;
            if (myTeam == null) return;

            PlayerRuntime bestMate = null;
            Transform bestHand = null;
            var bestScore = float.MinValue;

            foreach (var mate in myTeam.players)
            {
                if (mate == runtime) continue;
                var ai = FindAIForRuntime(mate);
                if (ai == null) continue;
                var d = Vector3.Distance(transform.position, ai.transform.position);
                var score = (30f - d) + mate.currentStats.vision * 0.5f;
                if (score > bestScore) { bestScore = score; bestMate = mate; bestHand = ai.transform; }
            }

            if (bestMate != null)
                ball.LaunchPass(runtime, bestHand, bestMate);
        }

        private IEnumerator DelayedShoot(float chance)
        {
            yield return new WaitForSeconds(Random.Range(0.2f, 0.6f));
            if (ball == null || ball.State != BallState.Held || ball.HolderRuntime != runtime) yield break;
            if (attackHoop == null) yield break;

            var dist = Vector3.Distance(transform.position, attackHoop.position);
            var distPenalty = Mathf.Clamp01((dist - 3f) / 15f) * 0.35f;
            matchManager?.AttemptShot(runtime, ball, attackHoop, 0f, distPenalty, hypeManager);
        }

        private EnemyAI FindAIForRuntime(PlayerRuntime rt)
        {
            foreach (var ai in FindObjectsByType<EnemyAI>(FindObjectsSortMode.None))
            {
                if (ai.runtime == rt) return ai;
            }

            return null;
        }
    }
}
