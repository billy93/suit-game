using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System;

public class SuitGameManager : NetworkBehaviour
{
    
    public static SuitGameManager Instance { get; private set; }
    private NetworkVariable<State> state = new NetworkVariable<State>(State.WaitingToStart);
    private Dictionary<ulong, bool> playerReadyDictionary;
    private Dictionary<ulong, int> playerChooseDictionary;
    private ulong winner;
    public event EventHandler OnStateChanged;

    public enum Selection{
        Rock,
        Scissor,
        Paper
    }

    private enum State {
        WaitingToStart,
        GamePlaying,
        GameEnd,
    }
    void Awake(){
        Instance = this;

        playerReadyDictionary = new Dictionary<ulong, bool>();
        playerChooseDictionary = new Dictionary<ulong, int>();
        NetworkManager.Singleton.OnClientDisconnectCallback += NetworkManager_Server_OnClientDisconnectCallback;
       
    }

    private void NetworkManager_Server_OnClientDisconnectCallback(ulong obj)
    {
        // if any user disconnect, end game
        Shutdown();
    }

    // Start is called before the first frame update
    void Start()
    {
        PlayerReadyServerRpc();
    }

    public override void OnNetworkSpawn() {
        state.OnValueChanged += State_OnValueChanged;
    }

    private void State_OnValueChanged(State previousValue, State newValue) {
        OnStateChanged?.Invoke(this, EventArgs.Empty);
    }

    // Update is called once per frame
    void Update()
    {
        if (!IsServer) {
            return;
        }

        switch (state.Value) {
            case State.WaitingToStart:
                break;
            case State.GamePlaying:
                
                break;
            case State.GameEnd:
                break;
        }
    }

    public void PlayServer(){
        bool start = NetworkManager.Singleton.StartHost();
        Debug.Log("Is start server ? "+start);
        // PlayerReadyServerRpc();
    }

    public void PlayClient(){
        bool start = NetworkManager.Singleton.StartClient();
        Debug.Log("Is start client? "+start);
        // PlayerReadyServerRpc();
    }

    [ServerRpc(RequireOwnership=false)]
    public void PlayerReadyServerRpc(
        ServerRpcParams serverRpcParams = default
    ){
        Debug.Log("NetworkManager.Singleton.ConnectedClientsIds.Count : "+NetworkManager.Singleton.ConnectedClientsIds.Count);
        
        playerReadyDictionary[serverRpcParams.Receive.SenderClientId] = true;
        bool allClientsReady = true;
        if (NetworkManager.Singleton.ConnectedClientsIds.Count < 2) {
            allClientsReady = false;
        }

        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds) {
            if (!playerReadyDictionary.ContainsKey(clientId) || !playerReadyDictionary[clientId]) {
                // This player is NOT ready
                allClientsReady = false;
                break;
            }
        }

        if (allClientsReady) {
            state.Value = State.GamePlaying;
        }
    }

    public bool IsGameStart(){
        return state.Value == State.GamePlaying;
    }

    public bool IsGameEnd(){
        return state.Value == State.GameEnd;
    }

    [ServerRpc(RequireOwnership=false)]
    public void ChooseServerRPC(int selection, ServerRpcParams rpcParams = default)
    {
        playerChooseDictionary[rpcParams.Receive.SenderClientId] = selection;
        if (playerChooseDictionary.Count == 2) {
            // check winner

            ulong winner = CheckWinner(rpcParams);
            CheckWinnerClientRpc(winner);
            state.Value = State.GameEnd;
        }
    }

    [ClientRpc]
    public void CheckWinnerClientRpc(ulong winner){
        this.winner = winner;
    }

    private ulong CheckWinner(ServerRpcParams rpcParams){
        int serverSelection = -1;
        int clientSelection = -1;

        ulong serverId = 0L;
        ulong clientId = 0L;
        foreach (KeyValuePair<ulong, int> kvp in playerChooseDictionary) {
            ulong playerId = kvp.Key;
            int playerChoose = kvp.Value;

            // Gunakan nilai enum dan ServerRpcParams sesuai kebutuhan
            if (playerId != NetworkManager.Singleton.LocalClientId)
            {
                // server 
                serverSelection = playerChoose;
                serverId = playerId;
            }
            else
            {
                // client
                clientSelection = playerChoose;
                clientId = playerId;
            }
        }
    
        Selection serverSelectionData = (Selection)serverSelection;
        Selection clientSelectionData = (Selection)clientSelection;
        
        ulong winner = 99L;
        if(serverSelectionData == Selection.Rock){
            if(clientSelectionData == Selection.Scissor){
                winner = serverId;
            }
            else if(clientSelectionData == Selection.Paper){
                winner = clientId;
            }
        }
        else if(serverSelectionData == Selection.Scissor){
            if(clientSelectionData == Selection.Rock){
                winner = clientId;
            }
            else if(clientSelectionData == Selection.Paper){
                winner = serverId;
            }
        }
        else if(serverSelectionData == Selection.Paper){
            if(clientSelectionData == Selection.Rock){
                winner = serverId;
            }
            else if(clientSelectionData == Selection.Scissor){
                winner = clientId;
            }
        }
        else{

        }

        return winner;
    }


    public int IsWinner(){
        if (this.winner == NetworkManager.Singleton.LocalClientId){
            return 1;
        }
        else if(this.winner == 99L){
            return 2;
        }
        return 0;
    }

    public void Shutdown(){
        NetworkManager.Singleton.Shutdown();
        Destroy(NetworkManager.Singleton.gameObject);
        Loader.Load(Loader.Scene.StartGame);
    }
}
