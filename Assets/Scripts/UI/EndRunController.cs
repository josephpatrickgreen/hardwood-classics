using System.Collections.Generic;
using ChainNet.Core;
using ChainNet.Data;
using ChainNet.Gameplay;
using ChainNet.Save;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ChainNet.UI
{
    /// <summary>
    /// End-of-run screen. Shown when the player beats the Boss Court.
    /// Displays unlocks, final stats, and offers a New Run or Main Menu option.
    /// </summary>
    public class EndRunController : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private GameObject endRunPanel;
        [SerializeField] private TextMeshProUGUI resultText;
        [SerializeField] private TextMeshProUGUI unlocksText;
        [SerializeField] private TextMeshProUGUI finalStatsText;
        [SerializeField] private Button newRunButton;
        [SerializeField] private Button mainMenuButton;

        [Header("References")]
        [SerializeField] private List<CharacterData> allUnlockableCharacters;
        [SerializeField] private List<TrinketData> allUnlockableTrinkets;

        private void Start()
        {
            Basketball.MatchManager.OnMatchEnd += OnMatchEnd;
            if (endRunPanel != null) endRunPanel.SetActive(false);

            if (newRunButton != null) newRunButton.onClick.AddListener(OnNewRun);
            if (mainMenuButton != null) mainMenuButton.onClick.AddListener(OnMainMenu);
        }

        private void OnDestroy()
        {
            Basketball.MatchManager.OnMatchEnd -= OnMatchEnd;
        }

        private void OnMatchEnd(bool playerWon)
        {
            if (MatchContext.Instance == null || !MatchContext.Instance.IsBossMatch) return;

            if (playerWon)
                ShowVictory();
            else
                ShowDefeat();
        }

        private void ShowVictory()
        {
            if (endRunPanel != null) endRunPanel.SetActive(true);
            if (resultText != null) resultText.text = "RUN COMPLETE\nYou ran the circuit.";

            var unlockMgr = FindFirstObjectByType<UnlockManager>();
            if (unlocksText != null && unlockMgr != null)
            {
                var sb = "Unlocked:\n";
                foreach (var id in unlockMgr.State.unlockedCharacters)
                    sb += $"• {ResolveCharacterName(id)}\n";
                foreach (var id in unlockMgr.State.unlockedTrinkets)
                    sb += $"• {ResolveTrinketName(id)}\n";
                unlocksText.text = sb;
            }

            var run = RunManager.Instance?.CurrentRun;
            if (finalStatsText != null && run != null)
            {
                var sb = $"Cash earned: {run.cash}\n";
                foreach (var p in run.playerTeam.players)
                    sb += $"{p.data?.displayName ?? "Player"} — Lv.{p.level}, {p.xp} XP\n";
                finalStatsText.text = sb;
            }
        }

        private void ShowDefeat()
        {
            if (endRunPanel != null) endRunPanel.SetActive(true);
            if (resultText != null) resultText.text = "RUN OVER\nLost to the boss.";
            if (unlocksText != null) unlocksText.text = "";
            if (finalStatsText != null) finalStatsText.text = "";
        }

        private void OnNewRun()
        {
            MatchContext.Instance?.Clear();
            RunManager.Instance?.StartCircuitRun(RunManager.Instance.CurrentRun?.playerTeam?.data);
            UnityEngine.SceneManagement.SceneManager.LoadScene("CircuitMap");
        }

        private void OnMainMenu()
        {
            MatchContext.Instance?.Clear();
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }

        private string ResolveCharacterName(string id)
        {
            if (allUnlockableCharacters == null) return id;
            foreach (var c in allUnlockableCharacters)
                if (c.characterId == id) return c.displayName;
            return id;
        }

        private string ResolveTrinketName(string id)
        {
            if (allUnlockableTrinkets == null) return id;
            foreach (var t in allUnlockableTrinkets)
                if (t.trinketId == id) return t.displayName;
            return id;
        }
    }
}
