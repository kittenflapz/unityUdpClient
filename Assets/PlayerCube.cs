/* Catt Symonds
 * 101209214
 * Multiplayer Systems, George Brown College
 * Last Updated: 26/09/2020 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCube : MonoBehaviour // The representation of the player in the game.
{
    public string networkID; // The ID.
    public bool markedForDestruction; // Can't destroy directly from anywhere but Update(), so makes sense to do it in this GameObject's Update().
    public Vector3 newTransformPos; // Can't get the GameObject's transform anywhere but Update(), so makes sense to do translations here using a temp variable.
    public NetworkMan networkMan; // A reference to the network manager, used later for making sure we don't move any cubes that aren't ours.
    public float speed; // Speed.
  
    // Start is called before the first frame update
    void Start()
    {
        markedForDestruction = false; // Obviously.
        networkMan = GameObject.Find("NetworkMan").GetComponent<NetworkMan>(); // There's only one in the scene, so find it and set it.
        newTransformPos = Vector3.zero; // Just to be safe.
        speed = 5.0f; // You can set this in the inspector if you like a speedier cube.
    }

    // Update is called once per frame
    void Update()
    {
        if (markedForDestruction) // True when the player is no longer sending heartbeats to the server, so has disconnected.
        {
            Destroy(gameObject); // No player? No cube.
        }

        if (networkID != networkMan.myAddress) // For every cube that isn't me.
        {
            transform.position = newTransformPos; // Update their positions.
            return; // Then get out! Everything that happens after this is input that should ONLY happen for the client controlling this cube.
        }

        /**********INPUT***********/

        if (Input.GetKey(KeyCode.UpArrow))
        {
            transform.Translate(Vector3.forward * Time.deltaTime * speed);
        }

        if (Input.GetKey(KeyCode.DownArrow))
        {
            transform.Translate(-Vector3.forward * Time.deltaTime * speed);
        }

        if (Input.GetKey(KeyCode.LeftArrow))
        {
            transform.Translate(Vector3.left * Time.deltaTime * speed);
        }

        if (Input.GetKey(KeyCode.RightArrow))
        {
            transform.Translate(-Vector3.left * Time.deltaTime * speed);
        }
    }

  
}
