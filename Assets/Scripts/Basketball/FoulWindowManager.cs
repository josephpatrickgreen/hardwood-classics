using ChainNet.Core;
using ChainNet.Events;
using ChainNet.Gameplay;
using ChainNet.UI;
using UnityEngine;

namespace ChainNet.Basketball
{
    /// <summary>
    /// Manages foul opportunity windows during a match.
    /// Detects contact, opens the window, ticks it down, and exposes it for CallFoul.
    /// </summary>
    public class FoulWindowManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private FoulManager foulManager;
        [SerializeField] private HeatManager heatManager;
        [SerializeField] private MatchHUDController hud;

        [SerializeField] private float foulWindowDuration = 1.5f;

        private FoulOpportunity currentOpportunity;
        private bool windowOpen;

        // ─────────────────────────────────────────────────────────────────────
        private void Update()
        {
            if (!windowOpen || currentOpportunity == null) return;

            currentOpportunity.timeRemaining -= Time.deltaTime;
            hud?.ShowFoulPrompt(currentOpportunity.timeRemaining);

            if (currentOpportunity.timeRemaining <= 0f)
                CloseWindow();
        }

        /// <summary>Call this when contact occurs (e.g. inside PlayerController or DirtyPlayManager).</summary>
        public void RegisterContact(PlayerRuntime caller, PlayerRuntime defender,
            float contactIntensity, bool dirtyAction, bool duringShot)
        {
            if (caller == null || defender == null) return;

            var opportunity = new FoulOpportunity
            {
                caller = caller,
                defender = defender,
                contactIntensity = contactIntensity,
                dirtyActionUsed = dirtyAction,
                duringShot = duringShot,
                timeRemaining = foulWindowDuration
            };

            currentOpportunity = opportunity;
            windowOpen = true;
            foulManager?.SetOpportunity(opportunity);
        }

        private void CloseWindow()
        {
            windowOpen = false;
            currentOpportunity = null;
            hud?.HideFoulPrompt();
            foulManager?.SetOpportunity(null);
        }
    }
}
