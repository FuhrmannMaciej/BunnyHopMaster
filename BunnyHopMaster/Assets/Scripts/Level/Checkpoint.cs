﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    [SerializeField]
    public int level;
    
    public Material completedMaterial;
    public MeshRenderer myRenderer;

    private void Start()
    {
        myRenderer = GetComponent<MeshRenderer>();
    }

    public void SetCompleted()
    {
        Debug.Log("Reached checkpoint: " + level);
        myRenderer.sharedMaterial = completedMaterial;
    }
}
