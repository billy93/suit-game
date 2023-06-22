using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports;
using Unity.Netcode.Transports.UTP;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;
using Unity.Networking.Transport.Relay;
public class AutoLobbyManager : NetworkBehaviour
{

    public static AutoLobbyManager Instance { get; private set; }
    
    private string playerName;

    public const string KEY_PLAYER_NAME = "PlayerName";
    public const string KEY_PLAYER_CHARACTER = "Character";
    public const string KEY_GAME_MODE = "GameMode";
    public const string KEY_RELAY_CODE = "RelayCode";
    private Lobby joinedLobby;
    
    private float heartbeatTimer;

    
    private void Awake() {
        if (Instance == null)
        {
            Debug.Log("INSTANCE NULL");
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Debug.Log("INSTANCE ADA");
            Destroy(gameObject);
        }
    }

    private void Update() {
        //HandleRefreshLobbyList(); // Disabled Auto Refresh for testing with multiple builds
        HandleLobbyHeartbeat();
        // HandleLobbyPolling();
    }

    public async void Authenticate(string playerName) {
        try{
            this.playerName = playerName;
            InitializationOptions initializationOptions = new InitializationOptions();
            initializationOptions.SetProfile(playerName);
            // initializationOptions.SetProfile(UnityEngine.Random.Range(0, 10000).ToString());
            await UnityServices.InitializeAsync(initializationOptions);

            AuthenticationService.Instance.SignedIn += async() => {
                Debug.Log("Signed in! " + AuthenticationService.Instance.PlayerId);

                // if no host, auto create lobby and wait other player to join
                List<Lobby> listLobby = await GetLobbyList();
                Debug.Log("LOBBY SIZE : "+listLobby.Count);
                if(listLobby.Count > 0){
                    // join game
                    await JoinLobby(listLobby[0]);
                }
                else{
                    // create new game 
                    await CreateLobby("Game1", 2, false);
                }
            };

            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }catch(Exception e){
            Debug.Log(e);
            
            List<Lobby> listLobby = await GetLobbyList();
            Debug.Log("LOBBY SIZE : "+listLobby.Count);
            if(listLobby.Count > 0){
                // join game
                await JoinLobby(listLobby[0]);
            }
            else{
                // create new game 
                await CreateLobby("Game1", 2, false);
            }
        }
    }

    public async Task<List<Lobby>> GetLobbyList() {
        try {
            QueryLobbiesOptions options = new QueryLobbiesOptions();
            options.Count = 25;

            // Filter for open lobbies only
            options.Filters = new List<QueryFilter> {
                new QueryFilter(
                    field: QueryFilter.FieldOptions.AvailableSlots,
                    op: QueryFilter.OpOptions.GT,
                    value: "0")
            };

            // Order by newest lobbies first
            options.Order = new List<QueryOrder> {
                new QueryOrder(
                    asc: false,
                    field: QueryOrder.FieldOptions.Created)
            };

            QueryResponse lobbyListQueryResponse = await Lobbies.Instance.QueryLobbiesAsync();
            List<Lobby> lobbyList = lobbyListQueryResponse.Results;
            return lobbyList;
        } catch (LobbyServiceException e) {
            Debug.Log(e);
        }

        return new List<Lobby>();
    }

    public async Task CreateLobby(string lobbyName, int maxPlayers, bool isPrivate) {
        Player player = GetPlayer();

        Allocation allocation = await RelayService.Instance.CreateAllocationAsync(2);
        string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
        Debug.Log("LOBBY CREATED BY HOST WITH RELAY ID : "+joinCode);
        CreateLobbyOptions options = new CreateLobbyOptions {
            Player = player,
            IsPrivate = isPrivate,
            Data = new Dictionary<string, DataObject> {
                { KEY_RELAY_CODE, new DataObject(DataObject.VisibilityOptions.Member, joinCode) }
            }
        };

        Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(new RelayServerData(allocation, "dtls"));

        joinedLobby = lobby;

        Debug.Log("Created Lobby " + lobby.Name);
        // NetworkManager.Singleton.StartHost();  
        StartHost(); 
    }

    private async Task<JoinAllocation> JoinRelay(string joinCode) {
        try {
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
            return joinAllocation;
        } catch (RelayServiceException e) {
            Debug.Log("JoinRelay : "+e);
            return default;
        }
    }

    public async Task JoinLobby(Lobby lobby) {
        try{
            Player player = GetPlayer();

            joinedLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobby.Id, new JoinLobbyByIdOptions {
                Player = player
            });
            string joinRelayCode = joinedLobby.Data[KEY_RELAY_CODE].Value;
            Debug.Log("CLIENT TRY TO JOIN USING RELAY CODE : "+joinRelayCode);
            JoinAllocation allocation = await JoinRelay(joinRelayCode);

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(new RelayServerData(allocation, "dtls"));
            NetworkManager.Singleton.StartClient();

        }catch(RelayServiceException e){
            Debug.Log("Relay error : "+e);
        }

    }

    public bool IsLobbyHost() {
        return joinedLobby != null && joinedLobby.HostId == AuthenticationService.Instance.PlayerId;
    }

    private async void HandleLobbyHeartbeat() {
        if (IsLobbyHost()) {
            heartbeatTimer -= Time.deltaTime;
            if (heartbeatTimer < 0f) {
                float heartbeatTimerMax = 15f;
                heartbeatTimer = heartbeatTimerMax;

                Debug.Log("Heartbeat");
                await LobbyService.Instance.SendHeartbeatPingAsync(joinedLobby.Id);
            }
        }
    }

    private Player GetPlayer() {
        return new Player(AuthenticationService.Instance.PlayerId, null, new Dictionary<string, PlayerDataObject> {
            { KEY_PLAYER_NAME, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, playerName) },
        });
    }

    public void StartHost() {
        // NetworkManager.Singleton.ConnectionApprovalCallback += NetworkManager_ConnectionApprovalCallback;
        NetworkManager.Singleton.OnClientConnectedCallback += NetworkManager_OnClientConnectedCallback;
        NetworkManager.Singleton.OnClientDisconnectCallback += NetworkManager_Server_OnClientDisconnectCallback;
        NetworkManager.Singleton.StartHost();
    }

    private void NetworkManager_Server_OnClientDisconnectCallback(ulong obj)
    {
    }

    public async Task DeleteLobby() {
        if (joinedLobby != null) {
            try {
                await LobbyService.Instance.DeleteLobbyAsync(joinedLobby.Id);

                joinedLobby = null;
            } catch (LobbyServiceException e) {
                Debug.Log(e);
            }
        }
    }
    private async void NetworkManager_OnClientConnectedCallback(ulong obj)
    {
        if(obj != NetworkManager.Singleton.LocalClientId){
            await DeleteLobby();            
            Loader.LoadNetwork(Loader.Scene.SuitGameScene);
        }
    }
}
