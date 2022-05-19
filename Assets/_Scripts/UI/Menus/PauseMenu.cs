using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using StarWriter.Core;
using StarWriter.Core.Audio;


namespace StarWriter.UI
{
    public class PauseMenu : MonoBehaviour
    {
        GameManager gameManager;

        // Start is called before the first frame update
        void Start()
        {
            gameManager = GameManager.Instance;

        }

        public void OnTutorialButton()
        {
            gameManager.OnClickTutorialToggleButton();
        }

        public void OnClickRestartButton()
        {
            gameManager.OnClickPlayButton();
        }

        public void OnClickResumeButton()
        {
            gameManager.OnClickResumeButton();
            transform.GetComponentInParent<GameMenu>().OnClickUnpauseGame();
        }


    }
}