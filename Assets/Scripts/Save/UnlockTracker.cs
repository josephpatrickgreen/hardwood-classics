using System.Collections.Generic;
using ChainNet.Core;
using ChainNet.Data;
using ChainNet.Gameplay;
using ChainNet.Save;
using UnityEngine;

namespace ChainNet.Save
{
    /// <summary>
    /// Tracks and persists run-wide unlock conditions.
    /// Checks completed match stats against known unlock criteria.
    /// </summary>
    public class UnlockTracker : MonoBehaviour
    {
        [SerializeField] private List<CharacterData> unlockableCharacters;
        [SerializeField] private List<TrinketData> unlockableTrinkets;

        private UnlockManager unlockManager;

        // ── Per-match tracking counters ────────────────────────────────────────
        public int JumpersThisMatch { get; private set; }
        public int SpecialsThisMatch { get; private set; }
        public bool CalledFoulThisMatch { get; private set; }
        public bool WonFightThisMatch { get; private set; }

        private void Awake()
        {
            unlockManager = FindFirstObjectByType<UnlockManager>();
            unlockManager?.Load();
        }

        public void ResetMatchCounters()
        {
            JumpersThisMatch = 0;
            SpecialsThisMatch = 0;
            CalledFoulThisMatch = false;
            WonFightThisMatch = false;
        }

        public void RecordJumper() => JumpersThisMatch++;
        public void RecordSpecial() => SpecialsThisMatch++;
        public void RecordFoulCalled() => CalledFoulThisMatch = true;
        public void RecordFightWon() => WonFightThisMatch = true;

        /// <summary>Check all unlock conditions after a won match against the given team.</summary>
        public void EvaluateUnlocks(string defeatedTeamId)
        {
            if (unlockManager == null || unlockableCharacters == null) return;

            foreach (var character in unlockableCharacters)
            {
                if (character.unlockCondition == null) continue;
                if (IsUnlockMet(character.unlockCondition, defeatedTeamId))
                {
                    if (!unlockManager.State.unlockedCharacters.Contains(character.characterId))
                    {
                        unlockManager.State.unlockedCharacters.Add(character.characterId);
                        Debug.Log($"[Unlock] Character unlocked: {character.displayName}");
                    }
                }
            }

            foreach (var trinket in unlockableTrinkets)
            {
                if (!unlockManager.State.unlockedTrinkets.Contains(trinket.trinketId))
                {
                    unlockManager.State.unlockedTrinkets.Add(trinket.trinketId);
                    Debug.Log($"[Unlock] Trinket unlocked: {trinket.displayName}");
                }
            }

            if (!string.IsNullOrEmpty(defeatedTeamId) &&
                !unlockManager.State.unlockedTeams.Contains(defeatedTeamId))
            {
                unlockManager.State.unlockedTeams.Add(defeatedTeamId);
            }

            unlockManager.Save();
        }

        private bool IsUnlockMet(UnlockCondition condition, string defeatedTeamId)
        {
            return condition.conditionType switch
            {
                UnlockConditionType.BeatTeam => defeatedTeamId == condition.targetId,
                UnlockConditionType.BeatBoss => defeatedTeamId == condition.targetId,
                UnlockConditionType.MakeJumpersAgainstTeam =>
                    defeatedTeamId == condition.targetId && JumpersThisMatch >= condition.requiredAmount,
                UnlockConditionType.UseSpecialsInMatch => SpecialsThisMatch >= condition.requiredAmount,
                UnlockConditionType.WinWithoutCallingFoul => !CalledFoulThisMatch,
                UnlockConditionType.WinFightAgainstTeam =>
                    defeatedTeamId == condition.targetId && WonFightThisMatch,
                _ => false
            };
        }
    }
}
