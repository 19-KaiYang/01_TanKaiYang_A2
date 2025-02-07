using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using TMPro;
using UnityEngine.UI;

public class PFDataMgr : MonoBehaviour
{
    public static PFDataMgr instance; // Singleton instance

    [SerializeField] TMP_Text XPDisplay;
    [SerializeField] TMP_Text LevelDisplay;
    [SerializeField] Image XP_Bar;

    private int currentXP = 0;
    private int currentLevel = 1;
    private int xpPerLevel = 100; // XP needed per level

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        GetUserData(); 
    }

    public void AddXP(int amount)
    {
        currentXP += amount;

        if (currentXP >= xpPerLevel) 
        {
            currentXP = 0;
            currentLevel++;
        }

        UpdateUI();
        SetUserData(); 
    }

    private void UpdateUI()
    {
        LevelDisplay.text = "Level " + currentLevel;
        XPDisplay.text = "XP: " + currentXP + " / " + xpPerLevel;
        XP_Bar.fillAmount = (float)currentXP / xpPerLevel;
    }

    private void SetUserData()
    {
        var request = new UpdateUserDataRequest
        {
            Data = new Dictionary<string, string>
            {
                {"XP", currentXP.ToString()},
                {"Level", currentLevel.ToString()}
            }
        };

        PlayFabClientAPI.UpdateUserData(request,
            result => Debug.Log("XP & Level Saved to PlayFab"),
            error => Debug.LogError("Error saving XP: " + error.GenerateErrorReport())
        );
    }

    private void GetUserData()
    {
        PlayFabClientAPI.GetUserData(new GetUserDataRequest(),
            result =>
            {
                if (result.Data != null)
                {
                    if (result.Data.ContainsKey("XP"))
                        currentXP = int.Parse(result.Data["XP"].Value);

                    if (result.Data.ContainsKey("Level"))
                        currentLevel = int.Parse(result.Data["Level"].Value);

                    UpdateUI();
                }
            },
            error => Debug.LogError("Error loading XP: " + error.GenerateErrorReport())
        );
    }
}
