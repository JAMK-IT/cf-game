﻿using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class SkipTimeCanvas : MonoBehaviour {

    bool blending = false;
    float speed = 1f;
    bool dimming = false;
    float timer = 0;
    Image black;
    NPCManagerV2 npcManager;
    bool allMedsGiven = true;

    GameObject afternoon;
    GameObject evening;
    GameObject night;
    GameObject warning;
    ClockTime.DayTime daytime;

	// Use this for initialization
	void Start () {

        black = GetComponent<Image>();
        black.color = new Color(0, 0, 0, 0);
        afternoon = transform.FindChild("Background").FindChild("Afternoon").gameObject;
        evening = transform.FindChild("Background").FindChild("Evening").gameObject;
        night = transform.FindChild("Background").FindChild("Night").gameObject;
        warning = transform.FindChild("Background").FindChild("Warning").gameObject;
        warning.SetActive(false);
        afternoon.SetActive(false);
        evening.SetActive(false);
        night.SetActive(false);

        npcManager = GameObject.FindGameObjectWithTag("NPCManager").GetComponent<NPCManagerV2>();

        daytime = GameObject.FindGameObjectWithTag("Clock").GetComponent<ClockTime>().currentDayTime;

        checkmeds();

        if (!allMedsGiven)
        {
            transform.FindChild("Background").FindChild("Text").gameObject.SetActive(false);
            warning.SetActive(true);
        }
        else
        {
            switch (daytime)
            {
                case ClockTime.DayTime.MORNING:
                    afternoon.SetActive(true);
                    break;
                case ClockTime.DayTime.AFTERNOON:
                    evening.SetActive(true);
                    break;
                case ClockTime.DayTime.EVENING:
                    night.SetActive(true);
                    break;
                default:
                    transform.FindChild("Background").FindChild("Text").gameObject.SetActive(false);
                    warning.SetActive(true);
                    warning.GetComponent<Text>().text = "You can't skip time right now!";
                    break;
            }
        }
    }
	
    void checkmeds()
    {
        switch (daytime)
        {
            case ClockTime.DayTime.MORNING:
                foreach (GameObject go in npcManager.responsibilityNpcs)
                {
                    NPCV2 npc = go.GetComponent<NPCV2>();
                    for(int i = 0; i < npc.morningMed.Length; i++)
                    {
                        if(npc.morningMed[i].title != null && !npc.morningMed[i].isActive)
                        {
                            allMedsGiven = false;
                            break;
                        }
                    }
                    if(!allMedsGiven)
                    {
                        break;
                    }
                }
                break;
            case ClockTime.DayTime.AFTERNOON:
                foreach (GameObject go in npcManager.responsibilityNpcs)
                {
                    NPCV2 npc = go.GetComponent<NPCV2>();
                    for (int i = 0; i < npc.afternoonMed.Length; i++)
                    {
                        if (npc.afternoonMed[i].title != null && !npc.afternoonMed[i].isActive)
                        {
                            allMedsGiven = false;
                            break;
                        }
                    }
                    if (!allMedsGiven)
                    {
                        break;
                    }
                }
                break;
            case ClockTime.DayTime.EVENING:
                foreach (GameObject go in npcManager.responsibilityNpcs)
                {
                    NPCV2 npc = go.GetComponent<NPCV2>();
                    for (int i = 0; i < npc.eveningMed.Length; i++)
                    {
                        if (npc.eveningMed[i].title != null && !npc.eveningMed[i].isActive)
                        {
                            allMedsGiven = false;
                            break;
                        }
                    }
                    if (!allMedsGiven)
                    {
                        break;
                    }
                }
                break;
        }
    }

	// Update is called once per frame
	void Update () {
	    if(blending)
        {
            black.color = new Color(0, 0, 0, black.color.a + Time.deltaTime * speed);
            if(black.color.a >= 1)
            {
                black.color = new Color(0, 0, 0, 1);
                blending = false;
                timer = 0;
                dimming = true;
                switch (daytime)
                {
                    case ClockTime.DayTime.MORNING:
                        GameObject.FindGameObjectWithTag("Clock").GetComponent<ClockTime>().setTime(12, 59);
                        break;
                    case ClockTime.DayTime.AFTERNOON:
                        GameObject.FindGameObjectWithTag("Clock").GetComponent<ClockTime>().setTime(14, 59);
                        break;
                    case ClockTime.DayTime.EVENING:
                        GameObject.FindGameObjectWithTag("Clock").GetComponent<ClockTime>().setTime(19, 59);
                        break;
                }
            }
        }
        if(dimming)
        {
            timer += Time.deltaTime;
            if(timer > 1f)
            {
                black.color = new Color(0, 0, 0, black.color.a - Time.deltaTime * speed);
                if (black.color.a <= 0)
                {   
                    black.color = new Color(0, 0, 0, 0);
                    Destroy(gameObject);
                }
            }
        }
	}

    public void NextDayTimeButton()
    {
        blending = true;
        transform.FindChild("Background").gameObject.SetActive(false);
    }

    public void Cancel()
    {
        Destroy(gameObject);
    }
}