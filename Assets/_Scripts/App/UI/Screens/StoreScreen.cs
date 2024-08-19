using CosmicShore.App.UI.Modals;
using CosmicShore.App.UI.Views;
using CosmicShore.Integrations.PlayFab.Economy;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CosmicShore.App.Ui.Menus
{
    public class StoreScreen : View
    {
        [Header("Crystal Balance")]
        [SerializeField] TMP_Text CrystalBalance;

        [Header("Captain Purchasing")]
        [SerializeField] PurchaseCaptainCard PurchaseCaptainPrefab;
        [SerializeField] List<HorizontalLayoutGroup> CaptainPurchaseRows;
        [SerializeField] PurchaseConfirmationModal PurchaseConfirmationModal;
        [SerializeField] Button PurchaseConfirmationButton;

        [Header("Game Purchasing")] 
        [SerializeField] PurchaseGameCard PurchaseGamePrefab;
        [SerializeField] List<HorizontalLayoutGroup> GamePurchaseRows;

        [Header("Daily Challenge and Faction Tickets")]
        //[SerializeField] PurchaseGameplayTicketCard FactionMissionTicketCard;
        [SerializeField] PurchaseGameplayTicketCard DailyChallengeTicketCard;

        bool captainCardsPopulated = false;

        void OnEnable()
        {
            //CaptainManager.OnLoadCaptainData += UpdateView;
            CatalogManager.OnLoadInventory += UpdateView;
            CatalogManager.OnCurrencyBalanceChange += UpdateCrystalBalance;
        }

        void OnDisable()
        {
            //CaptainManager.OnLoadCaptainData -= UpdateView;
            CatalogManager.OnLoadInventory -= UpdateView;
            CatalogManager.OnCurrencyBalanceChange -= UpdateCrystalBalance;
        }

        void Start()
        {
            // Clear out placeholder captain cards
            foreach (var row in CaptainPurchaseRows)
            {
                foreach (Transform child in row.transform)
                {
                    Destroy(child.gameObject);
                }
            }

            // If the scene is reloaded after playing a game, the catalog is already loaded, so the events wont fire
            if (CatalogManager.CatalogLoaded)
                UpdateView();
        }

        public override void UpdateView()
        {
            UpdateCrystalBalance();
            //FactionMissionTicketCard.SetVirtualItem(CatalogManager.Instance.GetFactionTicket());
            DailyChallengeTicketCard.SetVirtualItem(CatalogManager.Instance.GetDailyChallengeTicket());
            PopulateCaptainPurchaseCards();
        }

        [SerializeField] int CaptainsPerRow = 3;
        [SerializeField] int MaxCaptainRows = 2;

        void PopulateCaptainPurchaseCards()
        {
            
            if (captainCardsPopulated)
                return;

            // Get all purchaseable captains
            var captains = CatalogManager.StoreShelve.captains.Values.ToList();
            Debug.Log($"PopulateCaptainPurchaseCards, unfiltered: {captains.Count}");

            // Filter out owned captains
            captains = captains.Where(x => !CatalogManager.Inventory.captains.Contains(x)).ToList();
            Debug.Log($"PopulateCaptainPurchaseCards, excluding purchased: {captains.Count}");

            // Filter out unencountered captains
            captains = captains.Where(x => CaptainManager.Instance.GetCaptainByName(x.Name).Encountered == true).ToList();
            Debug.Log($"PopulateCaptainPurchaseCards, excluding not encountered: {captains.Count}");

            // if no captains, hide captains section
            // TODO: this just hides the rows
            if (captains.Count == 0)
                foreach (var r in CaptainPurchaseRows)
                    r.gameObject.SetActive(false);

            var captainIndex = 0;
            var rowIndex = 0;
            var row = CaptainPurchaseRows[rowIndex];
            while (captainIndex < CaptainsPerRow*MaxCaptainRows && captainIndex < captains.Count && rowIndex < MaxCaptainRows)
            {
                var captain = captains[captainIndex];

                var purchaseTicketCard = Instantiate(PurchaseCaptainPrefab);
                purchaseTicketCard.ConfirmationModal = PurchaseConfirmationModal;
                purchaseTicketCard.ConfirmationButton = PurchaseConfirmationButton;
                purchaseTicketCard.SetVirtualItem(captain);
                purchaseTicketCard.transform.SetParent(row.transform, false);

                captainIndex++;
                if (captainIndex % CaptainsPerRow == 0)
                {
                    rowIndex++;
                    if (rowIndex < MaxCaptainRows)
                    {
                        row = CaptainPurchaseRows[rowIndex];
                        row.gameObject.SetActive(true);
                    }
                }
            }

            captainCardsPopulated = true;
        }


        void PopulateGamePurchaseCards()
        {

        }

        void UpdateCrystalBalance()
        {
            StartCoroutine(UpdateBalanceCoroutine());
        }

        IEnumerator UpdateBalanceCoroutine()
        {
            var crystalBalance = int.Parse(CrystalBalance.text);
            var newCrystalBalance = CatalogManager.Instance.GetCrystalBalance();
            Debug.Log($"UpdateBalanceCoroutine - initial Balance: {crystalBalance}, new Balance: {newCrystalBalance}");
            var delta = crystalBalance- newCrystalBalance;
            var duration = 1f;
            var elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                CrystalBalance.text = ((int)(crystalBalance - (delta * elapsedTime / duration))).ToString();
                yield return null;
                elapsedTime += Time.unscaledDeltaTime;
            }
            CrystalBalance.text = CatalogManager.Instance.GetCrystalBalance().ToString();   
        }
    }
}