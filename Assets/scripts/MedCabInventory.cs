﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

/* list all the items found from the database in the medicine cabinet */
public class MedCabInventory : MonoBehaviour {

    GameObject slotPanel;
    ItemDatabase database;
    public GameObject medContainerPrefab;

    public void Init()
    {
        database = GameObject.Find("Inventory").GetComponent<ItemDatabase>();
        slotPanel = GameObject.Find("MedCabSlotPanel");
        for (int i = 0; i < database.database.Count; i++)
        {
            GameObject medContainerObj = Instantiate(medContainerPrefab);
            MedContainer medContainer = medContainerObj.GetComponent<MedContainer>();
            medContainerObj.transform.SetParent(slotPanel.transform);
            Item itemToAdd = database.FetchItemByID(i);
            medContainer.medName = itemToAdd.Title;
            medContainer.defaultDos = itemToAdd.DefaultDosage;
            medContainer.canSplit = itemToAdd.canSplit;
            medContainerObj.GetComponentInChildren<Text>().text = itemToAdd.Title + "\n" + itemToAdd.DefaultDosage + " mg";
            Sprite medContainerSprite = Resources.Load<Sprite>("Sprites/Meds/" + itemToAdd.Title);
            if (medContainerSprite == null)
                medContainerSprite = Resources.Load<Sprite>("Sprites/Meds/null");
            medContainerObj.GetComponent<Image>().sprite = medContainerSprite;
            medContainerObj.transform.localScale = new Vector3(1, 1, 1);
        }
    }

    public void Reset()
    {
        foreach (Transform t in slotPanel.transform)
        {
            Destroy(t.gameObject);
        }
    }
}
