﻿using CosmicShore.App.Systems.Xp;
using CosmicShore.Integrations.PlayFab.Economy;
using CosmicShore.Models;
using CosmicShore.Utility.Singleton;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CosmicShore.Integrations.Playfab.Economy
{
    [System.Serializable]
    class CaptainData
    {
        /// <summary>
        /// Dictionary mapping captain name to captain for all encountered, but not yet unlocked captains
        /// </summary>
        public Dictionary<string, Captain> EncounteredCaptains = new Dictionary<string, Captain>();
        /// <summary>
        /// Dictionary mapping captain name to captain for all unlocked captains
        /// </summary>
        public Dictionary<string, Captain> UnlockedCaptains = new Dictionary<string, Captain>();
        /// <summary>
        /// Dictionary mapping captain name to captain for all unlocked captains
        /// </summary>
        public Dictionary<string, Captain> AllCaptains = new Dictionary<string, Captain>();
    }

    public  class CaptainManager : SingletonPersistent<CaptainManager>
    {
        [SerializeField] SO_CaptainList AllCaptains;

        CaptainData captainData;

        void OnEnable()
        {
            XpHandler.XPLoaded += LoadCaptainData;
        }

        void OnDisable()
        {
            XpHandler.XPLoaded -= LoadCaptainData;
        }

        public void LoadCaptainData()
        {
            captainData = new CaptainData();
            foreach (var so_Captain in AllCaptains.CaptainList)
            {
                var captain = new Captain(so_Captain);

                // Set XP
                captain.XP = XpHandler.GetCaptainXP(captain);

                // check for unlocked
                var unlocked = CatalogManager.Inventory.ContainsCaptain(so_Captain.Name);
                if (unlocked)
                {
                    captain.Unlocked = true;
                    captainData.UnlockedCaptains.Add(so_Captain.Name, captain);
                }
                else
                {
                    // Check for encountered
                    captain.Encountered = true; 
                    captainData.EncounteredCaptains.Add(so_Captain.Name, captain);
                }
                captainData.AllCaptains.Add(so_Captain.Name, captain);

                // Set Level
                var captainUpgrade = CatalogManager.Inventory.captainUpgrades.Where(x => x.Tags.Contains(so_Captain.Ship.Class.ToString()) && x.Tags.Contains(so_Captain.PrimaryElement.ToString())).FirstOrDefault();
                if (captainUpgrade != null)
                {
                    foreach (var tag in captainUpgrade.Tags)
                        if (tag.StartsWith("Upgrade"))
                            captain.Level = int.Parse(tag.Replace("Upgrade_", ""));
                }
                else if (unlocked)
                    captain.Level = 1;
                else
                    captain.Level = 0;

                Debug.Log($"LoadCaptainData - {captain.Name}, Level:{captain.Level}, XP:{captain.XP}, Unlocked:{captain.Unlocked}, Encountered:{captain.Encountered}");
            }
        }

        public void IssueXP(string captainName, int amount)
        {
            Debug.Log($"CaptainManager.IssueXP {captainName}, {amount}");
            IssueXP(GetCaptainByName(captainName), amount);
        }

        public void IssueXP(Captain captain, int amount)
        {
            //if (!captainData.UnlockedCaptains.ContainsKey(captain.SO_Captain.Name)) { return; }

            captain.XP += amount;
            //captainData.UnlockedCaptains[captain.SO_Captain.Name].XP += amount;
            captainData.AllCaptains[captain.SO_Captain.Name].XP += amount;

            // Save to Playfab
            Debug.Log($"CaptainManager.IssueXP {captain.Name}, {amount}");
            XpHandler.IssueXP(captain, amount);
        }

        public Captain GetCaptainByName(string name)
        {
            return captainData.AllCaptains.Where(x => x.Value.Name == name).FirstOrDefault().Value;
        }

        public List<Captain> GetEncounteredCaptains()
        {
            return captainData.EncounteredCaptains.Values.ToList();
        }
        public List<Captain> GetUnlockedCaptains()
        {
            return captainData.UnlockedCaptains.Values.ToList();
        }
        public List<Captain> GetAllCaptains()
        {
            return captainData.AllCaptains.Values.ToList();
        }
    }
}