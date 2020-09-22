using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCube : MonoBehaviour
{
    public string networkID;
    public Color cubeColor;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        //Debug.Log("color: " + cubeColor);
        GetComponent<Renderer>().material.color = cubeColor;
    }
}
