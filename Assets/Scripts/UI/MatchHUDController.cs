using ChainNet.Basketball;
using ChainNet.Core;
using ChainNet.Gameplay;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ChainNet.UI
{
    /// <summary>
    /// Drives HUD during a match: score, hype/heat meters, possession, special cooldowns,
    /// foul-call prompt, and stamina bars.
    /// Subscribes to static events rather than polling.
    /// </summary>
    public class MatchHUDController : MonoBehaviour
    {
        [Header("Score")]
        [SerializeField] private TextMeshProUGUI playerScoreText;
        [SerializeField] private TextMeshProUGUI enemyScoreText;
        [SerializeField] private TextMeshProUGUI targetScoreText;
        [Header("Possession")]
        [SerializeField] private GameObject playerPossessionIndicator;
        [SerializeField] private GameObject enemyPossessionIndicator;
        [Header("Hype meter")]
        [SerializeField] private Slider playerHypeSlider;
        [SerializeField] private Slider enemyHypeSlider;
        [SerializeField] private TextMeshProUGUI playerHypeLevelText;
        [Header("Heat meter")]
        [SerializeField] private Slider heatSlider;
        [SerializeField] private Image heatFill;
        [SerializeField] private Color coolColor = Color.blue;
        [SerializeField] private Color boilingColor = Color.red;
        [Header("Foul prompt")]
        [SerializeField] private GameObject foulPrompt;
        [SerializeField] private TextMeshProUGUI foulPromptText;
        [Header("Special cooldown icons")]
        [SerializeField] private GameObject[] specialCooldownIcons;
        [SerializeField] private Image[] specialCooldownFills;
        [Header("Stamina")]
        [SerializeField] private Slider[] playerStaminaSliders;
        // ── Runtime refs set by MatchBootstrapper ──────────────────────────────
        private Basketball.MatchManager matchManager;
        private Events.HypeManager hypeManager;
        private Events.HeatManager heatManager;
        private Events.FoulManager foulManager;
        // ─────────────────────────────────────────────────────────────────────
        private void Start()
        {
            matchManager = FindFirstObjectByType<Basketball.MatchManager>();
            hypeManager = FindFirstObjectByType<Events.HypeManager>();
            heatManager = FindFirstObjectByType<Events.HeatManager>();
            foulManager = FindFirstObjectByType<Events.FoulManager>();
            MatchManager.OnScore += OnScore;
            MatchManager.OnMatchEnd += OnMatchEnd;
            PossessionManager.OnPossessionChanged += OnPossessionChanged;
            UIManager.OnHeatChanged += OnHeatChanged;
            UIManager.OnHypeChanged += OnHypeChanged;
            if (targetScoreText != null && matchManager != null)
                targetScoreText.text = $"First to {matchManager.targetScore}";
            if (foulPrompt != null) foulPrompt.SetActive(false);
        }
        private void OnDestroy()
            MatchManager.OnScore -= OnScore;
            MatchManager.OnMatchEnd -= OnMatchEnd;
            PossessionManager.OnPossessionChanged -= OnPossessionChanged;
            UIManager.OnHeatChanged -= OnHeatChanged;
            UIManager.OnHypeChanged -= OnHypeChanged;
        private void Update()
            UpdateStaminaBars();
            UpdateSpecialCooldowns();
            UpdateFoulPrompt();
        // ── Event handlers ────────────────────────────────────────────────────
        private void OnScore(TeamRuntime team)
            if (matchManager == null) return;
            if (playerScoreText != null)
                playerScoreText.text = matchManager.playerScore.ToString();
            if (enemyScoreText != null)
                enemyScoreText.text = matchManager.enemyScore.ToString();
        private void OnMatchEnd(bool playerWon)
            // Handled by RewardManager
        private void OnPossessionChanged(TeamRuntime team)
            var isPlayer = team == matchManager.playerTeam;
            if (playerPossessionIndicator != null) playerPossessionIndicator.SetActive(isPlayer);
            if (enemyPossessionIndicator != null) enemyPossessionIndicator.SetActive(!isPlayer);
        private void OnHeatChanged(float heat)
            if (heatSlider != null)
            {
                heatSlider.value = heat / 100f;
                if (heatFill != null)
                    heatFill.color = Color.Lerp(coolColor, boilingColor, heat / 100f);
            }
        private void OnHypeChanged(float hype)
            // UIManager raises this with player hype for now
            if (playerHypeSlider != null)
                playerHypeSlider.value = hype / 100f;
            if (hypeManager != null && matchManager != null && playerHypeLevelText != null)
                var level = hypeManager.GetHypeLevel(matchManager.playerTeam);
                playerHypeLevelText.text = level.ToString();
        // ── Per-frame updates ─────────────────────────────────────────────────
        private void UpdateStaminaBars()
            var run = Core.RunManager.Instance?.CurrentRun;
            if (run == null || playerStaminaSliders == null) return;
            for (var i = 0; i < playerStaminaSliders.Length && i < run.playerTeam.players.Count; i++)
                if (playerStaminaSliders[i] != null)
                    playerStaminaSliders[i].value = run.playerTeam.players[i].stamina / 100f;
        private void UpdateSpecialCooldowns()
            if (run == null || specialCooldownFills == null) return;
            for (var i = 0; i < specialCooldownFills.Length && i < run.playerTeam.players.Count; i++)
                if (specialCooldownFills[i] == null) continue;
                var p = run.playerTeam.players[i];
                var maxCd = p.data?.special?.cooldownSeconds ?? 1f;
                if (maxCd <= 0f) continue;
                specialCooldownFills[i].fillAmount = 1f - (p.specialCooldownRemaining / maxCd);
        private void UpdateFoulPrompt()
            // Show foul prompt when an opportunity exists (foulManager exposes it via reflection isn't ideal;
            // for prototype we just pulse the prompt on contact events by subscribing to a public event)
        /// <summary>Call this when a foul opportunity becomes available to show the prompt.</summary>
        public void ShowFoulPrompt(float timeRemaining)
            if (foulPrompt == null) return;
            foulPrompt.SetActive(true);
            if (foulPromptText != null)
                foulPromptText.text = $"CALL FOUL? [F]  ({timeRemaining:F1}s)";
        public void HideFoulPrompt()
    }
}
