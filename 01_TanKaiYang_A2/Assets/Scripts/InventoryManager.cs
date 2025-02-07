using System.Collections.Generic;
using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class InventoryManager : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI Msg;
    [SerializeField] private TextMeshProUGUI currencyTextInventory;
    [SerializeField] private TextMeshProUGUI currencyTextShop;
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private TextMeshProUGUI itemPriceText;

    [Header("Panels")]
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private GameObject shopPanel;

    [Header("Inventory UI")]
    [SerializeField] private GameObject inventoryItemPrefab;
    [SerializeField] private Transform inventoryContainer;

    [Header("Shop UI")]
    [SerializeField] private GameObject shopItemPrefab;
    [SerializeField] private Transform shopContainer;

    [SerializeField] private TextMeshProUGUI itemDescriptionText;

    private string selectedItemId;
    private int selectedItemPrice;
    private string virtualCurrency = "CN"; 

    void Start()
    {
        // Ensure Msg is assigned at runtime if not set in Inspector
        if (Msg == null)
        {
            Msg = GameObject.Find("MsgText")?.GetComponent<TextMeshProUGUI>();
            if (Msg == null)
            {
                Debug.LogWarning("Msg UI Text is missing! Debug messages won't be displayed.");
            }
        }

        GetVirtualCurrencies();
        GetPlayerInventory();
        GetCatalog();
        ShowInventory(); 
    }

    void UpdateMsg(string msg)
    {
        Debug.Log(msg);
        if (Msg != null)
        {
            Msg.text = msg;
        }
    }

    void OnError(PlayFabError e)
    {
        UpdateMsg(e.GenerateErrorReport());
    }

    
    public void ShowInventory()
    {
        inventoryPanel.SetActive(true);
        shopPanel.SetActive(false);
        GetPlayerInventory();
    }

    public void ShowShop()
    {
        inventoryPanel.SetActive(false);
        shopPanel.SetActive(true);
        GetCatalog();
    }

 
    public void GetVirtualCurrencies()
    {
        PlayFabClientAPI.GetUserInventory(new GetUserInventoryRequest(),
        result => {
            int coins = result.VirtualCurrency.ContainsKey(virtualCurrency) ? result.VirtualCurrency[virtualCurrency] : 0;

            if (currencyTextInventory != null) currencyTextInventory.text = "Coins: $" + coins;
            if (currencyTextShop != null) currencyTextShop.text = "Coins: $" + coins;
        }, OnError);
    }


    public void GetPlayerInventory()
    {
        PlayFabClientAPI.GetUserInventory(new GetUserInventoryRequest(),
        result => {
            
            foreach (Transform child in inventoryContainer)
            {
                Destroy(child.gameObject);
            }

            Dictionary<string, int> itemStack = new Dictionary<string, int>();

           
            foreach (ItemInstance item in result.Inventory)
            {
                if (itemStack.ContainsKey(item.DisplayName))
                {
                    itemStack[item.DisplayName] += (item.RemainingUses ?? 1); 
                }
                else
                {
                    itemStack[item.DisplayName] = (item.RemainingUses ?? 1);
                }
            }

            // Loop through the dictionary and create UI items
            foreach (var entry in itemStack)
            {
                GameObject newItem = Instantiate(inventoryItemPrefab, inventoryContainer);
                TMP_Text[] texts = newItem.GetComponentsInChildren<TMP_Text>();

                if (texts.Length >= 2)
                {
                    texts[0].text = entry.Key; // Item Name
                    texts[1].text = "x" + entry.Value; // Stack Count
                }
            }
        }, OnError);
    }



    public void GetCatalog()
    {
        PlayFabClientAPI.GetCatalogItems(new GetCatalogItemsRequest { CatalogVersion = "terranweapons" },
        result => {
            foreach (Transform child in shopContainer)
            {
                Destroy(child.gameObject); // Clear previous items
            }

            foreach (CatalogItem i in result.Catalog)
            {
                GameObject newItem = Instantiate(shopItemPrefab, shopContainer);

                TMP_Text[] texts = newItem.GetComponentsInChildren<TMP_Text>();
                if (texts.Length >= 2)
                {
                    texts[0].text = i.DisplayName;  // Assign item name
                    texts[1].text = "$" + i.VirtualCurrencyPrices["CN"]; // Assign price
                }

                newItem.GetComponent<Button>().onClick.AddListener(() =>
                    SelectItem(i.ItemId, i.DisplayName, (int)i.VirtualCurrencyPrices["CN"], i.Description, newItem)
                );
            }
        }, OnError);
    }



    void SelectItem(string itemId, string itemName, int price, string description, GameObject selectedItem)
    {
        selectedItemId = itemId;
        selectedItemPrice = price;

        itemNameText.text = itemName;
        itemPriceText.text = "Buy ($" + price + ")";
        itemDescriptionText.text = description; 

       
        foreach (Transform child in shopContainer)
        {
            Image img = child.GetComponent<Image>();
            if (img != null) img.color = Color.white;
        }

       
        Image selectedImg = selectedItem.GetComponent<Image>();
        if (selectedImg != null) selectedImg.color = Color.yellow;
    }



    public void BuyItem()
    {
        if (string.IsNullOrEmpty(selectedItemId)) return;

        PlayFabClientAPI.PurchaseItem(new PurchaseItemRequest
        {
            CatalogVersion = "terranweapons",
            ItemId = selectedItemId,
            VirtualCurrency = "CN",
            Price = selectedItemPrice
        },
        result => {
            StopAllCoroutines();  // Stop any running coroutines
            UpdateMsg($"Bought {selectedItemId}!");  // Show message

            GetVirtualCurrencies();  // Update coins
            GetPlayerInventory();    // Add item to inventory

            StartCoroutine(ClearPurchaseMessage()); // Clear message after 2 sec
        },
        OnError);
    }

    private IEnumerator ClearPurchaseMessage()
    {
        yield return new WaitForSeconds(2f);
        if (Msg != null)
        {
            Msg.text = ""; // Properly clear the message
        }
    }



   




    public void OnBackButtonPressed()
    {
        SceneManager.LoadScene("Menu");
    }

}
