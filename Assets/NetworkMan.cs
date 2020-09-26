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
    public List<string> playersToSpawn;

    // Start is called before the first frame update
    void Start()
    {
        udp = new UdpClient();
        
        udp.Connect("ec2-3-137-149-42.us-east-2.compute.amazonaws.com",12345);

        Byte[] sendBytes = Encoding.ASCII.GetBytes("connect");
      
        udp.Send(sendBytes, sendBytes.Length);

        udp.BeginReceive(new AsyncCallback(OnReceived), udp);

        InvokeRepeating("HeartBeat", 1, 1);

        InvokeRepeating("UpdateMyPosition", 1, 0.03f);
    }

    void OnDestroy(){
        udp.Dispose();
    }


    public enum commands{
        NEW_CLIENT,
        UPDATE,
        DROPPED_CLIENT,
        ALREADY_HERE_PLAYERS
    };
    
    [Serializable]
    public class Message{
        public commands cmd;
    }

    [Serializable]
    public class PositionUpdater
    {
        public Vector3 position;
    }

    [Serializable]
    public class Player{
        public string id;

        [Serializable]
        public struct receivedPosition{
            public float x;
            public float y;
            public float z;
        }
        public receivedPosition position;

    }



    [Serializable]
    public class DroppedPlayers
    {
        public string id;
        public Player[] players;
    }

    [Serializable]
    public class GameState
    {
        public Player[] players;
    }

    [Serializable]
    public class AlreadyHerePlayerList
    {
        public Player[] players;
    }

    [Serializable]
    public class NewPlayer
    {
        public Player player;
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
        //Debug.Log("Got this: " + returnData);

        latestMessage = JsonUtility.FromJson<Message>(returnData);
        try{
            switch(latestMessage.cmd){
                case commands.NEW_CLIENT:
                    NewPlayer newPlayer = JsonUtility.FromJson<NewPlayer>(returnData);
                    Debug.Log(returnData);
                    playersToSpawn.Add(newPlayer.player.id); // If I'M the new client, I should be the first thing I spawn
                    if (myAddress == "") // so my address won't yet be set
                    {
                        myAddress = newPlayer.player.id; // so set it
                    }
                    break;
                case commands.UPDATE:
                    latestGameState = JsonUtility.FromJson<GameState>(returnData);
                    UpdatePlayers();
                    Debug.Log(returnData);
                    break;
                case commands.DROPPED_CLIENT:
                    DroppedPlayers droppedPlayer = JsonUtility.FromJson<DroppedPlayers>(returnData); // get the id of the dropped player
                    DestroyPlayers(droppedPlayer.id);
                    Debug.Log(returnData);
                    break;
                case commands.ALREADY_HERE_PLAYERS: // this command should only come to the newly connected client
                    AlreadyHerePlayerList alreadyHerePlayers = JsonUtility.FromJson<AlreadyHerePlayerList>(returnData);
                    foreach (Player player in alreadyHerePlayers.players)
                    {
                        playersToSpawn.Add(player.id);
                    }
                    Debug.Log(returnData);
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

    void SpawnWaitingPlayers()
    {
        if (playersToSpawn.Count > 0) // if there are players in the waiting list
        {
            for (int i = 0; i < playersToSpawn.Count; i++) // go through the list and spawn each one
            {
                SpawnPlayer(playersToSpawn[i]);
            }
            playersToSpawn.Clear(); // reset the list
            playersToSpawn.TrimExcess(); // really reset it
        }
    }

    void SpawnPlayer(string _id)
    { 
        foreach(PlayerCube playerCube in playersInGame)
        {
            if (playerCube.networkID == _id) // if there's already a cube for me
            {
                return; // don't bother
            }
        }

        Vector3 randomPos = new Vector3(UnityEngine.Random.Range(-3f, 3f), UnityEngine.Random.Range(-3f, 3f), UnityEngine.Random.Range(-3f, 3f));
        GameObject newPlayerCube = Instantiate(playerPrefab, randomPos, Quaternion.identity);
        newPlayerCube.GetComponent<PlayerCube>().networkID = _id;
        playersInGame.Add(newPlayerCube.GetComponent<PlayerCube>());
    }

    void UpdatePlayers()
    {
        for (int i = 0; i < latestGameState.players.Length; i++)
        {
            for (int j = 0; j < playersInGame.Count; j++)
            {
                if (latestGameState.players[i].id == playersInGame[j].networkID) // if the player id and the cube id match
                {
                    if (latestGameState.players[i].id != myAddress)
                    {
                        playersInGame[j].newTransformPos =
                          new Vector3(latestGameState.players[i].position.x, latestGameState.players[i].position.y, latestGameState.players[i].position.z);
                    }
                }
            }
        }
    }

    void DestroyPlayers(string _id)
    {
        foreach (PlayerCube playerCube in playersInGame)
        {
            if (playerCube.networkID == _id) // if there's already a cube for me
            {
                playerCube.markedForDestruction = true;
            }
        }
    }

    void HeartBeat()
    {
        Byte[] sendBytes = Encoding.ASCII.GetBytes("heartbeat");
        udp.Send(sendBytes, sendBytes.Length);
    }

    void UpdateMyPosition()
    {
        PositionUpdater message = new PositionUpdater();

        for (int i = 0; i < playersInGame.Count; i++) // go through all the players and find which one is me
        {
            if (playersInGame[i].networkID == myAddress) // if it is me
            {
                message.position.x = playersInGame[i].transform.position.x; // store my position details in a PositionUpdater 
                message.position.y = playersInGame[i].transform.position.y;
                message.position.z = playersInGame[i].transform.position.z;
                Byte[] sendBytes = Encoding.ASCII.GetBytes(JsonUtility.ToJson(message)); // encode that PositionUpdater into a json
                udp.Send(sendBytes, sendBytes.Length); // send it to the server
            }
        }
    }

    void Update()
    {
        SpawnWaitingPlayers();
    }
}