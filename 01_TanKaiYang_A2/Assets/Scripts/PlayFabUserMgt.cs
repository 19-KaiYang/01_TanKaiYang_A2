using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using PlayFab;
using PlayFab.ClientModels;
using TMPro;
using PlayFab.MultiplayerModels;

public class PlayFabUserMgt : MonoBehaviour
{
    [SerializeField] TMP_Text msgbox, leaderboardbox;
    [SerializeField] TMP_InputField if_userInput, if_username, if_email, if_password, if_displayname, currentScore;
    public GameController gameController;
    public GameObject LeaderboardPanel, GlobalLeaderBoardBtn, NearbyLeaderboardBtn;
    PlayFabUserMgt playFabUserMgt;
    [SerializeField] TMP_Text motdText;


    public void OnButtonRegister()
    { //for button click
        var regReq = new RegisterPlayFabUserRequest
        {
            Email = if_email.text,
            Password = if_password.text,
            Username = if_username.text
        };
        PlayFabClientAPI.RegisterPlayFabUser(regReq, OnRegSuccess, OnError);
    }
    void OnRegSuccess(RegisterPlayFabUserResult r)
    {
        msgbox.text = "Register success! Playfab id allocated:" + r.PlayFabId;
        OnUpdateDispName(); //add display name together with registration
    }
    public void OnUpdateDispName() //display name is used for leaderboard and other public facing stuffs
    {
        var UserTitleDispNameReq = new UpdateUserTitleDisplayNameRequest
        {
            DisplayName = if_displayname.text,
        };
        PlayFabClientAPI.UpdateUserTitleDisplayName(UserTitleDispNameReq, OnDisplayNameUpdate, OnError);
    }

    void OnDisplayNameUpdate(UpdateUserTitleDisplayNameResult r)
    {
        UpdateMsg("Display Name updated to:" + r.DisplayName);
    }
    void OnError(PlayFabError e)
    {
        msgbox.text = "Error:" + e.GenerateErrorReport();
    }
    void OnLoginSucc(LoginResult r)
    {
        msgbox.text="Login Success! Playfab ID:"+r.PlayFabId;

        PlayerPrefs.SetString("PlayerID", r.PlayFabId); // Save ID
        PlayerPrefs.Save();


        LoadLevel();
    }
    void LoadLevel()
    {
        SceneManager.LoadScene("Menu"); //go to menu scene
    }
    public void OnLogOut()
    {
        PlayFabClientAPI.ForgetAllCredentials();
        PlayerPrefs.DeleteKey("GuestID");

        SceneManager.LoadScene("RegLoginScene"); 
    }
    public void LoadScene(string scn)
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(scn);
    }
    public void OnResetPassword()
    {
        var ResetPassReq = new SendAccountRecoveryEmailRequest
        {
            Email = if_email.text,
            TitleId = PlayFabSettings.TitleId
          
        };
        PlayFabClientAPI.SendAccountRecoveryEmail(ResetPassReq, onResetPassSucc, OnError);
    }
    void onResetPassSucc(SendAccountRecoveryEmailResult r)
    {
        msgbox.text = "Recovery email sent! Please check your email";
    }
    void UpdateMsg(string msg)
    {
        msgbox.text = msg;
    }
    public void OnButtonDeviceLogin()
    {
        string uniqueGuestID = PlayerPrefs.GetString("GuestID", "");

       
        if (string.IsNullOrEmpty(uniqueGuestID) || uniqueGuestID == "Reset")
        {
            uniqueGuestID = System.Guid.NewGuid().ToString();
            PlayerPrefs.SetString("GuestID", uniqueGuestID);
            PlayerPrefs.Save();
        }

        var request = new LoginWithCustomIDRequest
        {
            CustomId = uniqueGuestID,
            CreateAccount = true
        };

        PlayFabClientAPI.LoginWithCustomID(request, OnGuestLoginSuccess, OnError);
    }


    void OnGuestLoginSuccess(LoginResult r)
    {
        msgbox.text = "Guest Login Success! Playfab ID: " + r.PlayFabId;

      
        PlayFabClientAPI.GetAccountInfo(new GetAccountInfoRequest(),
            result =>
            {
                if (string.IsNullOrEmpty(result.AccountInfo.TitleInfo.DisplayName))
                {
                    var displayNameRequest = new UpdateUserTitleDisplayNameRequest
                    {
                        DisplayName = "Guest"
                    };
                    PlayFabClientAPI.UpdateUserTitleDisplayName(displayNameRequest, OnDisplayNameUpdate, OnError);
                }
            },
            OnError);

        PlayerPrefs.SetString("PlayerID", r.PlayFabId); 
        PlayerPrefs.Save();

        LoadLevel(); 
    }


    //public void OnButtonLoginEmail()
    //{
    //    var loginEmailReq = new LoginWithEmailAddressRequest
    //    {
    //        Email = if_email.text,
    //        Password = if_password.text,
    //        InfoRequestParameters = new GetPlayerCombinedInfoRequestParams
    //        {
    //            GetPlayerProfile = true
    //        }
    //    }; 
    //    PlayFabClientAPI.LoginWithEmailAddress(loginEmailReq, OnLoginSucc, OnError);
    //}
    //public void OnButtonLoginUserName()
    //{
    //    var loginReq = new LoginWithPlayFabRequest { 
    //        Username = if_username.text,
    //        Password = if_password.text,
    //        InfoRequestParameters = new GetPlayerCombinedInfoRequestParams
    //        {
    //            GetPlayerProfile = true
    //        }
    //    };
    //    PlayFabClientAPI.LoginWithPlayFab(loginReq, OnLoginSucc, OnError);
    //}

    public void OnButtonLogin()
    {
        string userInput = if_username.text; 
        string password = if_password.text;

        if (IsValidEmail(userInput))
        {
            var loginEmailReq = new LoginWithEmailAddressRequest
            {
                Email = userInput,
                Password = password,
                InfoRequestParameters = new GetPlayerCombinedInfoRequestParams
                {
                    GetPlayerProfile = true
                }
            };
            PlayFabClientAPI.LoginWithEmailAddress(loginEmailReq, OnLoginSucc, OnError);
        }
        else
        {
            var loginReq = new LoginWithPlayFabRequest
            {
                Username = userInput,
                Password = password,
                InfoRequestParameters = new GetPlayerCombinedInfoRequestParams
                {
                    GetPlayerProfile = true
                }
            };
            PlayFabClientAPI.LoginWithPlayFab(loginReq, OnLoginSucc, OnError);
        }
    }

    public void ClientGetTitleData()
    {
        PlayFabClientAPI.GetTitleData(new GetTitleDataRequest(),
            result => {
                if (result.Data == null || !result.Data.ContainsKey("MOTD"))
                {
                    Debug.Log("No MOTD available");
                }
                else
                {
                    Debug.Log("MOTD: " + result.Data["MOTD"]);
                    motdText.text = result.Data["MOTD"]; 
                }
            },
            error => {
                Debug.Log("Got error getting titleData:");
                Debug.Log(error.GenerateErrorReport());
            }
        );
    }


    void Start()
    {
        ClientGetTitleData();
        //UpdateLeaderboardRandom();
    }

    public void SubmitScore(int playerScore)
    {
        PlayFabClientAPI.GetPlayerStatistics(new GetPlayerStatisticsRequest(),
            result =>
            {
                int currentHighScore = 0;

                foreach (var stat in result.Statistics)
                {
                    if (stat.StatisticName == "Highscore")
                    {
                        currentHighScore = stat.Value; 
                        break;
                    }
                }

                if (playerScore > currentHighScore) 
                {
                    Debug.Log($"New Highscore! {playerScore} (Previous: {currentHighScore})");

                    PlayFabClientAPI.UpdatePlayerStatistics(new UpdatePlayerStatisticsRequest
                    {
                        Statistics = new List<StatisticUpdate>
                        {
                        new StatisticUpdate
                        {
                            StatisticName = "Highscore",
                            Value = playerScore
                        }
                        }
                    },
                    result => Debug.Log("Successfully submitted new highscore."),
                    error => Debug.LogError(error.GenerateErrorReport()));
                }
                else
                {
                    Debug.Log($"Score {playerScore} is not higher than current highscore {currentHighScore}. No update made.");
                }
            },
            error => Debug.LogError(error.GenerateErrorReport()));
    }



    private void OnStatisticsUpdated(UpdatePlayerStatisticsResult updateResult)
    {
        Debug.Log("Successfully submitted high score");
        UpdateMsg("Successfully leaderboard sent:" + updateResult.ToString());
    }



    public void RequestLeaderboard()
    {
        Debug.Log("Req LB");
        PlayFabClientAPI.GetLeaderboard(new GetLeaderboardRequest
        {
            StatisticName = "HighScore",
            StartPosition = 0,
            MaxResultsCount = 10
        }, result => DisplayLeaderboard(result), OnError);
    }


    void DisplayLeaderboard(GetLeaderboardResult rst)
    {
        leaderboardbox.text = "";

        string leaderboardHeader = "<b> Rank\tName\t\tScore</b>\n"; 
        string lbstring = leaderboardHeader;

        string currentPlayerId = PlayerPrefs.GetString("PlayerID", "");

        foreach (var item in rst.Leaderboard)
        {
            string playerName = string.IsNullOrEmpty(item.DisplayName) ? "Guest" : item.DisplayName;

            // Ensure consistent name length
            playerName = playerName.Length > 12 ? playerName.Substring(0, 12) : playerName.PadRight(12);

          
            string rankWithSpacing = $"  {item.Position + 1}"; 

            // Format the row
            string playerRow = $"{rankWithSpacing}\t\t{playerName}\t\t{item.StatValue}\n";

            // Highlight the **entire row** if it's the current player
            if (item.PlayFabId == currentPlayerId)
            {
                playerRow = $"<color=#FFD700><b>{playerRow}</b></color>"; // Gold color + Bold
            }

            lbstring += playerRow;
        }

        leaderboardbox.text = lbstring;
    }




    public void UpdateLeaderboardWithGameScore()
    {
        if (gameController != null)
        {
            int playerScore = gameController.GetScore(); 
            SubmitScore(playerScore);
        }
        else
        {
            Debug.LogError("GameController reference is not set!");
        }
    }




    public void OnButtonGetLeaderBoard()
    {
      

        PlayFabClientAPI.GetLeaderboard(new GetLeaderboardRequest
        {
            StatisticName = "Highscore",
            StartPosition = 0,
            MaxResultsCount = 10
        }, result => DisplayLeaderboard(result), OnError);
    }


    void OnLeaderboardGet(GetLeaderboardResult r)
    {
        string LeaderboardStr = "Leaderboard\n";
        foreach (var item in r.Leaderboard)
        {
            string onerow = item.Position + "/" + item.PlayFabId + "/" + item.DisplayName + "/" + item.StatValue + "\n";
            LeaderboardStr += onerow;
        }
        UpdateMsg(LeaderboardStr);
    }

    public void OnButtonSendLeaderboard()
    {
        var req = new UpdatePlayerStatisticsRequest
        {
            Statistics = new List<StatisticUpdate>
            {
                new StatisticUpdate
                {
                    StatisticName = "Highscore",
                    Value=int.Parse(currentScore.text)
                }
            }
        };
        UpdateMsg("Submitting score:" + currentScore.text);
        PlayFabClientAPI.UpdatePlayerStatistics(req, OnLeaderboardUpdate, OnError);
    }

    void OnLeaderboardUpdate(UpdatePlayerStatisticsResult r)
    {
        UpdateMsg("Successful Leaderboard sent:" + r.ToString());
    }

    public void OnButtonBackToLoginPressed()
    {
        SceneManager.LoadScene("RegLoginScene");
    }
    public void OnButtonGoToRegister()
    {
        SceneManager.LoadScene("RegisterPage");
    }

    public void OnButtonGuestLoginPressed()
    {
        SceneManager.LoadScene("Menu");
    }

    private bool IsValidEmail(string input)
    {
        return input.Contains("@") && input.Contains(".");
    }

    public void OnButtonShowLeaderboardUI()
    {
        if (!LeaderboardPanel.activeSelf) 
        {
            LeaderboardPanel.SetActive(true);
            GlobalLeaderBoardBtn.SetActive(true);
            NearbyLeaderboardBtn.SetActive(true);
        }
        else
        {
            LeaderboardPanel.SetActive(false);
            GlobalLeaderBoardBtn.SetActive(false);
            NearbyLeaderboardBtn.SetActive(false);
        }
    }

    public void OnButtonNearbyLeaderboard()
    {
        string currentPlayerId = PlayerPrefs.GetString("PlayerID", "");

        
        PlayFabClientAPI.GetLeaderboard(new GetLeaderboardRequest
        {
            StatisticName = "Highscore",
            StartPosition = 0,
            MaxResultsCount = 100 
        }, result =>
        {
            int playerRank = -1;

            foreach (var item in result.Leaderboard)
            {
                if (item.PlayFabId == currentPlayerId)
                {
                    playerRank = item.Position;
                    break;
                }
            }

            if (playerRank == -1)
            {
                leaderboardbox.text = "Player not found in leaderboard.";
                return;
            }

          
            int startRank = Mathf.Max(0, playerRank - 3); 
            int endRank = playerRank + 3;

            PlayFabClientAPI.GetLeaderboard(new GetLeaderboardRequest
            {
                StatisticName = "Highscore",
                StartPosition = startRank,
                MaxResultsCount = (endRank - startRank) + 1 
            }, nearbyResult => DisplayLeaderboard(nearbyResult), OnError);

        }, OnError);
    }


    public void onInventoryButtonpressed()
    {
        SceneManager.LoadScene("InventoryScene");
    }

    public void onSkillSceneButtonpressed()
    {
        SceneManager.LoadScene("SkillScene");
    }


}
