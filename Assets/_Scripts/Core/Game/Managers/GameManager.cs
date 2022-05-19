using UnityEngine;
using UnityEngine.SceneManagement;
using Amoebius.Utility.Singleton;

namespace StarWriter.Core
{
    [DefaultExecutionOrder(0)]
    public class GameManager : SingletonPersistent<GameManager>
    {          
        [SerializeField]
        private bool skipTutorial = false;

        [SerializeField]
        private bool isGyroEnabled = true;

        private GameSetting gameSettings;

        public delegate void OnPlayGameEvent();
        public static event OnPlayGameEvent onPlayGame;

        public delegate void OnToggleGyroEvent(bool status);
        public static event OnToggleGyroEvent onToggleGyro;

        void Start()
        {
            //PlayerPrefs.SetInt("Skip Tutorial", 1);
            gameSettings = GameSetting.Instance;

            if (PlayerPrefs.GetInt("Skip Tutorial") == 1) // 0 false and 1 true
            {
                skipTutorial = true;
            }
            else { skipTutorial = false; }
        }

        /// <summary>
        /// Toggles the Tutorial On/Off
        /// </summary>
        public void OnClickTutorialToggleButton()
        {
            SceneManager.LoadScene(1);
            if (PauseSystem.GetIsPaused()) { PauseGame(); };
            // Set gameSettings Tutorial status
            gameSettings.TutorialEnabled = !gameSettings.TutorialEnabled;
            //Set PlayerPrefs Tutorial status
            if (gameSettings.TutorialEnabled == true)
            {
                PlayerPrefs.SetInt("tutorialEnabled", 1);  //tutorial enabled
            }
            else
            {
                PlayerPrefs.SetInt("tutorialEnabled", 0);  //tutorial disabled
            }
            

        }

        /// <summary>
        /// Toggles the Gyro On/Off
        /// </summary>
        public void OnClickGyroToggleButton()
        {
            // Set gameSettings Gyro status
            gameSettings.GyroEnabled = isGyroEnabled = !isGyroEnabled;
            onToggleGyro(isGyroEnabled);

            // Set PlayerPrefs Gyro status
            if (isGyroEnabled == true)
            {
                PlayerPrefs.SetInt("gyroEnabled", 1); //gyro enabled

            }
            else
            {
                PlayerPrefs.SetInt("gyroEnabled", 0);  //gyro disabled
            }
        }
        /// <summary>
        /// Starts Tutorial or Game bases on skipTutorial status
        /// </summary>
        public void OnClickPlayButton()
        {
            if (skipTutorial)
            {
                SceneManager.LoadScene(2);
            }
            else
            {
                SceneManager.LoadScene(1);
            }
        }
        /// <summary>
        /// UnPauses game play
        /// </summary>
        public void UnPauseGame()
        {
            if (PauseSystem.GetIsPaused()) { PauseGame(); }
        }
        /// <summary>
        /// Pauses game play
        /// </summary>
        public void PauseGame()
        {
            PauseSystem.TogglePauseGame();
        }

        public void WaitOnPlayerLoading()
        {
            Debug.Log("WaitOnPlayerLoading");
            onPlayGame?.Invoke();
        }
    }
}

