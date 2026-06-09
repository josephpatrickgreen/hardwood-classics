using System.Collections.Generic;
using ChainNet.Core;
using ChainNet.Data;
using ChainNet.Gameplay;
using ChainNet.RoguelikeMap;
using ChainNet.Save;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ChainNet.UI
{
    /// <summary>
    /// Handles resolution of non-match map nodes: Trainer, Shop, Heal, Event, Recruit, etc.
    /// Shows a simple modal UI with context-appropriate choices.
    /// </summary>
    public class NodeEventController : MonoBehaviour
    {
        [Header("Modal Panel")]
        [SerializeField] private GameObject modalPanel;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI bodyText;
        [SerializeField] private Transform choiceButtonContainer;
        [SerializeField] private GameObject choiceButtonPrefab;

        [Header("Trinket pool for shop")]
        [SerializeField] private List<TrinketData> shopTrinketPool;

        private MapNodeRuntime activeNode;
        private CircuitMapController mapController;

        private void Awake()
        {
            mapController = FindFirstObjectByType<CircuitMapController>();
            if (modalPanel != null) modalPanel.SetActive(false);
        }

        public void ShowNodeEvent(MapNodeRuntime node)
        {
            activeNode = node;
            ClearChoices();
            if (modalPanel != null) modalPanel.SetActive(true);

            switch (node.nodeType)
            {
                case MapNodeType.Trainer:
                    ShowTrainer();
                    break;
                case MapNodeType.SneakerShop:
                case MapNodeType.TapeDealer:
                case MapNodeType.CornerStore:
                    ShowShop();
                    break;
                case MapNodeType.OpenGym:
                    ShowOpenGym();
                    break;
                case MapNodeType.MoneyGame:
                    ShowMoneyGame();
                    break;
                case MapNodeType.BarberShop:
                    ShowBarberShop();
                    break;
                case MapNodeType.BackAlley:
                    ShowBackAlley();
                    break;
                default:
                    ShowGenericEvent(node.nodeType.ToString());
                    break;
            }
        }

        // ── Node handlers ──────────────────────────────────────────────────────
        private void ShowTrainer()
        {
            SetTitle("Trainer");
            SetBody("A coach offers to sharpen one of your players. Pick a stat upgrade.");
            var team = RunManager.Instance?.CurrentRun?.playerTeam;
            if (team == null) { CloseModal(); return; }

            foreach (var player in team.players)
            {
                var capturedPlayer = player;
                AddChoice($"Upgrade {player.data?.displayName ?? "Player"} (+2 Jumper)",
                    () =>
                    {
                        capturedPlayer.currentStats.jumper += 2;
                        CloseModal();
                    });
                AddChoice($"Upgrade {player.data?.displayName ?? "Player"} (+2 Clamps)",
                    () =>
                    {
                        capturedPlayer.currentStats.clamps += 2;
                        CloseModal();
                    });
            }

            AddChoice("Pass", CloseModal);
        }

        private void ShowShop()
        {
            SetTitle("Shop");
            SetBody("Choose a trinket to equip.");
            var run = RunManager.Instance?.CurrentRun;
            if (run == null || shopTrinketPool == null || shopTrinketPool.Count == 0)
            {
                SetBody("Nothing in stock.");
                AddChoice("Leave", CloseModal);
                return;
            }

            // Pick 3 random trinkets
            var pool = new List<TrinketData>(shopTrinketPool);
            for (var i = 0; i < Mathf.Min(3, pool.Count); i++)
            {
                var idx = Random.Range(i, pool.Count);
                (pool[i], pool[idx]) = (pool[idx], pool[i]);
                var trinket = pool[i];
                AddChoice($"{trinket.displayName} ({trinket.rarity})",
                    () =>
                    {
                        // Equip to first available player
                        if (run.playerTeam?.players.Count > 0)
                        {
                            var tm = FindFirstObjectByType<Trinkets.TrinketManager>();
                            tm?.EquipTrinket(run.playerTeam.players[0], trinket);
                        }

                        CloseModal();
                    });
            }

            AddChoice("Skip", CloseModal);
        }

        private void ShowOpenGym()
        {
            SetTitle("Open Gym");
            SetBody("Rest and recovery. Your team restores stamina and treats injuries.");
            var team = RunManager.Instance?.CurrentRun?.playerTeam;
            AddChoice("Rest (+25 Stamina all)",
                () =>
                {
                    if (team != null)
                        foreach (var p in team.players)
                            p.stamina = Mathf.Min(100f, p.stamina + 25f);
                    CloseModal();
                });

            // Offer injury recovery if anyone is hurt
            if (team != null)
            {
                foreach (var player in team.players)
                {
                    if (!player.isInjured) continue;
                    var capturedPlayer = player;
                    AddChoice($"Treat {player.data?.displayName ?? "Player"}'s injury",
                        () =>
                        {
                            InjurySystem.ClearInjury(capturedPlayer);
                            CloseModal();
                        });
                }
            }

            AddChoice("Leave", CloseModal);
        }

        private const int MoneyGameMinBet = 15;
        private const int MoneyGameBetStep = 5;
        private const int MoneyGameBetTiers = 3;

        private void ShowMoneyGame()
        {
            SetTitle("Money Game");
            var run = RunManager.Instance?.CurrentRun;
            var betAmount = MoneyGameMinBet + Random.Range(0, MoneyGameBetTiers) * MoneyGameBetStep; // 15, 20, or 25
            SetBody($"A side bet is on the table — ${betAmount}. " +
                    "One of your ballers steps up for a quick shoot-out. Win or lose.");

            if (run == null) { AddChoice("Walk away", CloseModal); return; }

            AddChoice($"Enter the bet (risk ${betAmount})",
                () =>
                {
                    if (run.playerTeam?.players.Count > 0)
                    {
                        // Pick the player with highest jumper+nerve as the shooter
                        var shooter = run.playerTeam.players[0];
                        foreach (var p in run.playerTeam.players)
                        {
                            if (p.currentStats.jumper + p.currentStats.nerve >
                                shooter.currentStats.jumper + shooter.currentStats.nerve)
                                shooter = p;
                        }

                        var chance = 0.35f + shooter.currentStats.jumper * 0.025f +
                                     shooter.currentStats.nerve * 0.015f;
                        var won = Random.value <= chance;
                        var shooterName = shooter.data?.displayName ?? "Your player";

                        if (won)
                        {
                            run.cash += betAmount * 2;
                            SetBody($"{shooterName} drained it. +${betAmount * 2} cash!");
                        }
                        else
                        {
                            run.cash = Mathf.Max(0, run.cash - betAmount);
                            SetBody($"{shooterName} bricked it. -${betAmount} cash.");
                        }

                        SaveManager.Instance?.SaveRun(run);
                    }

                    ClearChoices();
                    AddChoice("Continue", CloseModal);
                });

            AddChoice("Walk away", CloseModal);
        }

        private void ShowBarberShop()
        {
            SetTitle("Barber Shop");
            SetBody("The barber slides you some intel and +2 Swagger.");
            var team = RunManager.Instance?.CurrentRun?.playerTeam;
            AddChoice("Get a cut (+2 Swagger all)",
                () =>
                {
                    if (team != null)
                        foreach (var p in team.players)
                            p.currentStats.swagger += 2;
                    CloseModal();
                });
            AddChoice("Pass", CloseModal);
        }

        private void ShowBackAlley()
        {
            SetTitle("Back Alley");
            SetBody("A shady figure offers to teach your crew some tricks. Edge or risk.");
            var team = RunManager.Instance?.CurrentRun?.playerTeam;
            AddChoice("Learn dirty tricks (+3 Edge, team)",
                () =>
                {
                    if (team != null)
                        foreach (var p in team.players)
                            p.currentStats.edge += 3;
                    CloseModal();
                });
            AddChoice("Challenge to a fight (+5 Frame or Injury)",
                () =>
                {
                    if (team?.players.Count > 0)
                    {
                        var leader = team.players[0];
                        var roll = Random.Range(0, 20) + leader.currentStats.frame;
                        if (roll > 12)
                            leader.currentStats.frame += 5;
                        else
                            leader.isInjured = true;
                    }

                    CloseModal();
                });
            AddChoice("Walk away", CloseModal);
        }

        private void ShowGenericEvent(string nodeTypeName)
        {
            SetTitle(nodeTypeName);
            SetBody("Something happens here.");
            AddChoice("Continue", CloseModal);
        }

        // ── UI helpers ────────────────────────────────────────────────────────
        private void SetTitle(string text)
        {
            if (titleText != null) titleText.text = text;
        }

        private void SetBody(string text)
        {
            if (bodyText != null) bodyText.text = text;
        }

        private void AddChoice(string label, System.Action onClick)
        {
            if (choiceButtonContainer == null) return;

            GameObject btn;
            if (choiceButtonPrefab != null)
            {
                btn = Instantiate(choiceButtonPrefab, choiceButtonContainer);
            }
            else
            {
                btn = new GameObject(label);
                btn.transform.SetParent(choiceButtonContainer, false);
                btn.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f);
                var lbl = new GameObject("Label");
                lbl.transform.SetParent(btn.transform, false);
                var txt = lbl.AddComponent<TextMeshProUGUI>();
                txt.text = label;
                txt.fontSize = 14f;
                txt.alignment = TextAlignmentOptions.Center;
                var lrt = lbl.GetComponent<RectTransform>();
                lrt.anchorMin = Vector2.zero;
                lrt.anchorMax = Vector2.one;
                lrt.offsetMin = Vector2.zero;
                lrt.offsetMax = Vector2.zero;
            }

            var buttonComponent = btn.GetComponent<Button>() ?? btn.AddComponent<Button>();
            buttonComponent.onClick.RemoveAllListeners();
            buttonComponent.onClick.AddListener(() => onClick());

            // Set the label text if using prefab
            var labelText = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (labelText != null) labelText.text = label;
        }

        private void ClearChoices()
        {
            if (choiceButtonContainer == null) return;
            for (var i = choiceButtonContainer.childCount - 1; i >= 0; i--)
                Destroy(choiceButtonContainer.GetChild(i).gameObject);
        }

        private void CloseModal()
        {
            if (modalPanel != null) modalPanel.SetActive(false);
            mapController?.RefreshNodeVisuals();
        }
    }
}
