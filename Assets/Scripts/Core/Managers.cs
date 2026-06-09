using System;
using System.Collections.Generic;
using ChainNet.Data;
using ChainNet.Gameplay;
using ChainNet.RoguelikeMap;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ChainNet.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void StartRun() => SceneManager.LoadScene("CircuitMap");
        public void LoadMainMenu() => SceneManager.LoadScene("MainMenu");
    }

    public class RunManager : MonoBehaviour
    {
        public static RunManager Instance { get; private set; }
        public RunState CurrentRun { get; private set; }

        private readonly MapGenerator generator = new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void StartCircuitRun(TeamData startingTeam)
        {
            var layers = generator.Generate();
            var allNodes = new List<MapNodeRuntime>();
            foreach (var layer in layers)
            {
                allNodes.AddRange(layer);
            }

            CurrentRun = new RunState
            {
                runId = Guid.NewGuid().ToString("N"),
                playerTeam = new TeamRuntime(startingTeam),
                mapNodes = allNodes
            };
        }
    }

    public class TeamManager : MonoBehaviour
    {
        public TeamRuntime PlayerTeam { get; private set; }
        public TeamRuntime EnemyTeam { get; private set; }

        public void SetTeams(TeamRuntime player, TeamRuntime enemy)
        {
            PlayerTeam = player;
            EnemyTeam = enemy;
        }
    }

    public class UIManager : MonoBehaviour
    {
        public static event Action<TeamRuntime, int> OnScore;
        public static event Action<float> OnHeatChanged;
        public static event Action<float> OnHypeChanged;
        /// <summary>Raised once when the player team's hype first hits 100 during a match.</summary>
        public static event Action OnMaxHypeReached;

        public static void RaiseScore(TeamRuntime team, int points) => OnScore?.Invoke(team, points);
        public static void RaiseHeat(float heat) => OnHeatChanged?.Invoke(heat);
        public static void RaiseHype(float hype) => OnHypeChanged?.Invoke(hype);
        public static void RaiseMaxHype() => OnMaxHypeReached?.Invoke();
    }
}
