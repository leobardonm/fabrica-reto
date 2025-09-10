using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldInfo : MonoBehaviour
{
    private static WorldInfo instance;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;  // Store this instance
            DontDestroyOnLoad(gameObject);  // Make it persist
        }
        else
        {
            Destroy(gameObject);  // Prevent duplicates
        }
    }
    /**/
    public float step_time = 0.5f;           
    public bool loop = false;      
    
}

//command F: WorldInfo WorldInfo = FindObjectOfType<WorldInfo>();
