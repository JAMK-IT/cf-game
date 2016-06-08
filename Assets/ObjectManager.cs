﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ObjectManager : MonoBehaviour {

    Dictionary<GameObject, GameObject> bookableObjects = new Dictionary<GameObject, GameObject>();
    // Use this for initialization
    void Start () {

        GameObject[] objtemp = GameObject.FindGameObjectsWithTag("BookableObject");

        foreach(GameObject obj in objtemp)
        {
            bookableObjects.Add(obj.transform.parent.gameObject, null);
        }

    }

	// Update is called once per frame
	void Update () {
	
	}

    public GameObject bookBed(GameObject targetee)
    {
        foreach (KeyValuePair<GameObject, GameObject> obj in bookableObjects)
        {
            if (obj.Key.tag == "Bed" && obj.Value == null)
            {
                bookableObjects[obj.Key] = targetee;
                return obj.Key;
            }
        }
        return null;
    }
    
    public GameObject bookObject(GameObject targetee)
    {
        foreach (KeyValuePair<GameObject, GameObject> obj in bookableObjects)
        {
            if (obj.Value == null)
            {
                bookableObjects[obj.Key] = targetee;
                return obj.Key;
            }
        }
        return null;
    }
    /* Reserves random chair for the "Reserver", so others wont use the same object at the same time */
    public GameObject bookRandomChair(GameObject reserver)
    {
        List<KeyValuePair<GameObject, GameObject>> temp = new List<KeyValuePair<GameObject, GameObject>>();
        foreach (KeyValuePair<GameObject, GameObject> obj in bookableObjects)
        {
            if( (obj.Key.tag == "Chair" || obj.Key.tag == "QueueChair") && bookableObjects[obj.Key] == null )
            {
                temp.Add(obj);
            }
        }
        if (temp.Count > 0)
        {
            System.Random rnd = new System.Random();
            int rand = rnd.Next(0, temp.Count);
            GameObject key = temp[rand].Key;
            bookableObjects[key] = reserver;
            return key;
        }
        return null;
    }

    public GameObject bookRandomQueueChair(GameObject reserver)
    {
        List<KeyValuePair<GameObject, GameObject>> temp = new List<KeyValuePair<GameObject, GameObject>>();
        foreach (KeyValuePair<GameObject, GameObject> obj in bookableObjects)
        {
            if ( obj.Key.tag == "QueueChair" && bookableObjects[obj.Key] == null)
            {
                temp.Add(obj);
            }
        }
        if (temp.Count > 0)
        {
            System.Random rnd = new System.Random();
            int rand = rnd.Next(0, temp.Count);
            GameObject key = temp[rand].Key;
            bookableObjects[key] = reserver;
            return key;
        }
        return null;
    }


    public void bookTargetObject(GameObject target, GameObject targetee)
    {
        if (bookableObjects[target] == null)
        {
            bookableObjects[target] = targetee;
        }
        else
        {
            print("object in use");
        }
    }

    public void unbookObject(GameObject go)
    {
        bookableObjects[go] = null;
    }
}
