using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;
using System.Net.Sockets;
using System.Net;

public class NetworkMan : MonoBehaviour
{
    public UdpClient udp;
    public GameObject playerPrefab;
    public List<PlayerCube> playersInGame;
    public string myAddress;

    // Start is called before the first frame update
    void Start()
    {
        udp = new UdpClient();
        
        udp.Connect("ec2-3-137-149-42.us-east-2.compute.amazonaws.com",12345);

        Byte[] sendBytes = Encoding.ASCII.GetBytes("connect");
      
        udp.Send(sendBytes, sendBytes.Length);

        udp.BeginReceive(new AsyncCallback(OnReceived), udp);

        InvokeRepeating("HeartBeat", 1, 1);
    }

    void OnDestroy(){
        udp.Dispose();
    }


    public enum commands{
        NEW_CLIENT,
        UPDATE
    };
    
    [Serializable]
    public class Message{
        public commands cmd;
    }
    
    [Serializable]
    public class Player{
        public string id;

        [Serializable]
        public struct receivedColor{
            public float R;
            public float G;
            public float B;
        }
        public receivedColor color;
    }

    [Serializable]
    public class NewPlayer{
    }

    [Serializable]
    public class GameState{
        public Player[] players;
    }

    public Message latestMessage;
    public GameState latestGameState;
    void OnReceived(IAsyncResult result){
        // this is what had been passed into BeginReceive as the second parameter:
        UdpClient socket = result.AsyncState as UdpClient;
        
        // points towards whoever had sent the message:
        IPEndPoint source = new IPEndPoint(0, 0);

        // get the actual message and fill out the source:
        byte[] message = socket.EndReceive(result, ref source);
        
        // do what you'd like with `message` here:
        string returnData = Encoding.ASCII.GetString(message);
        Debug.Log("Got this: " + returnData);
        
        latestMessage = JsonUtility.FromJson<Message>(returnData);
        try{
            switch(latestMessage.cmd){
                case commands.NEW_CLIENT:
                    // When a new player is connected, the client adds the details of this player into a list of currently connected players.
                    break;
                case commands.UPDATE:
                    latestGameState = JsonUtility.FromJson<GameState>(returnData);
                    break;
                default:
                    Debug.Log("Error");
                    break;
            }
        }
        catch (Exception e){
            Debug.Log(e.ToString());
        }
        
        // schedule the next receive operation once reading is done:
        socket.BeginReceive(new AsyncCallback(OnReceived), socket);
    }

    void SpawnPlayers()
    {
        if (playersInGame.Count == 0)
        {
            foreach (Player player in latestGameState.players) // go through each player the server says we have
            {
                Vector3 randomPos = new Vector3(UnityEngine.Random.Range(-3f, 3f), UnityEngine.Random.Range(-3f, 3f), UnityEngine.Random.Range(-3f, 3f));
                GameObject newPlayer = Instantiate(playerPrefab, randomPos, Quaternion.identity);
                newPlayer.GetComponent<PlayerCube>().networkID = player.id;
                playersInGame.Add(newPlayer.GetComponent<PlayerCube>());
            }
        }

        if (playersInGame.Count >= latestGameState.players.Length) // If we have the right number of player cubes in the game
        {
            // don't do anything
        }
        else
        {
            foreach (Player player in latestGameState.players) // go through each player the server says we have
            {

                foreach (PlayerCube playerCube in playersInGame) // go through each cube we have made
                {
                    if (player.id != playerCube.networkID) // if there isn't already a cube with this network id, spawn it
                    {
                        Vector3 randomPos = new Vector3(UnityEngine.Random.Range(-3f, 3f), UnityEngine.Random.Range(-3f, 3f), UnityEngine.Random.Range(-3f, 3f));
                        GameObject newPlayer = Instantiate(playerPrefab, randomPos, Quaternion.identity);
                        newPlayer.GetComponent<PlayerCube>().networkID = player.id;
                        playersInGame.Add(newPlayer.GetComponent<PlayerCube>());
                    }
                }
            }
        }
    }

    void UpdatePlayers()
    {
        foreach (Player player in latestGameState.players) // go through each player the server says we have
        {
            foreach (PlayerCube playerCube in playersInGame) // go through each cube we have made
            {
                if (player.id == playerCube.networkID) // if the player id and the cube id match
                {
                    playerCube.cubeColor = new Color(player.color.R, player.color.G, player.color.B);
                }
            }
        }
    }

    void DestroyPlayers(){

    }
    
    void HeartBeat(){
        Byte[] sendBytes = Encoding.ASCII.GetBytes("heartbeat");
        udp.Send(sendBytes, sendBytes.Length);
    }

    void Update(){
        SpawnPlayers();
        UpdatePlayers();
        DestroyPlayers();
    }
}