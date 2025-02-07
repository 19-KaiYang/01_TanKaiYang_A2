using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroyByContact : MonoBehaviour {

    public GameObject explosion;
    public GameObject playerExplosion;
    public int scoreValue;
    GameController gameController;
    PlayFabUserMgt playFabUserMgt;

    public int xpGain = 10;

    private void Start() {
        GameObject gameControllerObject = GameObject.FindWithTag("GameController");
        if(gameControllerObject != null){
            gameController = gameControllerObject.GetComponent<GameController>();
        } 
        else{
            Debug.Log("GameController object not found");
        }

        GameObject playFabManagerObject = GameObject.FindWithTag("PlayFabManager"); 
        if (playFabManagerObject != null)
        {
            playFabUserMgt = playFabManagerObject.GetComponent<PlayFabUserMgt>();
        }
        else
        {
            Debug.Log("PlayFabUserMgt object not found");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag != "Boundary")
        {
            Instantiate(explosion, transform.position, transform.rotation);

            if (other.tag == "Player")
            {
                Instantiate(playerExplosion, other.transform.position, other.transform.rotation);
                gameController.gameIsOver();

                // Submit the final score
                if (playFabUserMgt != null)
                {
                    int finalScore = gameController.GetScore(); // Get final score
                    playFabUserMgt.SubmitScore(finalScore); // Submit to leaderboard
                }
                else
                {
                    Debug.LogError("PlayFabUserMgt reference is missing!");
                }
            }

              // Add XP when destroying an asteroid
            PFDataMgr.instance.AddXP(xpGain);
            gameController.addScore(scoreValue); // Add score for other objects
            Destroy(other.gameObject);
            Destroy(gameObject);
        }
    }

}
