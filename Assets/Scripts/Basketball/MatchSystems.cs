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
        public TeamRuntime playerTeam;
        public TeamRuntime enemyTeam;
        public CourtData activeCourt;
        public int playerScore;
        public int enemyScore;
        public int targetScore = 21;

        public static event Action<TeamRuntime> OnScore;

        public void StartMatch()
        {
            playerScore = 0;
            enemyScore = 0;
        }

        public void EndMatch(bool playerWon)
        {
            enabled = false;
            Debug.Log($"Match ended. Player won: {playerWon}");
            SceneManager.LoadScene(mapSceneName);
        }

        public void Score(TeamRuntime scoringTeam, int points)
        {
            if (scoringTeam == playerTeam)
            {
                playerScore += points;
            }
            else
            {
                enemyScore += points;
            }

            OnScore?.Invoke(scoringTeam);
            UIManager.RaiseScore(scoringTeam, points);

            if (playerScore >= targetScore || enemyScore >= targetScore)
            {
                EndMatch(playerScore >= targetScore);
            }
        }
    }

    public class PossessionManager : MonoBehaviour
    {
        public TeamRuntime currentPossessionTeam;

        public void ChangePossession(TeamRuntime nextTeam)
        {
            currentPossessionTeam = nextTeam;
        }
    }

    public class ScoreManager : MonoBehaviour
    {
        public int GetPointsForShot(float distance) => distance > 6.75f ? 2 : 1;
    }

    public class BallController : MonoBehaviour
    {
        public BallState state;
        public Transform holder;

        public void AttachToHolder(Transform hand)
        {
            holder = hand;
            state = BallState.Held;
            transform.SetParent(hand, false);
        }

        public void BecomeLoose()
        {
            holder = null;
            state = BallState.Loose;
            transform.SetParent(null, true);
        }
    }

    public static class ShotCalculator
    {
        public static float CalculateShotChance(PlayerRuntime shooter, float defenderContest, float distancePenalty, float hypeBonus, float trinketBonus)
        {
            var chance = 0.35f;
            chance += shooter.currentStats.jumper * 0.02f;
            chance -= defenderContest * 0.25f;
            chance -= distancePenalty;
            chance += hypeBonus;
            chance += trinketBonus;
            chance -= Mathf.Clamp01(1f - (shooter.stamina / 100f)) * 0.2f;
            return Mathf.Clamp(chance, 0.05f, 0.95f);
        }
    }
}
