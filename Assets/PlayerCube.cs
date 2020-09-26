using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCube : MonoBehaviour
{
    public string networkID;
    public Color cubeColor;
    public bool markedForDestruction;
    public NetworkMan networkMan;
    public float speed;

    public Vector3 newTransformPos;
    // Start is called before the first frame update
    void Start()
    {
        markedForDestruction = false;
        networkMan = GameObject.Find("NetworkMan").GetComponent<NetworkMan>();
        newTransformPos = Vector3.zero;
        speed = 5.0f;
    }

    // Update is called once per frame
    void Update()
    {
        //Debug.Log("color: " + cubeColor);
        GetComponent<Renderer>().material.color = cubeColor;

        if (markedForDestruction)
        {
            Destroy(gameObject);
        }

        if (networkID != networkMan.myAddress) // don't move anyone but me!
        {
            transform.position = newTransformPos;
            return;
        }


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
