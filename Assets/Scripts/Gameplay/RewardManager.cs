using System;
using System.Collections.Generic;
using ChainNet.Core;
using ChainNet.Data;
using ChainNet.Gameplay;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ChainNet.Gameplay
{
    /// <summary>
    /// Drives the post-match reward screen. Generates reward choices, applies XP and level-ups,
    /// then returns to map.
    /// </summary>
    public class RewardManager : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private GameObject rewardPanel;
        [SerializeField] private TextMeshProUGUI xpGainedText;
        [SerializeField] private TextMeshProUGUI cashGainedText;
        [SerializeField] private TextMeshProUGUI unlockProgressText;
        [SerializeField] private Transform choiceContainer;
        [SerializeField] private GameObject choiceButtonPrefab;

        [Header("Trinket pool for rewards")]
        [SerializeField] private List<TrinketData> trinketPool;

        // ── Constants ─────────────────────────────────────────────────────────
        private const int BaseXPPerWin = 50;
        private const int XPPerLevel = 100;
        private const int BaseCashPerWin = 20;
        private static readonly int[] StatUpgradeOptions = { 2, 3 };

        private Basketball.MatchManager matchManager;

        // ─────────────────────────────────────────────────────────────────────
        private void Start()
        {
            matchManager = FindFirstObjectByType<Basketball.MatchManager>();
            Basketball.MatchManager.OnMatchEnd += OnMatchEnded;
            if (rewardPanel != null) rewardPanel.SetActive(false);
        }

        private void OnDestroy()
        {
            Basketball.MatchManager.OnMatchEnd -= OnMatchEnded;
        }

        private void OnMatchEnded(bool playerWon)
        {
            if (!playerWon) { ReturnToMap(); return; }
            ShowRewardScreen();
        }

        // ── Reward screen ──────────────────────────────────────────────────────
        private void ShowRewardScreen()
        {
            if (rewardPanel != null) rewardPanel.SetActive(true);

            var run = RunManager.Instance?.CurrentRun;
            if (run == null) { ReturnToMap(); return; }

            var xp = BaseXPPerWin;
            var cash = BaseCashPerWin;

            // Grant XP to all players
            foreach (var player in run.playerTeam.players)
            {
                player.xp += xp;
                CheckLevelUp(player);
            }

            run.cash += cash;

            if (xpGainedText != null) xpGainedText.text = $"+{xp} XP";
            if (cashGainedText != null) cashGainedText.text = $"+{cash} $";

            if (unlockProgressText != null)
                unlockProgressText.text = "";

            GenerateChoices(run);
        }

        private void GenerateChoices(RunState run)
        {
            ClearChoices();
            var choices = BuildRewardChoices(run);

            foreach (var choice in choices)
            {
                var captured = choice;
                AddChoiceButton(choice.displayName, () =>
                {
                    ApplyReward(captured, run);
                    ClearChoices();
                    if (rewardPanel != null) rewardPanel.SetActive(false);
                    ReturnToMap();
                });
            }
        }

        private List<RewardData> BuildRewardChoices(RunState run)
        {
            var choices = new List<RewardData>();

            // Choice 1: Trinket (if pool available)
            if (trinketPool != null && trinketPool.Count > 0)
            {
                var trinket = trinketPool[UnityEngine.Random.Range(0, trinketPool.Count)];
                choices.Add(new RewardData
                {
                    rewardId = "trinket",
                    displayName = $"Equip: {trinket.displayName}",
                    trinket = trinket
                });
            }

            // Choice 2: Stat upgrade for random player
            if (run.playerTeam?.players.Count > 0)
            {
                var player = run.playerTeam.players[UnityEngine.Random.Range(0, run.playerTeam.players.Count)];
                var amount = StatUpgradeOptions[UnityEngine.Random.Range(0, StatUpgradeOptions.Length)];
                choices.Add(new RewardData
                {
                    rewardId = $"stat_upgrade_{player.data?.characterId}",
                    displayName = $"Level up {player.data?.displayName ?? "Player"} (+{amount} Jumper)",
                    xp = amount  // xp field stores the boost amount for this reward type
                });
            }

            // Choice 3: Heal
            choices.Add(new RewardData
            {
                rewardId = "heal",
                displayName = "Heal team (+25% Stamina)",
                healTeam = true
            });

            return choices;
        }

        private void ApplyReward(RewardData reward, RunState run)
        {
            if (reward.healTeam)
            {
                foreach (var p in run.playerTeam.players)
                    p.stamina = Mathf.Min(100f, p.stamina + 25f);
            }

            if (reward.trinket != null && run.playerTeam?.players.Count > 0)
            {
                var tm = FindFirstObjectByType<Trinkets.TrinketManager>();
                tm?.EquipTrinket(run.playerTeam.players[0], reward.trinket);
            }

            if (reward.rewardId.StartsWith("stat_upgrade_"))
            {
                const string prefix = "stat_upgrade_";
                var characterId = reward.rewardId.Length > prefix.Length
                    ? reward.rewardId[prefix.Length..]
                    : string.Empty;
                var amount = reward.xp > 0 ? reward.xp : 2;
                foreach (var p in run.playerTeam.players)
                {
                    if (!string.IsNullOrEmpty(characterId) && p.data?.characterId == characterId)
                    {
                        p.currentStats.jumper += amount;
                        break;
                    }
                }
            }
        }

        // ── XP / Leveling ──────────────────────────────────────────────────────
        private static void CheckLevelUp(PlayerRuntime player)
        {
            while (player.xp >= player.level * XPPerLevel)
            {
                player.xp -= player.level * XPPerLevel;
                player.level++;
                ApplyLevelUpStatBoost(player);
                Debug.Log($"{player.data?.displayName} leveled up to {player.level}!");
            }
        }

        private static void ApplyLevelUpStatBoost(PlayerRuntime player)
        {
            // Archetype-specific boosts
            if (player.data == null) return;
            switch (player.data.archetype)
            {
                case CharacterArchetype.HandleGod:
                    player.currentStats.handle++;
                    player.currentStats.vision++;
                    break;
                case CharacterArchetype.RimWrecker:
                case CharacterArchetype.PlaygroundBig:
                    player.currentStats.finish++;
                    player.currentStats.boards++;
                    break;
                case CharacterArchetype.MidrangeKiller:
                case CharacterArchetype.NoLookProphet:
                    player.currentStats.jumper++;
                    player.currentStats.nerve++;
                    break;
                case CharacterArchetype.Lockdown:
                    player.currentStats.clamps++;
                    player.currentStats.swipe++;
                    break;
                case CharacterArchetype.Enforcer:
                    player.currentStats.edge++;
                    player.currentStats.frame++;
                    break;
                default:
                    player.currentStats.motor++;
                    break;
            }
        }

        // ── UI helpers ────────────────────────────────────────────────────────
        private void AddChoiceButton(string label, Action onClick)
        {
            if (choiceContainer == null) return;

            GameObject btn;
            if (choiceButtonPrefab != null)
                btn = Instantiate(choiceButtonPrefab, choiceContainer);
            else
            {
                btn = new GameObject(label);
                btn.transform.SetParent(choiceContainer, false);
                btn.AddComponent<Image>().color = new Color(0.15f, 0.15f, 0.25f);
                var lbl = new GameObject("Label");
                lbl.transform.SetParent(btn.transform, false);
                var txt = lbl.AddComponent<TextMeshProUGUI>();
                txt.text = label;
                txt.fontSize = 14f;
                txt.alignment = TextAlignmentOptions.Center;
                var lrt = lbl.GetComponent<RectTransform>();
                lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
                lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
            }

            var labelTmp = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (labelTmp != null) labelTmp.text = label;

            var button = btn.GetComponent<Button>() ?? btn.AddComponent<Button>();
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClick());
        }

        private void ClearChoices()
        {
            if (choiceContainer == null) return;
            for (var i = choiceContainer.childCount - 1; i >= 0; i--)
                Destroy(choiceContainer.GetChild(i).gameObject);
        }

        private static void ReturnToMap()
        {
            MatchContext.Instance?.Clear();
            UnityEngine.SceneManagement.SceneManager.LoadScene("CircuitMap");
        }
    }
}
