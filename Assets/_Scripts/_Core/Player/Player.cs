﻿using UnityEngine;
using StarWriter.Core;
using StarWriter.Core.HangerBuilder;
using StarWriter.Core.Input;

[System.Serializable]
public class Player : MonoBehaviour
{
    [SerializeField] string playerName;
    [SerializeField] string playerUUID;
    [SerializeField] Ship ship;
    [SerializeField] Skimmer skimmer;
    [SerializeField] GameObject shipContainer;
    //[SerializeField] AIGunner aiGunner;
    public Teams Team;

    public string PlayerName { get => playerName; }
    public string PlayerUUID { get => playerUUID; }
    public Ship Ship { get => ship; }

    GameManager gameManager;

    void Start()
    {
        gameManager = GameManager.Instance;

        if (playerUUID == "admin")  //TODO check if this is local client
        {
            foreach (Transform child in shipContainer.transform) Destroy(child.gameObject);

            Ship shipInstance = Hangar.Instance.LoadPlayerShip();
            shipInstance.transform.SetParent(shipContainer.transform, false);

            var inputController = GetComponent<InputController>();
            inputController.ship = shipInstance;
            

            shipInstance.GetComponent<AIPilot>().enabled = false;

            //inputController.shipAnimation = shipInstance.GetComponent<ShipAnimation>();

            ship = shipInstance.GetComponent<Ship>();
            ship.Team = Team;
            ship.Player = this;

            gameManager.WaitOnPlayerLoading();
        }
        else
        {
            // TODO: random dice roll, or opposite of player ship selection
            Ship shipInstance = Hangar.Instance.LoadAI1Ship();
            shipInstance.transform.SetParent(shipContainer.transform, false);
            ship = shipInstance.GetComponent<Ship>();

            ship.Team = Team;
            ship.Player = this;
            skimmer.Player = this;
            ship.skimmer= skimmer;
            //aiGunner.trailSpawner = ship.TrailSpawner;

            shipInstance.GetComponent<AIPilot>().enabled = true;

            gameManager.WaitOnAILoading(ship.GetComponent<AIPilot>());
        }
    }
}