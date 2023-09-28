using StarWriter.Core.CloutSystem;
using StarWriter.Core.HangerBuilder;
using StarWriter.Utility.Singleton;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.SceneManagement;
using static StarWriter.Core.LoadoutFavoriting.LoadoutCard;

public struct Loadout
{
    public int Intensity;
    public int PlayerCount;
    public ShipTypes ShipType;
    public MiniGames GameMode;

    public Loadout(int intensity, int playerCount, ShipTypes shipType, MiniGames gameMode)
    {
        Intensity = intensity;
        PlayerCount = playerCount;
        ShipType = shipType;
        GameMode = gameMode;
    }
    public override string ToString()
    {
        return Intensity + "_" + PlayerCount + "_" + ShipType + "_" + GameMode ;
    }
}
namespace StarWriter.Core.LoadoutFavoriting
{
    public static class LoadoutSystem
    {
        static int loadoutIndex = 0;

        static global::Loadout activeLoadout;

        static List<global::Loadout> loadouts;
 
        public static void Init()
        {
            loadouts = new List<global::Loadout>()          
            {
            new global::Loadout() { Intensity=1, PlayerCount=1, GameMode= MiniGames.BlockBandit, ShipType= ShipTypes.Manta},
            new global::Loadout() { Intensity=1, PlayerCount=1, GameMode= MiniGames.BlockBandit, ShipType= ShipTypes.Manta},
            new global::Loadout() { Intensity=1, PlayerCount=1, GameMode= MiniGames.BlockBandit, ShipType= ShipTypes.Manta},
            new global::Loadout() { Intensity=1, PlayerCount=1, GameMode= MiniGames.BlockBandit, ShipType= ShipTypes.Manta}
            };
            activeLoadout = loadouts[0];
        }
        public static bool CheckLoadoutsExist(int idx)
        {
            return loadouts.Count > idx;
        }

        public static global::Loadout GetActiveLoadout()
        {
            return activeLoadout;
        }

        public static global::Loadout GetLoadout(int idx)
        {
            return loadouts[idx];
        }

        public static List<global::Loadout> GetFullListOfLoadouts()
        {
            return loadouts;
        }
        public static int GetActiveLoadoutsIndex()    //Loadout Select Buttions set index 1-4
        {
            return loadoutIndex;
        }
        

        public static void SetCurrentlySelectedLoadout(global::Loadout loadout, int loadoutIndex)
        {
            int idx = loadoutIndex--;  //change 1-4 to 0-3
            idx = Mathf.Clamp(idx, 0, loadouts.Count);
            loadouts[idx] = loadout;
        }

        
        public static void SetActiveLoadoutIndex(int loadoutIndex) 
        {
            Debug.Log("Loadout Index changed to " + loadoutIndex);

            int idx = loadoutIndex--;  //change 1-4 to 0-3
            idx = Mathf.Clamp(idx, 0, loadouts.Count);            
            activeLoadout = loadouts[idx];
        }

    }
}



