using UnityEngine;
using StarWriter.Core;
using System;

namespace StarWriter.UI
{
    public class MusicToggleSynchrornizer : MonoBehaviour
    {
        private bool isMuted = true;
        public SwitchToggle switchToggle;

        void Start()
        {
            GameSetting gameSettings = GameSetting.Instance;
            isMuted = gameSettings.IsMuted;

            switchToggle = GetComponent<SwitchToggle>();
        }

        private void OnEnable()
        {
            GameSetting.OnChangeGyroStatus += SyncGyroStatus;
        }

        private void OnDisable()
        {
            GameSetting.OnChangeGyroStatus -= SyncGyroStatus;
        }

        private void SyncGyroStatus(bool status)
        {
            isMuted = status;
            switchToggle.Toggled(isMuted);
        }
    }

}
