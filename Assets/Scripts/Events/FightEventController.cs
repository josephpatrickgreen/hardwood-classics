using ChainNet.Basketball;
using System;
using System.Collections.Generic;
using ChainNet.Gameplay;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ChainNet.Events
{
    /// <summary>
    /// Triggers when Heat is high and a fight-worthy action happens.
    /// Shows a modal choice panel, resolves via stat check, applies consequences.
    /// </summary>
    public class FightEventController : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private GameObject fightPanel;
        [SerializeField] private TextMeshProUGUI flavorText;
        [SerializeField] private Transform choiceContainer;
        [SerializeField] private GameObject choiceButtonPrefab;

        [Header("References")]
        [SerializeField] private HeatManager heatManager;
        [SerializeField] private HypeManager hypeManager;
        [SerializeField] private Basketball.MatchManager matchManager;

        private PlayerRuntime instigator;
        private PlayerRuntime opponent;

        public static event Action<bool> OnFightResolved; // true = player won

        private void Awake()
        {
            if (fightPanel != null) fightPanel.SetActive(false);
        }

        /// <summary>Try to start a fight sequence. Does nothing if heat is too low.</summary>
        public void TryStartFight(PlayerRuntime player, PlayerRuntime enemy)
        {
            if (heatManager == null || !heatManager.IsBoiling()) return;
            if (player == null || enemy == null) return;

            instigator = player;
            opponent = enemy;
            ShowFightPanel();
        }

        // ── Panel construction ─────────────────────────────────────────────────
        private void ShowFightPanel()
        {
            if (fightPanel != null) fightPanel.SetActive(true);

            var name = instigator.data?.displayName ?? "Someone";
            var enemyName = opponent.data?.displayName ?? "the defender";
            SetFlavor($"{enemyName} squares up on {name}. What do you do?");

            ClearChoices();
            AddChoice("Swing Back", OnSwingBack);
            AddChoice("Let Enforcer Handle It", OnEnforcerHandle);
            AddChoice("Talk Him Down", OnTalkDown);
            AddChoice("Call Foul", OnCallFoul);
            AddChoice("Walk Away", OnWalkAway);
        }

        // ── Choice handlers ───────────────────────────────────────────────────
        private void OnSwingBack()
        {
            var score = instigator.currentStats.edge + instigator.currentStats.frame
                        + UnityEngine.Random.Range(0, 20);
            var enemyScore = opponent.currentStats.edge + opponent.currentStats.frame
                             + UnityEngine.Random.Range(0, 20);
            var won = score >= enemyScore;

            if (won)
            {
                hypeManager?.AddHype(matchManager?.playerTeam, 12f);
                heatManager?.ReduceHeat(20f);
                LogOutcome("Stood your ground. +Hype.");
            }
            else
            {
                instigator.stamina = Mathf.Max(0f, instigator.stamina - 20f);
                hypeManager?.ReduceHype(matchManager?.playerTeam, 8f);
                heatManager?.AddHeat(10f);
                LogOutcome("Came up short. -Stamina, -Hype.");
            }

            ResolveFight(won);
        }

        private void OnEnforcerHandle()
        {
            // Find team enforcer (highest edge+frame)
            var enforcer = FindTeamEnforcer();
            if (enforcer == null) { OnSwingBack(); return; }

            var score = enforcer.currentStats.edge + enforcer.currentStats.frame
                        + UnityEngine.Random.Range(0, 20);
            var enemyScore = opponent.currentStats.edge + opponent.currentStats.frame
                             + UnityEngine.Random.Range(0, 20);
            var won = score >= enemyScore;

            if (won)
            {
                hypeManager?.AddHype(matchManager?.playerTeam, 10f);
                heatManager?.ReduceHeat(25f);
                LogOutcome($"{enforcer.data?.displayName} handled it. +Hype.");
            }
            else
            {
                enforcer.stamina = Mathf.Max(0f, enforcer.stamina - 15f);
                heatManager?.AddHeat(8f);
                LogOutcome($"{enforcer.data?.displayName} got roughed up.");
            }

            ResolveFight(won);
        }

        private void OnTalkDown()
        {
            var cool = instigator.currentStats.cool + instigator.currentStats.swagger;
            var enemyCool = opponent.currentStats.cool + UnityEngine.Random.Range(0, 15);
            var won = cool > enemyCool;

            if (won)
            {
                heatManager?.ReduceHeat(30f);
                LogOutcome("Kept it cool. Heat dropped.");
            }
            else
            {
                heatManager?.AddHeat(5f);
                LogOutcome("Opponent wasn't having it.");
            }

            ResolveFight(won);
        }

        private void OnCallFoul()
        {
            var foulMgr = FindFirstObjectByType<FoulManager>();
            if (foulMgr == null) { ResolveFight(false); return; }

            var court = matchManager?.activeCourt;
            var heat = heatManager != null ? heatManager.currentHeat / 100f * 0.3f : 0f;
            var valid = foulMgr.CallFoul(instigator, opponent, court, heat);
            LogOutcome(valid ? "Ref agreed — foul called." : "Ref waved it off.");
            heatManager?.ReduceHeat(15f);
            ResolveFight(valid);
        }

        private void OnWalkAway()
        {
            heatManager?.ReduceHeat(10f);
            hypeManager?.ReduceHype(matchManager?.playerTeam, 4f);
            LogOutcome("Walked away. Avoided escalation.");
            ResolveFight(false);
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        private void ResolveFight(bool playerWon)
        {
            OnFightResolved?.Invoke(playerWon);
            ClearChoices();
            if (fightPanel != null) fightPanel.SetActive(false);
        }

        private PlayerRuntime FindTeamEnforcer()
        {
            var team = matchManager?.playerTeam;
            if (team == null) return instigator;

            PlayerRuntime best = null;
            var bestScore = -1;
            foreach (var p in team.players)
            {
                var s = p.currentStats.edge + p.currentStats.frame;
                if (s > bestScore) { bestScore = s; best = p; }
            }

            return best;
        }

        private void SetFlavor(string text)
        {
            if (flavorText != null) flavorText.text = text;
        }

        private static void LogOutcome(string msg) => Debug.Log($"[Fight] {msg}");

        private void ClearChoices()
        {
            if (choiceContainer == null) return;
            for (var i = choiceContainer.childCount - 1; i >= 0; i--)
                Destroy(choiceContainer.GetChild(i).gameObject);
        }

        private void AddChoice(string label, Action onClick)
        {
            if (choiceContainer == null) return;

            GameObject btn;
            if (choiceButtonPrefab != null)
                btn = Instantiate(choiceButtonPrefab, choiceContainer);
            else
            {
                btn = new GameObject(label);
                btn.transform.SetParent(choiceContainer, false);
                btn.AddComponent<Image>().color = new Color(0.3f, 0.1f, 0.1f);
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
    }
}
