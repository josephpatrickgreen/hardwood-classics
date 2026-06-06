using ChainNet.Core;
using UnityEngine;

namespace ChainNet.UI
{
    public class MainMenuController : MonoBehaviour
    {
        public void StartRun()
        {
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
