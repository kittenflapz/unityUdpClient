/* Catt Symonds
 * 101209214
 * Multiplayer Systems, George Brown College
 * Last Updated: 26/09/2020 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;
using System.Net.Sockets;
using System.Net;

public class NetworkMan : MonoBehaviour
{
    public UdpClient udp; // For talking to the server.
    public GameObject playerPrefab; // The cube that will represent each player.
    public List<PlayerCube> playersInGame; // A list of the CUBES (representing players) currently in the game.
    public string myAddress;  // My IP and port. We'll use this later to make sure there's no funny business.
    public List<string> playersToSpawn; // We have to store a list of players that we SHOULD spawn but HAVEN'T yet here, 
    // so that our SpawnPlayers() function knows when it's time to do its thing.

    // Start is called before the first frame update
    void Start()
    {
        udp = new UdpClient();
        
        udp.Connect("ec2-3-137-149-42.us-east-2.compute.amazonaws.com",12345); // Connect to the server at this address with this IP.

        Byte[] sendBytes = Encoding.ASCII.GetBytes("connect"); // Make a message saying we'd like to connect, please.
      
        udp.Send(sendBytes, sendBytes.Length); // Send it.

        udp.BeginReceive(new AsyncCallback(OnReceived), udp); // Receive messages.

        InvokeRepeating("HeartBeat", 1, 1); // Call our HeartBeat() function in one second, then keep doing it every one second.

        InvokeRepeating("UpdateMyPosition", 1, 0.03f); // Call our UpdateMyPosition() function in one second, then keep doing it 30 times every second.
    }

    void OnDestroy(){
        udp.Dispose(); // Clean up.
    }


    public enum commands{ // We'll use this enum to know what the server is asking us to do.
        NEW_CLIENT,
        UPDATE,
        DROPPED_CLIENT,
        ALREADY_HERE_PLAYERS
    };
    
    [Serializable]
    public class Message{ // This is what we'll turn whatever the server sends into.
        public commands cmd;
    }

    [Serializable]
    public class PositionUpdater // We use this to send our own position to the server, so it knows where we are and can tell the other clients.
    {
        public Vector3 position;
    }

    [Serializable]
    public class Player{ // Different to PlayerCubes! We'll make some lists of objects of this class later, in GameState, DroppedPlayers, and AlreadyHerePlayerList
        public string id; // Their IP and port.

        [Serializable] // If this isn't serializable, you're in for a bad time.
        public struct receivedPosition{ // We'll use this in about five lines.
            public float x;
            public float y;
            public float z;
        }
        public receivedPosition position; // This is where we'll keep the positions for all the cubes that aren't us so we can move them to the right place on screen.

    }



    [Serializable]
    public class DroppedPlayers // Players that have disconnected.
    {
        public string id;
        public Player[] players;
    }

    [Serializable]
    public class GameState 
    {
        public Player[] players; // Players currently online ACCORDING TO THE SERVER - different than PlayerCubes!
    }

    [Serializable]
    public class AlreadyHerePlayerList // We only use this when we're a new player joining and want to know who else is already at the party, so we can make cubes for them.
    {
        public Player[] players;
    }

    [Serializable]
    public class NewPlayer // For spawning a new player's cube when they arrive at the party.
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
            switch(latestMessage.cmd){ // Work out what kind of command the server sent - what does it want from us?
                case commands.NEW_CLIENT: // A new client is joining!
                    NewPlayer newPlayer = JsonUtility.FromJson<NewPlayer>(returnData); // Make a NewPlayer object for this new player so we can make them a nice cube.
                    Debug.Log(returnData);
                    playersToSpawn.Add(newPlayer.player.id); // If I'M the new client, I should be the first thing I spawn
                    if (myAddress == "") // So my address won't yet be set
                    {
                        myAddress = newPlayer.player.id; // So set it - we'll use this later to avoid any tomfoolery
                    }
                    break;
                case commands.UPDATE: // The server is just sending an update - this is where all the information about the OTHER cubes in our scene, 
                    // not us, is sent to us, so we can put their cubes in the right place.
                    latestGameState = JsonUtility.FromJson<GameState>(returnData); // latestGameState stores all the current players and their positions, so update that.
                    UpdatePlayers(); // Move all the cubes around!
                    Debug.Log(returnData);
                    break;
                case commands.DROPPED_CLIENT: // The server is telling us that someone has left the cube party.
                    DroppedPlayers droppedPlayer = JsonUtility.FromJson<DroppedPlayers>(returnData); // Get the ID of the dropped player
                    DestroyPlayers(droppedPlayer.id); // Get rid of their cube.
                    Debug.Log(returnData);
                    break;
                case commands.ALREADY_HERE_PLAYERS: // This command should only come to the newly connected client - it's the server helpfully telling us who 
                    // is already here, so we can spawn their cubes.
                    AlreadyHerePlayerList alreadyHerePlayers = JsonUtility.FromJson<AlreadyHerePlayerList>(returnData); // Populate the list.
                    foreach (Player player in alreadyHerePlayers.players)
                    {
                        playersToSpawn.Add(player.id); // Spawn all the other cubes!
                    }
                    Debug.Log(returnData);
                    break;
                default:
                    Debug.Log("Error"); // Something went wrong.
                    break;
            }
        }
        catch (Exception e){
            Debug.Log(e.ToString()); // Something went really wrong.
        }
        
        // schedule the next receive operation once reading is done:
        socket.BeginReceive(new AsyncCallback(OnReceived), socket);
    }

    void SpawnWaitingPlayers()
    {
        if (playersToSpawn.Count > 0) // If there are players in the waiting list...
        {
            for (int i = 0; i < playersToSpawn.Count; i++) // Go through the waiting list
            {
                SpawnPlayer(playersToSpawn[i]); // Spawn each player.
            }
            playersToSpawn.Clear(); // Reset the list.
            playersToSpawn.TrimExcess(); // Really reset it.
        }
    }

    void SpawnPlayer(string _id) // Where new cubes are spawned.
    { 
        foreach(PlayerCube playerCube in playersInGame)
        {
            if (playerCube.networkID == _id) // If there's already a cube for me...
            {
                return; // Don't bother, and get out before the spawning stuff happens.
            }
        }

        Vector3 randomPos = new Vector3(UnityEngine.Random.Range(-3f, 3f), 
            UnityEngine.Random.Range(-3f, 3f), UnityEngine.Random.Range(-3f, 3f)); // Come up with a random spot.
        GameObject newPlayerCube = Instantiate(playerPrefab, randomPos, Quaternion.identity); //  Spawn the cube there.
        newPlayerCube.GetComponent<PlayerCube>().networkID = _id; // Set the ID of the cube we just spawned.
        playersInGame.Add(newPlayerCube.GetComponent<PlayerCube>()); // Add it to our list o' cubes.
    }

    void UpdatePlayers() // Updating all the cube positions except my own.
    {
        for (int i = 0; i < latestGameState.players.Length; i++) // Go through all the players the server says we have
        {
            for (int j = 0; j < playersInGame.Count; j++) // And go through all the player cubes we have in game already
            {
                if (latestGameState.players[i].id == playersInGame[j].networkID) // If the player ID and the cube ID match
                {
                    if (latestGameState.players[i].id != myAddress) // And it's NOT me (my position is updated in my own Input section of PlayerCube.cs)
                    {
                        // Send the position the server says these other cubes have to their cube objects.
                        playersInGame[j].newTransformPos =
                          new Vector3(latestGameState.players[i].position.x, latestGameState.players[i].position.y, latestGameState.players[i].position.z); 
                    }
                }
            }
        }
    }

    void DestroyPlayers(string _id) // This is where we destroy cubes. 
    {
        foreach (PlayerCube playerCube in playersInGame) // Go through all the cubes we have in the game currently.
        {
            if (playerCube.networkID == _id) // If this is the droid we're looking for (based on the _id sent)
            {
                playerCube.markedForDestruction = true; // Tell the cube to delete itself on its next Update.
            }
        }
    }

    void HeartBeat() // Just telling the server we're still alive.
    {
        Byte[] sendBytes = Encoding.ASCII.GetBytes("heartbeat");
        udp.Send(sendBytes, sendBytes.Length);
    }

    void UpdateMyPosition()
    {
        PositionUpdater message = new PositionUpdater(); // Clean up the message ready to start again.

        for (int i = 0; i < playersInGame.Count; i++) // Go through all the players and find which one is me.
        {
            if (playersInGame[i].networkID == myAddress) // If it is me...
            {
                message.position.x = playersInGame[i].transform.position.x; // Store my position details in a PositionUpdater 
                message.position.y = playersInGame[i].transform.position.y; // Store my position details in a PositionUpdater 
                message.position.z = playersInGame[i].transform.position.z; // Store my position details in a PositionUpdater 
                Byte[] sendBytes = Encoding.ASCII.GetBytes(JsonUtility.ToJson(message)); // Encode that PositionUpdater into a json
                udp.Send(sendBytes, sendBytes.Length); // Send it to the server.
            }
        }
    }

    void Update()
    {
        SpawnWaitingPlayers();
    }
}