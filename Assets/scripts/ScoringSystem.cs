﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
public class ScoringSystem : MonoBehaviour {

    public int score = 50;
    public int totalscore = 0;
    public int oldtotalscore = 0;
    int medinactivepunishment = -2;
    int respnpcleavinghospital = 10;
    int respnpcdeathpunishment = -25;
    public bool gameover = false;


    GameObject positivebar;
    Text percentage;
	// Use this for initialization
	void Start () {
        positivebar = GameObject.FindGameObjectWithTag("ScoreBar").transform.FindChild("Positive").gameObject;
        percentage = GameObject.FindGameObjectWithTag("ScoreBar").transform.FindChild("Percentage").GetComponent<Text>();
    }

    public void addToScore(int add)
    {
        if(add < 0)
        {
            if(score > 0)
                score += add;
            if (score <= 0)
            {
                score = 0;
                GameObject.FindGameObjectWithTag("UIManager").GetComponent<UIManager>().gameOver(totalscore);
            }
                
        }
        else if (add > 0)
        {
            if (score < 100)
                score += add;
            if (score > 100)
                score = 100;
        }
        positivebar.GetComponent<RectTransform>().sizeDelta = new Vector2(score * 4, 50.0f);
        percentage.text = score + "%";
    }

    public void reset()
    {
        score = 50;
        oldtotalscore = 0;
        totalscore = 0;
        gameover = false;
        positivebar.GetComponent<RectTransform>().sizeDelta = new Vector2(score * 4, 50.0f);
        percentage.text = score + "%";
    }

    public void medInactive()
    {
        addToScore(medinactivepunishment);
    }

    public void responsibilityNPCDied()
    {
        addToScore(respnpcdeathpunishment);
    }

    public void responsibilityNPCLeftHospital()
    {
        addToScore(respnpcleavinghospital);
    }

    public void endDay()
    {
        totalscore += score * 10;
    }

    public void nextDay()
    {
        oldtotalscore = totalscore;
    }


}
