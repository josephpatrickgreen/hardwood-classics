using System.Collections.Generic;
using ChainNet.Basketball;
using ChainNet.Core;
using ChainNet.Data;
using ChainNet.Events;
using ChainNet.Gameplay;
using ChainNet.Save;
using UnityEngine;

namespace ChainNet.Characters
{
    /// <summary>
    /// Spawns and wires up both teams when the Match scene loads.
    /// Pulls data from MatchContext, creates PlayerRuntime instances, and places prefabs.
    /// </summary>
    public class MatchBootstrapper : MonoBehaviour
    {
        [SerializeField] private MatchManager matchManager;
        [SerializeField] private PossessionManager possessionManager;
        [SerializeField] private HypeManager hypeManager;
        [SerializeField] private HeatManager heatManager;
        [SerializeField] private FoulManager foulManager;
        [SerializeField] private DirtyPlayManager dirtyPlayManager;
        [SerializeField] private FightManager fightManager;

        [SerializeField] private Transform playerSpawnRoot;
        [SerializeField] private Transform enemySpawnRoot;
        [SerializeField] private Transform hoopPlayerSide;
        [SerializeField] private Transform hoopEnemySide;
        [SerializeField] private BallController ball;

        [SerializeField] private Vector3[] playerSpawnOffsets =
        {
            new(0f, 0f, 2f),
            new(-3f, 0f, 0f),
            new(3f, 0f, 0f)
        };

        [SerializeField] private Vector3[] enemySpawnOffsets =
        {
            new(0f, 0f, -2f),
            new(-3f, 0f, -4f),
            new(3f, 0f, -4f)
        };

        private readonly List<PlayerBinding> playerBindings = new();
        private readonly List<PlayerBinding> enemyBindings = new();

        private UnlockTracker unlockTracker;

        private void Start()
        {
            var ctx = MatchContext.Instance;
            if (ctx == null)
            {
                Debug.LogWarning("MatchBootstrapper: No MatchContext found — using default test setup.");
                return;
            }

            // Apply injury penalties before the match starts
            InjurySystem.ApplyInjuryPenalties(ctx.PlayerTeam);

            SpawnTeam(ctx.PlayerTeam, playerSpawnRoot, playerSpawnOffsets, playerBindings, true);
            SpawnTeam(ctx.EnemyTeam, enemySpawnRoot, enemySpawnOffsets, enemyBindings, false);

            if (matchManager != null)
            {
                matchManager.playerTeam = ctx.PlayerTeam;
                matchManager.enemyTeam = ctx.EnemyTeam;
                matchManager.activeCourt = ctx.Court;
                matchManager.StartMatch();
            }

            if (possessionManager != null)
                possessionManager.SetInitialPossession(ctx.PlayerTeam);

            // Wire UnlockTracker
            unlockTracker = FindFirstObjectByType<UnlockTracker>();
            if (unlockTracker != null)
            {
                unlockTracker.ResetMatchCounters();
                SpecialController.OnSpecialUsed += OnSpecialUsed;
                FightEventController.OnFightResolved += OnFightResolved;
                MatchManager.OnMatchEnd += OnMatchEnd;
            }
        }

        private void OnDestroy()
        {
            if (unlockTracker != null)
            {
                SpecialController.OnSpecialUsed -= OnSpecialUsed;
                FightEventController.OnFightResolved -= OnFightResolved;
                MatchManager.OnMatchEnd -= OnMatchEnd;
            }
        }

        // ── UnlockTracker callbacks ────────────────────────────────────────────
        private void OnSpecialUsed(PlayerRuntime player)
        {
            unlockTracker?.RecordSpecial();
        }

        private void OnFightResolved(bool playerWon)
        {
            if (playerWon) unlockTracker?.RecordFightWon();
        }

        private void OnMatchEnd(bool playerWon)
        {
            if (!playerWon || unlockTracker == null) return;

            var enemyTeamId = MatchContext.Instance?.EnemyTeam?.data?.teamId ?? "";

            // Tick injuries after each completed match
            var run = RunManager.Instance?.CurrentRun;
            if (run != null)
            {
                foreach (var player in run.playerTeam.players)
                    InjurySystem.TickInjury(player);
            }

            unlockTracker.EvaluateUnlocks(enemyTeamId);
        }

        private void SpawnTeam(TeamRuntime team, Transform spawnRoot, Vector3[] offsets,
            List<PlayerBinding> bindings, bool isPlayer)
        {
            if (team == null) return;
            var root = spawnRoot != null ? spawnRoot.position : Vector3.zero;

            for (var i = 0; i < team.players.Count && i < 3; i++)
            {
                var player = team.players[i];
                var prefab = player.data?.playerPrefab;
                var spawnPos = root + (i < offsets.Length ? offsets[i] : Vector3.zero);

                GameObject go;
                if (prefab != null)
                {
                    go = Instantiate(prefab, spawnPos, Quaternion.identity);
                }
                else
                {
                    go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                    go.transform.position = spawnPos;
                    go.name = $"{player.data?.displayName ?? "Player"}_{i}";
                }

                var binding = new PlayerBinding
                {
                    runtime = player,
                    gameObject = go,
                    handTransform = go.transform
                };

                if (isPlayer)
                {
                    var ctrl = go.GetComponent<PlayerController>() ?? go.AddComponent<PlayerController>();
                    ctrl.SetRuntimeData(player, ball, matchManager, foulManager, hypeManager,
                        heatManager, dirtyPlayManager,
                        isPlayer ? hoopEnemySide : hoopPlayerSide);
                    binding.controller = ctrl;
                }
                else
                {
                    var ai = go.GetComponent<EnemyAI>() ?? go.AddComponent<EnemyAI>();
                    ai.SetRuntimeData(player, team,
                        GetPlayerTeamRuntimes(),
                        ball, matchManager, hypeManager, heatManager,
                        isPlayer ? hoopEnemySide : hoopPlayerSide);
                }

                bindings.Add(binding);
            }
        }

        private List<PlayerRuntime> GetPlayerTeamRuntimes()
        {
            var list = new List<PlayerRuntime>();
            if (MatchContext.Instance?.PlayerTeam != null)
                list.AddRange(MatchContext.Instance.PlayerTeam.players);
            return list;
        }

        private class PlayerBinding
        {
            public PlayerRuntime runtime;
            public GameObject gameObject;
            public Transform handTransform;
            public PlayerController controller;
        }
    }
}
