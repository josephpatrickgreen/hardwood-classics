using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ChainNet.UI
{
    /// <summary>
    /// Options / Settings screen shown from the Main Menu.
    /// Persists volume preferences in PlayerPrefs and displays the in-game controls reference.
    /// </summary>
    public class OptionsController : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject optionsPanel;
        [SerializeField] private Button closeButton;

        [Header("Volume")]
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;
        [SerializeField] private TextMeshProUGUI masterVolumeLabel;
        [SerializeField] private TextMeshProUGUI musicVolumeLabel;
        [SerializeField] private TextMeshProUGUI sfxVolumeLabel;

        [Header("Controls reference")]
        [SerializeField] private TextMeshProUGUI controlsText;

        // ── PlayerPrefs keys ──────────────────────────────────────────────────
        private const string KeyMaster = "opt_vol_master";
        private const string KeyMusic  = "opt_vol_music";
        private const string KeySfx    = "opt_vol_sfx";

        // ── Controls reference text ───────────────────────────────────────────
        private const string ControlsReference =
            "MOVEMENT      WASD\n" +
            "SPRINT        Left Shift  (drains stamina)\n" +
            "SHOOT         Space\n" +
            "PASS          E\n" +
            "DRIBBLE MOVE  Q\n" +
            "SPECIAL       R\n" +
            "STEAL         C   (hold Left Ctrl = dirty steal)\n" +
            "BLOCK         V\n" +
            "CALL FOUL     F";

        // ── Lifecycle ─────────────────────────────────────────────────────────
        private void Awake()
        {
            if (optionsPanel != null) optionsPanel.SetActive(false);
            if (closeButton != null) closeButton.onClick.AddListener(Hide);
        }

        private void Start()
        {
            if (controlsText != null) controlsText.text = ControlsReference;
            LoadVolumes();
            WireSliders();
        }

        // ── Public API ────────────────────────────────────────────────────────
        public void Show()
        {
            if (optionsPanel != null) optionsPanel.SetActive(true);
            LoadVolumes();
        }

        public void Hide()
        {
            SaveVolumes();
            if (optionsPanel != null) optionsPanel.SetActive(false);
        }

        // ── Volume helpers ────────────────────────────────────────────────────
        private void LoadVolumes()
        {
            var master = PlayerPrefs.GetFloat(KeyMaster, 1f);
            var music  = PlayerPrefs.GetFloat(KeyMusic,  0.8f);
            var sfx    = PlayerPrefs.GetFloat(KeySfx,    1f);

            AudioListener.volume = master;

            if (masterVolumeSlider != null) masterVolumeSlider.value = master;
            if (musicVolumeSlider  != null) musicVolumeSlider.value  = music;
            if (sfxVolumeSlider    != null) sfxVolumeSlider.value    = sfx;

            UpdateLabels(master, music, sfx);
        }

        private void WireSliders()
        {
            if (masterVolumeSlider != null)
                masterVolumeSlider.onValueChanged.AddListener(v =>
                {
                    AudioListener.volume = v;
                    if (masterVolumeLabel != null)
                        masterVolumeLabel.text = $"Master  {Mathf.RoundToInt(v * 100)}%";
                });

            if (musicVolumeSlider != null)
                musicVolumeSlider.onValueChanged.AddListener(v =>
                {
                    if (musicVolumeLabel != null)
                        musicVolumeLabel.text = $"Music  {Mathf.RoundToInt(v * 100)}%";
                });

            if (sfxVolumeSlider != null)
                sfxVolumeSlider.onValueChanged.AddListener(v =>
                {
                    if (sfxVolumeLabel != null)
                        sfxVolumeLabel.text = $"SFX  {Mathf.RoundToInt(v * 100)}%";
                });
        }

        private void SaveVolumes()
        {
            if (masterVolumeSlider != null) PlayerPrefs.SetFloat(KeyMaster, masterVolumeSlider.value);
            if (musicVolumeSlider  != null) PlayerPrefs.SetFloat(KeyMusic,  musicVolumeSlider.value);
            if (sfxVolumeSlider    != null) PlayerPrefs.SetFloat(KeySfx,    sfxVolumeSlider.value);
            PlayerPrefs.Save();
        }

        private void UpdateLabels(float master, float music, float sfx)
        {
            if (masterVolumeLabel != null) masterVolumeLabel.text = $"Master  {Mathf.RoundToInt(master * 100)}%";
            if (musicVolumeLabel  != null) musicVolumeLabel.text  = $"Music  {Mathf.RoundToInt(music * 100)}%";
            if (sfxVolumeLabel    != null) sfxVolumeLabel.text    = $"SFX  {Mathf.RoundToInt(sfx * 100)}%";
        }
    }
}
