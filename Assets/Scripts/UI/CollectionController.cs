using System.Collections.Generic;
using ChainNet.Data;
using ChainNet.Save;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ChainNet.UI
{
    /// <summary>
    /// Collection screen shown from the Main Menu. Displays every character and trinket
    /// in the roster, greying out anything that is still locked.
    /// </summary>
    public class CollectionController : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject collectionPanel;
        [SerializeField] private Button closeButton;

        [Header("Character list")]
        [SerializeField] private Transform characterListContainer;
        [SerializeField] private TextMeshProUGUI characterHeaderText;

        [Header("Trinket list")]
        [SerializeField] private Transform trinketListContainer;
        [SerializeField] private TextMeshProUGUI trinketHeaderText;

        [Header("All unlockable content")]
        [SerializeField] private List<CharacterData> allCharacters;
        [SerializeField] private List<TrinketData> allTrinkets;

        // ── Colours ───────────────────────────────────────────────────────────
        private static readonly Color UnlockedBg  = new(0.10f, 0.15f, 0.25f);
        private static readonly Color LockedBg    = new(0.07f, 0.07f, 0.09f);
        private static readonly Color UnlockedText = Color.white;
        private static readonly Color LockedText  = new(0.45f, 0.45f, 0.45f);
        private static readonly Color SubText     = new(0.70f, 0.70f, 0.70f);

        // ─────────────────────────────────────────────────────────────────────
        private void Awake()
        {
            if (collectionPanel != null) collectionPanel.SetActive(false);
            if (closeButton != null) closeButton.onClick.AddListener(Hide);
        }

        public void Show()
        {
            if (collectionPanel != null) collectionPanel.SetActive(true);
            Populate();
        }

        public void Hide()
        {
            if (collectionPanel != null) collectionPanel.SetActive(false);
        }

        // ── Population ────────────────────────────────────────────────────────
        private void Populate()
        {
            var unlockMgr = FindFirstObjectByType<UnlockManager>();
            unlockMgr?.Load();

            PopulateCharacters(unlockMgr);
            PopulateTrinkets(unlockMgr);
        }

        private void PopulateCharacters(UnlockManager unlockMgr)
        {
            ClearContainer(characterListContainer);
            if (allCharacters == null) return;

            var total    = allCharacters.Count;
            var unlocked = 0;

            foreach (var c in allCharacters)
            {
                var isUnlocked = IsCharacterUnlocked(c, unlockMgr);
                if (isUnlocked) unlocked++;

                var title    = c.displayName + (string.IsNullOrEmpty(c.nickname) ? "" : $"  \"{c.nickname}\"");
                var subtitle = $"[{c.archetype}]" + (c.special != null ? $"  —  {c.special.displayName}" : "");
                BuildEntry(characterListContainer, title, subtitle, isUnlocked);
            }

            if (characterHeaderText != null)
                characterHeaderText.text = $"Roster  {unlocked}/{total}";
        }

        private void PopulateTrinkets(UnlockManager unlockMgr)
        {
            ClearContainer(trinketListContainer);
            if (allTrinkets == null) return;

            var total    = allTrinkets.Count;
            var unlocked = 0;

            foreach (var t in allTrinkets)
            {
                var isUnlocked = unlockMgr == null
                    || unlockMgr.State.unlockedTrinkets.Contains(t.trinketId);
                if (isUnlocked) unlocked++;

                var subtitle = $"[{t.rarity}]  {t.description}";
                BuildEntry(trinketListContainer, t.displayName, subtitle, isUnlocked);
            }

            if (trinketHeaderText != null)
                trinketHeaderText.text = $"Trinkets  {unlocked}/{total}";
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        private static bool IsCharacterUnlocked(CharacterData c, UnlockManager unlockMgr)
        {
            if (c.unlockCondition == null) return true;
            if (unlockMgr == null) return false;
            return unlockMgr.IsUnlocked(c.unlockCondition)
                || unlockMgr.State.unlockedCharacters.Contains(c.characterId);
        }

        private static void BuildEntry(Transform container, string title, string subtitle, bool unlocked)
        {
            if (container == null) return;

            // Root entry
            var entry = new GameObject(title);
            entry.transform.SetParent(container, false);
            var rt = entry.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0f, 58f);
            entry.AddComponent<Image>().color = unlocked ? UnlockedBg : LockedBg;

            // Title label
            var titleGo  = new GameObject("Title");
            titleGo.transform.SetParent(entry.transform, false);
            var titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
            titleTmp.text      = unlocked ? title : "??? (Locked)";
            titleTmp.fontSize  = 14f;
            titleTmp.color     = unlocked ? UnlockedText : LockedText;
            var trt = titleTmp.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0f, 0.5f); trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(10f, 2f);  trt.offsetMax = new Vector2(-10f, -2f);

            // Subtitle label
            var subGo  = new GameObject("Sub");
            subGo.transform.SetParent(entry.transform, false);
            var subTmp = subGo.AddComponent<TextMeshProUGUI>();
            subTmp.text     = unlocked ? subtitle : "Complete unlock conditions to reveal";
            subTmp.fontSize = 11f;
            subTmp.color    = unlocked ? SubText : LockedText;
            var srt = subTmp.GetComponent<RectTransform>();
            srt.anchorMin = Vector2.zero;              srt.anchorMax = new Vector2(1f, 0.5f);
            srt.offsetMin = new Vector2(10f, 2f);      srt.offsetMax = new Vector2(-10f, -2f);
        }

        private static void ClearContainer(Transform container)
        {
            if (container == null) return;
            for (var i = container.childCount - 1; i >= 0; i--)
                Destroy(container.GetChild(i).gameObject);
        }
    }
}
