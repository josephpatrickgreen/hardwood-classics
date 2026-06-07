using System;
using ChainNet.Core;
using ChainNet.Data;
using ChainNet.Gameplay;
using UnityEngine;

namespace ChainNet.Events
{
    public class HypeManager : MonoBehaviour
    {
        public float playerHype;
        public float enemyHype;

        public void AddHype(TeamRuntime team, float amount)
        {
            if (team == null) return;
            if (RunManager.Instance?.CurrentRun?.playerTeam == team)
            {
                var wasBelow = playerHype < 100f;
                playerHype += amount;
                if (wasBelow && playerHype >= 100f)
                    UIManager.RaiseMaxHype();
            }
            else
            {
                enemyHype += amount;
            }

            UIManager.RaiseHype(GetCurrentHype(team));
        }

        public void ReduceHype(TeamRuntime team, float amount) => AddHype(team, -amount);
        public void AddEnemyHype(float amount)
        {
            enemyHype += amount;
            UIManager.RaiseHype(enemyHype);
        }

        public HypeLevel GetHypeLevel(TeamRuntime team)
        {
            var hype = GetCurrentHype(team);
            if (hype < 20f) return HypeLevel.Quiet;
            if (hype < 40f) return HypeLevel.Buzzing;
            if (hype < 60f) return HypeLevel.Hot;
            if (hype < 80f) return HypeLevel.CrowdIn;
            if (hype < 100f) return HypeLevel.Legendary;
            return HypeLevel.TapeMoment;
        }

        private float GetCurrentHype(TeamRuntime team)
        {
            return RunManager.Instance?.CurrentRun?.playerTeam == team ? playerHype : enemyHype;
        }
    }

    public class HeatManager : MonoBehaviour
    {
        public float currentHeat;
        public float maxHeat = 100f;

        public void AddHeat(float amount)
        {
            currentHeat = Mathf.Clamp(currentHeat + amount, 0f, maxHeat);
            UIManager.RaiseHeat(currentHeat);
        }

        public void ReduceHeat(float amount)
        {
            currentHeat = Mathf.Clamp(currentHeat - amount, 0f, maxHeat);
            UIManager.RaiseHeat(currentHeat);
        }

        public bool IsBoiling() => currentHeat >= maxHeat * 0.75f;
    }

    public class DirtyPlayManager : MonoBehaviour
    {
        [SerializeField] private HeatManager heatManager;

        public DirtyPlayResult ResolveDirtyPlay(PlayerRuntime actor, PlayerRuntime defender, bool successBias)
        {
            var edge = actor.currentStats.edge * 0.02f;
            var defense = defender.currentStats.cool * 0.015f;
            var success = UnityEngine.Random.value + edge - defense + (successBias ? 0.1f : 0f) > 0.5f;

            var result = new DirtyPlayResult
            {
                success = success,
                heatGenerated = success ? 10f : 6f,
                injuryChance = success ? 0.08f : 0.03f,
                foulChance = success ? 0.35f : 0.2f,
                fightChance = success ? 0.2f : 0.1f
            };

            heatManager.AddHeat(result.heatGenerated);
            return result;
        }
    }

    public class FoulManager : MonoBehaviour
    {
        [SerializeField] private HeatManager heatManager;
        [SerializeField] private HypeManager hypeManager;

        private FoulOpportunity activeOpportunity;
        public bool LastInvalidCallCancelledShot { get; private set; }

        public void SetOpportunity(FoulOpportunity opportunity)
        {
            activeOpportunity = opportunity;
        }

        public bool CallFoul(PlayerRuntime caller, PlayerRuntime defender, CourtData court, float currentHeatPenalty)
        {
            LastInvalidCallCancelledShot = false;
            if (caller == null || defender == null || court == null)
            {
                LastInvalidCallCancelledShot = true;
                return false;
            }

            if (activeOpportunity == null || activeOpportunity.timeRemaining <= 0f)
            {
                ApplyInvalidCall(caller, defender);
                return false;
            }

            var credibility = activeOpportunity.contactIntensity
                             + caller.currentStats.swagger * 0.03f
                             + caller.currentStats.cool * 0.02f
                             - defender.currentStats.cool * 0.02f
                             + court.foulStrictness
                             - currentHeatPenalty;

            if (activeOpportunity.dirtyActionUsed)
            {
                credibility += 0.15f;
            }

            var valid = credibility > 0.5f;
            if (valid)
            {
                heatManager.AddHeat(4f);
                if (RunManager.Instance?.CurrentRun?.playerTeam != null)
                {
                    hypeManager.AddHype(RunManager.Instance.CurrentRun.playerTeam, 4f);
                }
            }
            else
            {
                ApplyInvalidCall(caller, defender);
            }

            activeOpportunity = null;
            return valid;
        }

        private void ApplyInvalidCall(PlayerRuntime caller, PlayerRuntime defender)
        {
            LastInvalidCallCancelledShot = true; // Explicit: bad foul call cancels the shot.
            heatManager.AddHeat(8f);
            var playerTeam = RunManager.Instance?.CurrentRun?.playerTeam;
            if (playerTeam != null)
            {
                hypeManager.ReduceHype(playerTeam, 5f);
            }

            hypeManager.AddEnemyHype(4f);
        }
    }

    public class FightManager : MonoBehaviour
    {
        [SerializeField] private HeatManager heatManager;

        public bool TryResolveFight(PlayerRuntime player, PlayerRuntime enemy)
        {
            if (!heatManager.IsBoiling())
            {
                return false;
            }

            var fightScore = player.currentStats.edge + player.currentStats.frame + UnityEngine.Random.Range(0, 20);
            var enemyScore = enemy.currentStats.edge + enemy.currentStats.frame + UnityEngine.Random.Range(0, 20);
            var won = fightScore >= enemyScore;
            heatManager.ReduceHeat(25f);
            return won;
        }
    }
}
