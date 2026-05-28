using UnityEngine;
using UnityEngine.SceneManagement;

namespace UI
{
    public class InGameMenuController : MonoBehaviour
    {
        public void ReturnToMainMenu()
        {
            Core.SaveSystem.SaveGame();
            
            SceneManager.LoadScene("MainMenu");
        }
    }
}