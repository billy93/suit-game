using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;

public class Startup : NetworkBehaviour
{
    private float startupTime;

    // Start is called before the first frame update
    void Start()
    {
        // NetworkManager.Singleton.StartHost();
    }

    // Update is called once per frame
    void Update()
    {
        startupTime += Time.deltaTime;
        if(startupTime > 5f){
            if(NetworkManager.Singleton != null){
                Debug.Log("network exist, now change scene");
                // Loader.LoadNetwork(Loader.Scene.StartGame);
                startupTime = 0f;
                Loader.LoadNetwork(Loader.Scene.StartGame);
                // NetworkManager.Singleton.SceneManager.LoadScene("StartGame", LoadSceneMode.Single);
            }
        }
    }
}
