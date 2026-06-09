using ChainNet.Core;
using UnityEngine;

namespace ChainNet.UI
{
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private CollectionController collectionController;
        [SerializeField] private OptionsController optionsController;

        private TeamSelectController teamSelect;

        private void Awake()
        {
            teamSelect = FindFirstObjectByType<TeamSelectController>();
            if (collectionController == null)
                collectionController = FindFirstObjectByType<CollectionController>();
            if (optionsController == null)
                optionsController = FindFirstObjectByType<OptionsController>();
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

        public void OpenCollection() => collectionController?.Show();
        public void OpenOptions()    => optionsController?.Show();

        public void Quit()
        {
            Application.Quit();
        }
    }
}
