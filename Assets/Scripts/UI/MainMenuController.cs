using ChainNet.Core;
using UnityEngine;

namespace ChainNet.UI
{
    public class MainMenuController : MonoBehaviour
    {
        private TeamSelectController teamSelect;

        private void Awake()
        {
            teamSelect = FindFirstObjectByType<TeamSelectController>();
        }

        /// <summary>
        /// Opens the team-selection screen; the run starts from there once a team is confirmed.
        /// Falls back to immediately starting the run if no TeamSelectController is present.
        /// </summary>
        public void StartRun()
        {
            if (teamSelect != null)
                teamSelect.ShowTeamSelect();
            else
                GameManager.Instance?.StartRun();
        }

        public void OpenCollection() { }
        public void OpenOptions() { }
        public void Quit()
        {
            Application.Quit();
        }
    }
}
