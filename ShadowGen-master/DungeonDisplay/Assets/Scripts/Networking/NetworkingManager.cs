using System.Collections;
using System.Collections.Generic;
using Steamworks;
using Steamworks.Data;
using UnityEngine;
using System.Net;
using System.Threading.Tasks;
using UnityEngine.Assertions;
using System.Linq;
using System;

public enum NetworkingState
{
    NoConnection,
    IsServer,
    IsClient,
}

public class NetworkingManager : MonoBehaviour
{
    public Lobby gameLobby;
    public DDGameServer gameServer;
    public DDGameClient gameClient;

    public bool host = true;
    public NetworkingState networkingState = NetworkingState.NoConnection;

    #region Singlton
    public static NetworkingManager Instance { get; private set; }
    #endregion

    private void Awake()
    {
        UnityEngine.Profiling.Profiler.maxUsedMemory = UnityEngine.Profiling.Profiler.maxUsedMemory * 16;

        networkingState = NetworkingState.NoConnection;
        host = true;

        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(this.gameObject);

        //You are attempting to join your friends server
        SteamFriends.OnGameLobbyJoinRequested += JoinOnOtherClientRequest;
    }

    private async void JoinOnOtherClientRequest(Lobby lobby, SteamId steamId)
    {
        host = false;
        //If - this is not a part of a server
        if (networkingState == NetworkingState.NoConnection)
        {
            networkingState = NetworkingState.IsClient;
            try
            {
                //Add yourself to that lobby
                gameLobby = lobby;
                var lob = await lobby.Join();
                if (lob == RoomEnter.Success)
                {
                    //Add your client to the server
                    gameClient = SteamNetworkingSockets.ConnectRelay<DDGameClient>(steamId, int.Parse(lobby.GetData("GameVirtualIP")));
                    var client = gameClient.RunAsync();
                }
            }
            catch
            {
                networkingState = NetworkingState.NoConnection;
                host = true;
            }
        }
        else
        {
            networkingState = NetworkingState.NoConnection;
            host = true;
        }
    }

    public void InviteToCurrentLobbyButton()
    {
        if(networkingState != NetworkingState.NoConnection)
        {
            SteamFriends.OpenGameInviteOverlay(gameLobby.Id);
        }
        else
        {
            InfoLogCanvasScript.SendInfoMessage("Not in a server!", UnityEngine.Color.red);
        }
    }

    public async Task ServerButtonPressed()
    {
        if (networkingState == NetworkingState.IsServer)
        {
            DeatachFromOnline();
        }
        else if(networkingState == NetworkingState.NoConnection)
        {
            //Create lobby
            int appliedVirtualIP = 1;
            InfoLogCanvasScript.SendInfoMessage("Requesting lobbys... ", UnityEngine.Color.black);
            var lobbys = await SteamMatchmaking.LobbyList.RequestAsync();
            
            if(lobbys == null)
            {
                InfoLogCanvasScript.SendInfoMessage("No Lobbys Yet", UnityEngine.Color.black);
            }
            else
            {
                InfoLogCanvasScript.SendInfoMessage(lobbys.Length + " Lobbys Found", UnityEngine.Color.black);
                appliedVirtualIP = lobbys.Length + 1;
            }

            await CreateLobby();

            gameLobby.SetData("GameVirtualIP", appliedVirtualIP.ToString());

            await gameLobby.Join();

            //Create server
            gameServer = SteamNetworkingSockets.CreateRelaySocket<DDGameServer>(appliedVirtualIP);
            var server = gameServer.RunAsync();

            await Task.Delay(2000);

            //Create a personal client
            gameClient = SteamNetworkingSockets.ConnectRelay<DDGameClient>(SteamClient.SteamId, appliedVirtualIP);
            var client = gameClient.RunAsync();

            InfoLogCanvasScript.SendInfoMessage("Server Created at Virtual IP: " + appliedVirtualIP, UnityEngine.Color.black);

            host = true;

            await Task.Delay(2000);
            networkingState = NetworkingState.IsServer;

            //CHANGE SERVER BUTTON
        }
    }

    private void OnApplicationQuit()
    {
        DeatachFromOnline();
    }

    public void DeatachFromOnline()
    {
        if (networkingState == NetworkingState.IsClient)
        {
            gameLobby.Leave();
            host = true;
            gameClient.Close();
            networkingState = NetworkingState.NoConnection;

            InfoLogCanvasScript.SendInfoMessage("Client Disconnected", UnityEngine.Color.black);
        }
        else if (networkingState == NetworkingState.IsServer)
        {
            try
            {
                //Shutdown the server
                foreach(Connection c in gameServer.Connected) { c.Close();}
                gameServer.Close();
                networkingState = NetworkingState.NoConnection;

                //Leave the lobby
                gameLobby.Leave();
                host = true;

                InfoLogCanvasScript.SendInfoMessage("Server Shutdown", UnityEngine.Color.black);
            }
            catch
            {

            }
        }
    }

    private async Task CreateLobby()
    {
        var lobbyr = await SteamMatchmaking.CreateLobbyAsync(32);
        if (!lobbyr.HasValue)
        {
            return;
        }
        var lobby = lobbyr.Value;
        lobby.SetPublic();
        gameLobby = lobby;
    }
}
