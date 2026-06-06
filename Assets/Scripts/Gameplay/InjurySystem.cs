using ChainNet.Data;
using UnityEngine;

namespace ChainNet.Gameplay
{
    /// <summary>
    /// Applies and clears stat penalties for injured players.
    /// Call ApplyInjuryPenalties when a match starts and ClearInjury via the OpenGym node.
    /// </summary>
    public static class InjurySystem
    {
        // Stat reduction applied while a player is injured
        private const int InjuryHandlePenalty = 2;
        private const int InjuryJumperPenalty = 2;
        private const int InjuryFinishPenalty = 2;
        private const int InjuryMotorPenalty = 3;
        private const int InjuryStaminaPenalty = 30;

        /// <summary>
        /// Apply stat penalties to every injured player on the team at match start.
        /// Penalties are subtracted in-place and remembered via a tag on the runtime.
        /// </summary>
        public static void ApplyInjuryPenalties(TeamRuntime team)
        {
            if (team == null) return;
            foreach (var player in team.players)
            {
                if (!player.isInjured || player.injuryPenaltyApplied) continue;

                player.currentStats.handle = Mathf.Max(0, player.currentStats.handle - InjuryHandlePenalty);
                player.currentStats.jumper = Mathf.Max(0, player.currentStats.jumper - InjuryJumperPenalty);
                player.currentStats.finish = Mathf.Max(0, player.currentStats.finish - InjuryFinishPenalty);
                player.currentStats.motor = Mathf.Max(0, player.currentStats.motor - InjuryMotorPenalty);
                player.stamina = Mathf.Max(0f, player.stamina - InjuryStaminaPenalty);
                player.injuryPenaltyApplied = true;

                Debug.Log($"[Injury] {player.data?.displayName} is playing hurt — stats reduced.");
            }
        }

        /// <summary>
        /// Restore stat penalties and clear the injury flag for a single player.
        /// Call this at an OpenGym / Heal node, or after a match rest.
        /// </summary>
        public static void ClearInjury(PlayerRuntime player)
        {
            if (player == null || !player.isInjured) return;

            if (player.injuryPenaltyApplied)
            {
                player.currentStats.handle += InjuryHandlePenalty;
                player.currentStats.jumper += InjuryJumperPenalty;
                player.currentStats.finish += InjuryFinishPenalty;
                player.currentStats.motor += InjuryMotorPenalty;
                player.injuryPenaltyApplied = false;
            }

            player.isInjured = false;
            player.currentInjury = null;
            Debug.Log($"[Injury] {player.data?.displayName} has recovered from injury.");
        }

        /// <summary>Tick injury duration counter after each match; returns true if injury cleared naturally.</summary>
        public static bool TickInjury(PlayerRuntime player)
        {
            if (player == null || !player.isInjured) return false;
            if (player.currentInjury == null)
            {
                ClearInjury(player);
                return true;
            }

            player.currentInjury.matchDuration--;
            if (player.currentInjury.matchDuration <= 0)
            {
                ClearInjury(player);
                return true;
            }

            return false;
        }
    }
}
