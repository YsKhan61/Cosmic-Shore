using UnityEngine;

namespace StarWriter.Core.Input
{
    public class FlipUI : MonoBehaviour
    {
        void OnEnable()
        {
            PhoneFlipDetector.onPhoneFlip += OnPhoneFlip;
        }

        void OnDisable()
        {
            PhoneFlipDetector.onPhoneFlip -= OnPhoneFlip;
        }
        
        void OnPhoneFlip(bool state)
        {
            transform.rotation = state ? Quaternion.identity /* Flip Off */ : Quaternion.Euler(0, 0, 180) /* Flip On */;
        }
    }
}