﻿using MLAPI;
using MLAPI.Messaging;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class LobbyControl : NetworkBehaviour
{
    [HideInInspector]
    public static bool isHosting;

    [SerializeField]
    private string m_InGameSceneName = "PVPGame";
    public TMP_Text LobbyText;
    private bool m_AllPlayersInLobby;

    private Dictionary<ulong, bool> m_ClientsInLobby;
    private Dictionary<ulong, string> m_ClientsUsername;
    private string m_UserLobbyStatusText;

    /// <summary>
    ///     Awake
    ///     This is one way to kick off a multiplayer session
    /// </summary>
    private void Awake()
    {
        m_ClientsInLobby = new Dictionary<ulong, bool>();
        m_ClientsUsername = new Dictionary<ulong, string>();

        //We added this information to tell us if we are going to host a game or join an the game session
        if (isHosting)
            NetworkManager.Singleton.StartHost(); //Spin up the host
        else
            NetworkManager.Singleton.StartClient(); //Spin up the client

        if (NetworkManager.Singleton.IsListening)
        {
            //Always add ourselves to the list at first
            m_ClientsInLobby.Add(NetworkManager.Singleton.LocalClientId, false);

            if(IsServer)
                m_ClientsUsername.Add(NetworkManager.Singleton.LocalClientId, DBManager.username != null ? DBManager.username : "Guest_" + NetworkManager.Singleton.LocalClientId);

            //If we are hosting, then handle the server side for detecting when clients have connected
            //and when their lobby scenes are finished loading.
            if (IsServer)
            {
                m_AllPlayersInLobby = false;

                //Server will be notified when a client connects
                //NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnectedCallback;
                SceneTransitionHandler.sceneTransitionHandler.OnClientLoadedScene += ClientLoadedScene;
            }

            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnectedCallback;

            if (IsServer)
            {
                //Update our lobby
                GenerateUserStatsForLobby();
            }
        }

        SceneTransitionHandler.sceneTransitionHandler.SetSceneState(SceneTransitionHandler.SceneStates.Lobby);
    }

    private void OnGUI()
    {
        if (LobbyText != null) LobbyText.text = m_UserLobbyStatusText;
    }

    /// <summary>
    ///     GenerateUserStatsForLobby
    ///     Psuedo code for setting player state
    ///     Just updating a text field, this could use a lot of "refactoring"  :)
    /// </summary>
    private void GenerateUserStatsForLobby()
    {
        m_UserLobbyStatusText = string.Empty;
        foreach (var clientLobbyStatus in m_ClientsInLobby)
        {
            //m_UserLobbyStatusText += "Player_" + clientLobbyStatus.Key + "          ";
            m_UserLobbyStatusText += m_ClientsUsername[clientLobbyStatus.Key] + "          ";

            if (clientLobbyStatus.Value)
                m_UserLobbyStatusText += "(Ready)\n";
            else
                m_UserLobbyStatusText += "(Not Ready)\n";
        }
    }

    /// <summary>
    ///     UpdateAndCheckPlayersInLobby
    ///     Checks to see if we have at least 2 or more people to start
    /// </summary>
    private void UpdateAndCheckPlayersInLobby()
    {
        //This is game preference, but I am assuming at least 2 players?
        m_AllPlayersInLobby = m_ClientsInLobby.Count > 0;

        foreach (var clientLobbyStatus in m_ClientsInLobby)
        {
            SendClientReadyStatusUpdatesClientRpc(clientLobbyStatus.Key, clientLobbyStatus.Value, m_ClientsUsername[clientLobbyStatus.Key]);
            if (!NetworkManager.Singleton.ConnectedClients.ContainsKey(clientLobbyStatus.Key))

                //If some clients are still loading into the lobby scene then this is false
                m_AllPlayersInLobby = false;
        }

        CheckForAllPlayersReady();
    }

    /// <summary>
    ///     ClientLoadedScene
    ///     Invoked when a client has loaded this scene
    /// </summary>
    /// <param name="clientId"></param>
    private void ClientLoadedScene(ulong clientId)
    {
        //not working anyway idk why


        //if (IsServer)
        //{
        //    if (!m_ClientsInLobby.ContainsKey(clientId))
        //    {
        //        m_ClientsInLobby.Add(clientId, false);
                
        //    }

        //    //SyncUsernameClientRpc(clientId);

        //    //if (!m_ClientsUsername.ContainsKey(clientId))
        //    //{
        //    //    //m_ClientsUsername.Add(clientId, NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject.transform.GetComponent<PlayerScript>().username.Value);
        //    //    m_ClientsUsername.Add(clientId, GetUsernameClientRpc(clientId));
        //    //    GenerateUserStatsForLobby();
        //    //}

        //    GenerateUserStatsForLobby();
        //    UpdateAndCheckPlayersInLobby();
        //}
    }

    /// <summary>
    ///     OnClientConnectedCallback
    ///     Since we are entering a lobby and MLAPI NetowrkingManager is spawning the player,
    ///     the server can be configured to only listen for connected clients at this stage.
    /// </summary>
    /// <param name="clientId">client that connected</param>
    private void OnClientConnectedCallback(ulong clientId)
    {
        //if (IsServer)
        //{
        //    if (!m_ClientsInLobby.ContainsKey(clientId)) m_ClientsInLobby.Add(clientId, false);

        //    //SyncUsernameClientRpc(clientId);

        //    //if (!m_ClientsUsername.ContainsKey(clientId))
        //    //{
        //    //    //m_ClientsUsername.Add(clientId, NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject.transform.GetComponent<PlayerScript>().username.Value);
        //    //    m_ClientsUsername.Add(clientId, GetUsernameClientRpc(clientId));
        //    //}

        //    //GenerateUserStatsForLobby();

        //    UpdateAndCheckPlayersInLobby();
        //}
        if(!IsServer)
        {
            // send username to server
            SendUsernameToServerServerRpc(clientId, DBManager.username != null ? DBManager.username : "Guest_" + NetworkManager.Singleton.LocalClientId);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SendUsernameToServerServerRpc(ulong clientId, string username)
    {
        if (!m_ClientsInLobby.ContainsKey(clientId)) m_ClientsInLobby.Add(clientId, false);

        if (!m_ClientsUsername.ContainsKey(clientId))
            m_ClientsUsername.Add(clientId, username);
        else
            m_ClientsUsername[clientId] = username;

        GenerateUserStatsForLobby();
        UpdateAndCheckPlayersInLobby();
    }

    /// <summary>
    ///     SendClientReadyStatusUpdatesClientRpc
    ///     Sent from the server to the client when a player's status is updated.
    ///     This also populates the connected clients' (excluding host) player state in the lobby
    /// </summary>
    /// <param name="clientId"></param>
    /// <param name="isReady"></param>
    [ClientRpc]
    private void SendClientReadyStatusUpdatesClientRpc(ulong clientId, bool isReady, string username)
    {
        if (!IsServer)
        {
            if (!m_ClientsInLobby.ContainsKey(clientId))
                m_ClientsInLobby.Add(clientId, isReady);
            else
                m_ClientsInLobby[clientId] = isReady;


            if (!m_ClientsUsername.ContainsKey(clientId))
                m_ClientsUsername.Add(clientId, username);
            else
                m_ClientsUsername[clientId] = username;


            GenerateUserStatsForLobby();
        }
    }

    /// <summary>
    ///     CheckForAllPlayersReady
    ///     Checks to see if all players are ready, and if so launches the game
    /// </summary>
    private void CheckForAllPlayersReady()
    {
        if (m_AllPlayersInLobby)
        {
            var allPlayersAreReady = true;
            foreach (var clientLobbyStatus in m_ClientsInLobby)
                if (!clientLobbyStatus.Value)

                    //If some clients are still loading into the lobby scene then this is false
                    allPlayersAreReady = false;

            //Only if all players are ready
            if (allPlayersAreReady)
            {
                //Remove our client connected callback
                NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnectedCallback;

                //Remove our scene loaded callback
                SceneTransitionHandler.sceneTransitionHandler.OnClientLoadedScene -= ClientLoadedScene;

                //Transition to the ingame scene
                SceneTransitionHandler.sceneTransitionHandler.SwitchScene(m_InGameSceneName);
            }
        }
    }

    /// <summary>
    ///     PlayerIsReady
    ///     Tied to the Ready button in the InvadersLobby scene
    /// </summary>
    public void PlayerIsReady()
    {
        if (IsServer)
        {
            m_ClientsInLobby[NetworkManager.Singleton.ServerClientId] = true;
            UpdateAndCheckPlayersInLobby();
        }
        else
        {
            m_ClientsInLobby[NetworkManager.Singleton.LocalClientId] = true;
            OnClientIsReadyServerRpc(NetworkManager.Singleton.LocalClientId);
        }

        GenerateUserStatsForLobby();
    }

    /// <summary>
    ///     OnClientIsReadyServerRpc
    ///     Sent to the server when the player clicks the ready button
    /// </summary>
    /// <param name="clientid">clientId that is ready</param>
    [ServerRpc(RequireOwnership = false)]
    private void OnClientIsReadyServerRpc(ulong clientid)
    {
        if (m_ClientsInLobby.ContainsKey(clientid))
        {
            m_ClientsInLobby[clientid] = true;
            UpdateAndCheckPlayersInLobby();
            GenerateUserStatsForLobby();
        }
    }
}
