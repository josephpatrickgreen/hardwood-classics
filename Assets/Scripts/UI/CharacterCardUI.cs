using ChainNet.Basketball;
using ChainNet.Data;
using ChainNet.Events;
using ChainNet.Gameplay;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ChainNet.UI
{
    /// <summary>
    /// Displays character info panel: portrait, stats, special, trinkets, injury status.
    /// Used on both the map screen and an in-match roster view.
    /// </summary>
    public class CharacterCardUI : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] private Image portrait;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI nicknameText;
        [SerializeField] private TextMeshProUGUI archetypeText;
        [SerializeField] private TextMeshProUGUI levelText;

        [Header("Stats")]
        [SerializeField] private TextMeshProUGUI handleText;
        [SerializeField] private TextMeshProUGUI jumperText;
        [SerializeField] private TextMeshProUGUI finishText;
        [SerializeField] private TextMeshProUGUI bounceText;
        [SerializeField] private TextMeshProUGUI visionsText;
        [SerializeField] private TextMeshProUGUI clampsText;
        [SerializeField] private TextMeshProUGUI edgeText;
        [SerializeField] private TextMeshProUGUI motorText;

        [Header("Special")]
        [SerializeField] private TextMeshProUGUI specialNameText;
        [SerializeField] private TextMeshProUGUI specialDescText;
        [SerializeField] private Slider specialCooldownSlider;

        [Header("Status")]
        [SerializeField] private TextMeshProUGUI injuryText;
        [SerializeField] private Slider staminaSlider;

        [Header("Trinkets")]
        [SerializeField] private TextMeshProUGUI trinketsText;

        // ─────────────────────────────────────────────────────────────────────
        public void Populate(PlayerRuntime player)
        {
            if (player == null) return;
            var data = player.data;

            if (portrait != null && data?.portrait != null) portrait.sprite = data.portrait;
            if (nameText != null) nameText.text = data?.displayName ?? "Unknown";
            if (nicknameText != null) nicknameText.text = data?.nickname != null ? $"\"{data.nickname}\"" : "";
            if (archetypeText != null) archetypeText.text = data?.archetype.ToString() ?? "";
            if (levelText != null) levelText.text = $"Lv.{player.level}";

            var s = player.currentStats;
            SetStat(handleText, "HDL", s.handle);
            SetStat(jumperText, "JMP", s.jumper);
            SetStat(finishText, "FIN", s.finish);
            SetStat(bounceText, "BNC", s.bounce);
            SetStat(visionsText, "VIS", s.vision);
            SetStat(clampsText, "CLM", s.clamps);
            SetStat(edgeText, "EDG", s.edge);
            SetStat(motorText, "MTR", s.motor);

            if (data?.special != null)
            {
                if (specialNameText != null) specialNameText.text = data.special.displayName;
                if (specialDescText != null) specialDescText.text = data.special.description;
            }

            if (specialCooldownSlider != null)
            {
                var maxCd = data?.special?.cooldownSeconds ?? 1f;
                specialCooldownSlider.value = maxCd > 0f
                    ? 1f - (player.specialCooldownRemaining / maxCd)
                    : 1f;
            }

            if (staminaSlider != null) staminaSlider.value = player.stamina / 100f;
            if (injuryText != null)
                injuryText.text = player.isInjured
                    ? $"INJURED: {player.currentInjury?.displayName ?? "Unknown"}"
                    : "";

            if (trinketsText != null)
            {
                var trinketList = "";
                foreach (var t in player.equippedTrinkets)
                    trinketList += $"• {t.displayName}\n";
                trinketsText.text = string.IsNullOrEmpty(trinketList) ? "No trinkets" : trinketList;
            }
        }

        private static void SetStat(TextMeshProUGUI label, string statName, int value)
        {
            if (label != null) label.text = $"{statName}: {value}";
        }
    }
}
