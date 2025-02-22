using CosmicShore.Game.Arcade;
using Cinemachine;
using UnityEngine;
using System.Collections;
using CosmicShore.Game;

namespace CosmicShore
{

    public class VCamRecorderController : MonoBehaviour
    {
        //[SerializeField] MiniGame game;
        [SerializeField] IPlayer player;
        [SerializeField] CinemachineVirtualCameraBase specialCamera;
        // Start is called before the first frame update
        void Start()
        {
            StartCoroutine(LateStartCoroutine());
        }
        IEnumerator LateStartCoroutine() 
        {
            yield return new WaitForSeconds(3);
            //specialCamera.Follow = specialCamera.LookAt = game.ActivePlayer.Ship.transform;
            specialCamera.Follow = specialCamera.LookAt = player.Ship.Transform;
        }
    }
}
