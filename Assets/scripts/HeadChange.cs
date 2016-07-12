﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class HeadChange : MonoBehaviour {

    /*
    public string ChangeToRandomHead()
    {
        if(heads.Count == 0)
        {
            Object[] headstemp = Resources.LoadAll("heads");
            for (int i = 0; i < 48; i++)
            {
                heads.Add((GameObject)headstemp[i]);
            }
        }
        int rand = UnityEngine.Random.Range(0, heads.Count - 1);
        Transform iiro = transform.Find("Iiro");
        Transform root = iiro.Find("Root");
        Transform torso = root.Find("Torso");
        Transform chest = torso.Find("Chest");
        Transform Head = chest.Find("Head");
        Transform head = Head.Find("iiro_head");
        MeshFilter mesh = head.GetComponent<MeshFilter>();
        MeshRenderer material = head.GetComponent<MeshRenderer>();
        mesh.sharedMesh = heads[rand].GetComponent<MeshFilter>().sharedMesh;
        material.sharedMaterial = heads[rand].GetComponent<MeshRenderer>().sharedMaterial;
        return heads[rand].name;
    }
    */

    public string ChangeHead(List<GameObject> heads)
    {
        int rand = Random.Range(0, heads.Count - 1);
        Transform iiro = transform.Find("Iiro");
        Transform root = iiro.Find("Root");
        Transform torso = root.Find("Torso");
        Transform chest = torso.Find("Chest");
        Transform Head = chest.Find("Head");
        Transform head = Head.Find("iiro_head");
        MeshFilter mesh = head.GetComponent<MeshFilter>();
        MeshRenderer material = head.GetComponent<MeshRenderer>();
        mesh.sharedMesh = heads[rand].GetComponent<MeshFilter>().sharedMesh;
        material.sharedMaterial = heads[rand].GetComponent<MeshRenderer>().sharedMaterial;
        return heads[rand].name;
    }
}
