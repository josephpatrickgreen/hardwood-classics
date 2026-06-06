using System;
using ChainNet.Core;
using ChainNet.Data;
using ChainNet.Gameplay;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ChainNet.Basketball
{
    public class MatchManager : MonoBehaviour
    {
        [SerializeField] private string mapSceneName = "CircuitMap";
        [SerializeField] private PossessionManager possessionManager;
        [SerializeField] private ScoreManager scoreManager;

        public TeamRuntime playerTeam;
        public TeamRuntime enemyTeam;
        public CourtData activeCourt;
        public int playerScore;
        public int enemyScore;
        public int targetScore = 21;
        public bool MatchActive { get; private set; }

        public static event Action<TeamRuntime> OnScore;
        public static event Action<bool> OnMatchEnd;   // true = player won

        public void StartMatch()
        {
            playerScore = 0;
            enemyScore = 0;
            MatchActive = true;
        }

        public void EndMatch(bool playerWon)
        {
            if (!MatchActive) return;
            MatchActive = false;
            OnMatchEnd?.Invoke(playerWon);
            Debug.Log($"Match ended. Player won: {playerWon}");
            // Return to map after brief delay so reward screen can show first
        }

        public void ReturnToMap()
        {
            SceneManager.LoadScene(mapSceneName);
        }

        public void Score(TeamRuntime scoringTeam, int points)
        {
            if (!MatchActive) return;

            if (scoringTeam == playerTeam)
                playerScore += points;
            else
                enemyScore += points;

            OnScore?.Invoke(scoringTeam);
            UIManager.RaiseScore(scoringTeam, points);

            // Possession changes after made basket
            possessionManager?.ChangePossession(scoringTeam == playerTeam ? enemyTeam : playerTeam);

            if (playerScore >= targetScore || enemyScore >= targetScore)
                EndMatch(playerScore >= targetScore);
        }

        /// <summary>Attempt a shot from the given shooter. Runs the shot arc and resolves result.</summary>
        public void AttemptShot(PlayerRuntime shooter, BallController ball, Transform hoopTransform,
            float defenderContest, float distancePenalty, Events.HypeManager hypeManager)
        {
            if (!MatchActive || ball == null || ball.State != BallState.Held) return;

            var isPlayerShot = playerTeam?.players.Contains(shooter) ?? false;
            var team = isPlayerShot ? playerTeam : enemyTeam;
            var hypeLevel = hypeManager != null ? hypeManager.GetHypeLevel(team) : HypeLevel.Quiet;
            var hypeBonus = hypeLevel >= HypeLevel.Hot ? 0.05f * ((int)hypeLevel - 1) : 0f;
            var trinketBonus = TrinketHelper.GetShotBonus(shooter);

            var chance = ShotCalculator.CalculateShotChance(shooter, defenderContest, distancePenalty,
                hypeBonus, trinketBonus);

            ball.OnShotResolved += (s, made) =>
            {
                if (made)
                {
                    var distance = hoopTransform != null
                        ? Vector3.Distance(s.data != null ? hoopTransform.position : Vector3.zero, hoopTransform.position)
                        : 0f;
                    var pts = scoreManager != null ? scoreManager.GetPointsForShot(distance) : 1;
                    Score(team, pts);
                    hypeManager?.AddHype(team, made ? 5f : 0f);
                }
                // Unsubscribe to avoid duplicate calls
                ball.OnShotResolved -= null;
            };

            ball.LaunchShot(shooter, chance, hoopTransform != null ? hoopTransform.position : Vector3.up * 3f);
        }
    }

    public class PossessionManager : MonoBehaviour
    {
        public TeamRuntime CurrentPossessionTeam { get; private set; }

        public static event Action<TeamRuntime> OnPossessionChanged;

        public void SetInitialPossession(TeamRuntime team)
        {
            CurrentPossessionTeam = team;
            OnPossessionChanged?.Invoke(team);
        }

        public void ChangePossession(TeamRuntime nextTeam)
        {
            CurrentPossessionTeam = nextTeam;
            OnPossessionChanged?.Invoke(nextTeam);
        }

        public bool IsPlayerPossession(TeamRuntime playerTeam) => CurrentPossessionTeam == playerTeam;
    }

    public class ScoreManager : MonoBehaviour
    {
        private const float TwoPointLineDistance = 6.75f;
        public int GetPointsForShot(float distance) => distance > TwoPointLineDistance ? 2 : 1;
    }

    public static class ShotCalculator
    {
        private const float BaseChance = 0.35f;
        private const float JumperWeight = 0.02f;
        private const float ContestPenaltyWeight = 0.25f;
        private const float MaxStaminaPenalty = 0.2f;
        private const float MinShotChance = 0.05f;
        private const float MaxShotChance = 0.95f;

        public static float CalculateShotChance(PlayerRuntime shooter, float defenderContest,
            float distancePenalty, float hypeBonus, float trinketBonus)
        {
            var staminaFraction = Mathf.Clamp01(1f - (shooter.stamina / 100f));
            var chance = BaseChance
                + shooter.currentStats.jumper * JumperWeight
                - defenderContest * ContestPenaltyWeight
                - distancePenalty
                + hypeBonus
                + trinketBonus
                - staminaFraction * MaxStaminaPenalty;
            return Mathf.Clamp(chance, MinShotChance, MaxShotChance);
        }
    }

    public static class TrinketHelper
    {
        public static float GetShotBonus(PlayerRuntime player)
        {
            var bonus = 0f;
            foreach (var trinket in player.equippedTrinkets)
            {
                foreach (var mod in trinket.modifiers)
                {
                    if (mod.stat == "jumper" || mod.stat == "finish")
                        bonus += mod.amount * 0.01f;
                }
            }

            return bonus;
        }

        public static float GetDefenseBonus(PlayerRuntime player)
        {
            var bonus = 0f;
            foreach (var trinket in player.equippedTrinkets)
            {
                foreach (var mod in trinket.modifiers)
                {
                    if (mod.stat == "clamps" || mod.stat == "swipe")
                        bonus += mod.amount * 0.01f;
                }
            }

            return bonus;
        }
    }
}
