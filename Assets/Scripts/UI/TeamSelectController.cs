using System.Collections.Generic;
using ChainNet.Core;
using ChainNet.Data;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ChainNet.UI
{
    /// <summary>
    /// Pre-run team selection screen. Shows each available team with their roster summary
    /// and lets the player pick one before the run begins.
    /// Wire the "Start" button in MainMenuController to call ShowTeamSelect() instead of
    /// going directly to CircuitMap.
    /// </summary>
    public class TeamSelectController : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject teamSelectPanel;
        [SerializeField] private Transform teamListContainer;
        [SerializeField] private GameObject teamCardPrefab;

        [Header("Detail pane")]
        [SerializeField] private TextMeshProUGUI teamNameText;
        [SerializeField] private TextMeshProUGUI teamDescText;
        [SerializeField] private TextMeshProUGUI rosterSummaryText;
        [SerializeField] private TextMeshProUGUI passiveText;
        [SerializeField] private Button confirmButton;

        [Header("Available teams")]
        [SerializeField] private List<TeamData> availableTeams;

        private TeamData selectedTeam;

        private void Awake()
        {
            if (teamSelectPanel != null) teamSelectPanel.SetActive(false);
            if (confirmButton != null)
            {
                confirmButton.onClick.AddListener(OnConfirm);
                confirmButton.interactable = false;
            }
        }

        public void ShowTeamSelect()
        {
            if (teamSelectPanel != null) teamSelectPanel.SetActive(true);
            selectedTeam = null;
            if (confirmButton != null) confirmButton.interactable = false;
            ClearTeamList();
            PopulateTeamList();
            ClearDetailPane();
        }

        public void HideTeamSelect()
        {
            if (teamSelectPanel != null) teamSelectPanel.SetActive(false);
        }

        // ── Team list ─────────────────────────────────────────────────────────
        private void PopulateTeamList()
        {
            if (availableTeams == null) return;

            foreach (var team in availableTeams)
            {
                var capturedTeam = team;
                GameObject card;

                if (teamCardPrefab != null)
                {
                    card = Instantiate(teamCardPrefab, teamListContainer);
                }
                else
                {
                    card = new GameObject(team.teamId);
                    card.transform.SetParent(teamListContainer, false);
                    card.AddComponent<Image>().color = new Color(0.15f, 0.15f, 0.25f);
                    var rt = card.GetComponent<RectTransform>();
                    rt.sizeDelta = new Vector2(220f, 60f);

                    var lbl = new GameObject("Label");
                    lbl.transform.SetParent(card.transform, false);
                    var txt = lbl.AddComponent<TextMeshProUGUI>();
                    txt.fontSize = 14f;
                    txt.alignment = TextAlignmentOptions.Center;
                    var lrt = lbl.GetComponent<RectTransform>();
                    lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
                    lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
                }

                var labelTmp = card.GetComponentInChildren<TextMeshProUGUI>();
                if (labelTmp != null) labelTmp.text = team.displayName;

                var btn = card.GetComponent<Button>() ?? card.AddComponent<Button>();
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => SelectTeam(capturedTeam));
            }
        }

        private void SelectTeam(TeamData team)
        {
            selectedTeam = team;
            PopulateDetailPane(team);
            if (confirmButton != null) confirmButton.interactable = true;
        }

        // ── Detail pane ───────────────────────────────────────────────────────
        private void PopulateDetailPane(TeamData team)
        {
            if (teamNameText != null) teamNameText.text = team.displayName;
            if (teamDescText != null) teamDescText.text = team.description;

            if (passiveText != null)
                passiveText.text = team.passive != null
                    ? $"{team.passive.displayName}: {team.passive.description}"
                    : "";

            if (rosterSummaryText != null)
            {
                var sb = "";
                foreach (var character in team.roster)
                {
                    if (character == null) continue;
                    sb += $"• {character.displayName}";
                    if (!string.IsNullOrEmpty(character.nickname))
                        sb += $"  \"{character.nickname}\"";
                    sb += $"  [{character.archetype}]\n";
                    if (character.special != null)
                        sb += $"   Special: {character.special.displayName}\n";
                }
                rosterSummaryText.text = sb;
            }
        }

        private void ClearDetailPane()
        {
            if (teamNameText != null) teamNameText.text = "";
            if (teamDescText != null) teamDescText.text = "";
            if (passiveText != null) passiveText.text = "";
            if (rosterSummaryText != null) rosterSummaryText.text = "";
        }

        // ── Confirm ───────────────────────────────────────────────────────────
        private void OnConfirm()
        {
            if (selectedTeam == null) return;

            HideTeamSelect();
            RunManager.Instance?.StartCircuitRun(selectedTeam);
            UnityEngine.SceneManagement.SceneManager.LoadScene("CircuitMap");
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        private void ClearTeamList()
        {
            if (teamListContainer == null) return;
            for (var i = teamListContainer.childCount - 1; i >= 0; i--)
                Destroy(teamListContainer.GetChild(i).gameObject);
        }
    }
}
