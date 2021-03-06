﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

/*
 * NPC script includes behavior state machine for all patients.
 * 
 * NPC's change their state according to priorities given to tasks.
 * There are 3 priorities that can be given, 1 - nothing else to do, 2 - has a need to do, 3 - do now.
 * The states are in queue, thus they will be executed in first in first out order.
 * 
 * 
 */


public class NPC : MonoBehaviour
{

    public struct Medicine
    {
        public string title;
        /* has this med been given on the right time */
        public bool isActive;
        public float dosage;
    };

    /* states */
    public enum NPCState
    {
        STATE_ARRIVED = 0,
        STATE_QUE,
        STATE_IDLE,
        STATE_DEAD,
        STATE_TALK_TO_OTHER_NPC, 
        STATE_TALK_TO_PLAYER,
        STATE_SLEEP,
        STATE_SLEEP_ON_FLOOR,
        STATE_GO_TO_DOC,
        STATE_LEAVE_HOSPITAL,
        STATE_MOVE_TO_WARD_AREA,
        STATE_TRY_UNSTUCK,
        STATE_GO_WC,
        STATE_IDLE_SIT,
        STATE_DAY_CHANGE,
        STATE_DEFAULT, //Do nothing
        STATE_RANDOM_SLEEP
    }

    /* basic stuff */
    bool nursesDeployedForMe = false;
    /* Lock state to the current */
    bool lockstate = false;
    /* Should this npc skip next medcheck time */
    public bool skipNextMedCheck = false;
    /* Pause update */
    public bool paused = false;
    /* How active patient is, 1 - mostly sleeping, 2 - sleeping, sitting, 3 - running around the hospital */
    public int activityRate = 0;

    ClockTime clock;
    LineRenderer DebugPath;

    /* Animation component */
    public IiroAnimBehavior anim;

    /* Patients name */
    public string myName;

    /* Patients social security number */
    public string myId;
    public int myGender; // 0 = female, 1 = male

    /* Health */
    public int myHp = 50;    
    int currentTaskPriority = 0;

    /* 2D picture of the head patient has. (Exported with magical voxel) */
    public Sprite myHead2d;
    int prevTaskPriority = 0;
    /* Did patient pass out */
    bool dead = false;
    /* Reference to player */
    private GameObject player;
    /* The floating object on patient if he is player's responsibility */
    public GameObject responsibilityIndicator;
    public GameObject responsibilityIndicatorclone;
    /* has the npc visited the doctor */
    public bool diagnosed = false;
    /* how tired the npc is */
    public float fatique = 0;
    /* float to indicate the need to go to wc */
    public float callofnature = 0;
    public float callofnaturetimer = 0;
    public float fatiquetimer = 0;
    public NPCState prevState;
    /* Current state */
    public NPCState myState;
    /* All states which are added, Int = Priority */
    public Dictionary<int, Queue<NPCState>> stateQueue;
    public GameObject myBed;
    /* Use this to end current state completely */
    public bool taskCompleted = true;
    /* Zone around the patient to recognize close objects */
    Dialog dialogZone;
    ObjectInteraction interactionComponent;
    /* Manager to book and unbook objects */
    ObjectManager objectManager;
    public bool talking = false;
    public bool sleeping = false;
    private bool sitting = false;
    public bool cantFindBed = false;
    /* Has the npc already queued toilet use in stateQueue */
    private bool wcqueued = false;
    /* Has higher priority task been initiated and last state was important enough to remember (Priority 2) */
    bool prevStateUncompleted = false;
    /* Is this patient players responsibility */
    public bool playersResponsibility = false;
    /* Has the npc already queued sleeping in stateQueue */
    private bool sleepingqueued = false;

    /* This npc was reguested to talk by someone else */
    private bool reguestedToTalk = false;

    /* This npc reguested the talk with someone else */
    private bool wantsToTalk = false;
    ScoringSystem scoreSystem;

    /* NEW MEDICINE SYSTEM */
    float deathTimer; // time without medicine
    const int LOSE_HP_TIME = 2; // lose one hitpoint every X seconds if there is no medicine active


    
    public Item[] myMedication; // all of this NPC's meds, usages etc.
    public string[] myProblems;
    
    /* specific meds for different times of day */
    public Medicine[] morningMed = new Medicine[4];
    public Medicine[] afternoonMed = new Medicine[4];
    public Medicine[] eveningMed = new Medicine[4];
    public Medicine[] nightMed = new Medicine[4];

    /* if correct med is not active at the correct time of the day, start losing hp */
    public bool isLosingHp = false;

    /* position stuff */
    public Vector3 dest; // current destination position
    NavMeshAgent agent;
    NPCManager npcManager;
    Vector3 receptionPos = new Vector3(47, 4, 125); // position of reception

    /* timing stuff */
    float timer; // time NPC has been in the current state
    const float RECEPTION_WAITING_TIME = 1.5f;
    const float IDLE_IN_THIS_PLACE_TIME = 5f;
    const float MAX_TIME_TALK_TO_OTHER = 10f;
    const int WALK_RADIUS = 500;
    const float SLEEP_TIME = 10f;
    const float AT_DOC = 5f;
    const float IN_WC = 5f;
    const float STAY_ON_FLOOR_ON_FALL = 10.0f;

    //how long doctor will wait for patient
    const float DOC_WAIT_TIME = 10.0f;
    float doctimer = 0;

    /* Remember old agent information before pausing it */
    Vector3 lastAgentVelocity = Vector3.zero;
    NavMeshPath lastAgentPath;
    bool agentPaused = false;

    Tutorial tutorial;

    public AudioSource correctSound;
    public AudioSource wrongSound;
    public AudioSource dieSound;

    /* Points where npc's are allowed to die */
    Vector3[] deathpoints = {
        new Vector3(722, 0, -526),
        new Vector3(722, 0, -254),
        new Vector3(550, 0, -76),
        new Vector3(288, 0, -76),
        new Vector3(310, 0, 134),
        new Vector3(574, 0, 134),
        new Vector3(482, 0, -263),
        new Vector3(220, 0, -263),
        new Vector3(220, 0, -210),
        new Vector3(220, 0, -253),
        new Vector3(220, 0, -243),
        new Vector3(220, 0, -320),
        new Vector3(220, 0, -300),
        new Vector3(309, 0, 21),
        new Vector3(309, 0, 40),
        new Vector3(309, 0, 60),
        new Vector3(309, 0, 80),
        new Vector3(309, 0, 100),
        new Vector3(309, 0, 120),
        new Vector3(309, 0, 140),
        new Vector3(309, 0, 160),
        new Vector3(564, 0, 25),
        new Vector3(564, 0, 45),
        new Vector3(564, 0, 65),
        new Vector3(564, 0, 85),
        new Vector3(564, 0, 105),
        new Vector3(564, 0, 125),
        new Vector3(650, 0, -605),
        new Vector3(800, 0, -65),
        new Vector3(670, 0, -505),
        new Vector3(670, 0, -405),
        new Vector3(670, 0, -305),
        new Vector3(670, 0, -205),
        new Vector3(670, 0, -105),
        new Vector3(700, 0, -605),
        new Vector3(700, 0, -505),
        new Vector3(700, 0, -405),
        new Vector3(700, 0, -305),
        new Vector3(700, 0, -205),
        new Vector3(700, 0, -105),
    };

    // Use this for initialization
    void Start()
    {
        DebugPath = GetComponent<LineRenderer>(); //get the line renderer
        player = GameObject.FindGameObjectWithTag("Player");
        stateQueue = new Dictionary<int, Queue<NPCState>>();
        agent = GetComponent<NavMeshAgent>();
        //Set npc speed randomly
        agent.speed = Random.Range(60f, 100f);
        anim = GetComponent<IiroAnimBehavior>();
        //Set animation speed to match the walk speed.
        anim.setWalkAnimSpeed(agent.speed);
        npcManager = GameObject.Find("NPCManager").GetComponent<NPCManager>();
        dest = Vector3.zero;
        stateQueue.Add(1, new Queue<NPCState>());
        stateQueue.Add(2, new Queue<NPCState>());
        stateQueue.Add(3, new Queue<NPCState>());
        interactionComponent = GetComponent<ObjectInteraction>();
        objectManager = GameObject.FindGameObjectWithTag("ObjectManager").GetComponent<ObjectManager>();
        clock = GameObject.FindGameObjectWithTag("Clock").GetComponent<ClockTime>();
        scoreSystem = GameObject.FindGameObjectWithTag("ScoringSystem").GetComponent<ScoringSystem>();
        tutorial = GameObject.Find("Tutorial").GetComponent<Tutorial>();

        /* Start by arriving */
        addStateToQueue(2, NPCState.STATE_ARRIVED);
    }

    // Update is called once per frame
    void Update()
    {
        /* if paused or waiting for animation, pause agent */
        if (paused || anim.waitforanim)
        {
            if(!agentPaused)
                pause();
        }
        
        if (!paused && !anim.waitforanim)
        {
            
            if (agentPaused)
            {
                /* Resumes agent with old information */
                resume();
            }
            /* Move responsibility indicator 
             * TODO: Just make it patients child
             */
            if (playersResponsibility)
            {
                responsibilityIndicatorclone.transform.position = new Vector3(transform.position.x, transform.position.y + 64, transform.position.z);
                responsibilityIndicatorclone.transform.rotation = transform.rotation;
            }
            //check if there are some natural needs, or unstucking needs
            if (myState != NPCState.STATE_DAY_CHANGE && myState != NPCState.STATE_DEAD && myState != NPCState.STATE_LEAVE_HOSPITAL)
                checkNeeds();
            //Set current state to highest priority currently queued
            //if higher priority job compared to current state is found, current state will be paused
            if (!lockstate)
                setMyStateFromQueue();
            //Act according to the myState (Current state)
            if(!anim.waitforanim)
                actAccordingToState();
        }
    }

    /*
     * Pause agent and store old information
    */
    void pause()
    {
        if(agent.enabled)
        {
            lastAgentVelocity = agent.velocity;
            lastAgentPath = agent.path;
            agent.velocity = Vector3.zero;
            agent.ResetPath();
        }

        agentPaused = true;
    }

    /*
    * Resume agent from old information
    */
    void resume()
    {
        if(agent.enabled)
        {
            agent.velocity = lastAgentVelocity;
            if(lastAgentPath == null)
            {
                
            }
            else
            {
                agent.SetPath(lastAgentPath);
            }
        }
        agentPaused = false;
    }


    private void actAccordingToState()
    {
        /* act according to myState */
        switch (myState)
        {
            case NPCState.STATE_SLEEP_ON_FLOOR:
                sleepOnFloor();
                break;
            case NPCState.STATE_SLEEP:
                sleep();
                break;
            case NPCState.STATE_ARRIVED:
                arrival();
                break;
            case NPCState.STATE_QUE:
                queue();
                break;
            case NPCState.STATE_IDLE:
                idle();
                break;
            case NPCState.STATE_DEAD:
                die();
                break;
            case NPCState.STATE_TALK_TO_PLAYER:
                talkToPlayer();
                break;
            case NPCState.STATE_TALK_TO_OTHER_NPC:
                talkToNPC();
                break;
            case NPCState.STATE_GO_TO_DOC:
                goToDoc();
                break;
            case NPCState.STATE_LEAVE_HOSPITAL:
                leaveHospital();
                break;
            case NPCState.STATE_MOVE_TO_WARD_AREA:
                goToWardArea();
                break;
            case NPCState.STATE_TRY_UNSTUCK:
                tryUnstuck();
                break;
            case NPCState.STATE_GO_WC:
                goWC();
                break;
            case NPCState.STATE_IDLE_SIT:
                idleSit();
                break;
            case NPCState.STATE_DAY_CHANGE:
                dayReset();
                break;
            case NPCState.STATE_DEFAULT:
                break;
            case NPCState.STATE_RANDOM_SLEEP:
                randomSleep();
                break;
        }
    }

    /*
     * Check if the npc has any needs
     * Wc, medicine, sleeping, talking
     */
    void checkNeeds()
    {
        GameObject targeter;
        targeter = dialogZone.getWhoIsTargetingMe();
        if (targeter != null && myState != NPCState.STATE_GO_TO_DOC && myState != NPCState.STATE_LEAVE_HOSPITAL)
        {
            
            if(targeter.tag == "Player")
            {
                if(myState != NPCState.STATE_TALK_TO_PLAYER)
                    addStateToQueue(3, NPCState.STATE_TALK_TO_PLAYER);
                interactionComponent.setTarget(dialogZone.getWhoIsTargetingMe());
                reguestedToTalk = true;
            }
            else if (targeter.tag == "NPC" && !wantsToTalk) 
            {
                if(myState != NPCState.STATE_TALK_TO_OTHER_NPC)
                    addStateToQueue(3, NPCState.STATE_TALK_TO_OTHER_NPC);
                interactionComponent.setTarget(dialogZone.getWhoIsTargetingMe());
                reguestedToTalk = true;

            }
        }
        else if(reguestedToTalk)
        {
            reguestedToTalk = false;
            taskCompleted = true;
            talking = false;
            agent.Resume();
        }

        //check status only if has visited the doctor
        if (diagnosed && !tutorial.tutorialOn)
        {
            //check medication every update if player is diagnosed
            checkMed();
            //sleep at night
            if (!sleepingqueued && clock.currentDayTime == ClockTime.DayTime.NIGHT)
            {
                addStateToQueue(2, NPCState.STATE_SLEEP);
                sleepingqueued = true;
            }
            //Increase fatigue every x seconds if state is not sleep
            //if sleeping already queued, just skip
            if (!sleepingqueued && !sleeping)
            {
                fatiquetimer += Time.deltaTime;
                if (fatiquetimer > 5.0f)
                {
                    fatique += 1f;
                    fatiquetimer = 0;
                }
                //if has bed and fatique over x, queue sleep task
                if (fatique > 10.0f)
                {
                    addStateToQueue(2, NPCState.STATE_SLEEP);
                    sleepingqueued = true;
                }
            }
            //Don't test need to go to wc if already queued
            if (!wcqueued)
            {
                callofnaturetimer += Time.deltaTime;
                if (callofnaturetimer > 5.0f)
                {
                    callofnature += 1;
                    callofnaturetimer = 0;
                }
                if (callofnature > 30.0f)
                {
                    addStateToQueue(2, NPCState.STATE_GO_WC);
                    wcqueued = true;
                }
            }
        }
    }

    /*
     * Random sitting
     */
    void idleSit()
    {
        if (dest == Vector3.zero)
        {
            GameObject targetChair = objectManager.bookRandomChair(gameObject);
            if (targetChair == null)
            {
                taskCompleted = true;
            }
            else
            {
                interactionComponent.setTarget(targetChair);
                interactionComponent.setCurrentChair(interactionComponent.getTarget());
                // set destination to queue chair
                if(targetChair.tag == "Chair2")
                {
                    dest = interactionComponent.getDestToTargetObjectSide(1, 20.0f);
                }
                else
                {
                    dest = interactionComponent.getDestToTargetObjectSide(0, 20.0f);
                }
                
                // move to the queue position received
                moveTo(dest);
            }

        }
        else if (arrivedToDestination(10.0f) && !sitting)
        {
            //rotate to look away from the chair so animation will move the player on the chair
            if (interactionComponent.RotateAwayFrom(interactionComponent.getTarget().transform))
            {
                sitting = true;
                agent.Stop();
                if (interactionComponent.getCurrentChair().tag == "Chair2")
                {
                    anim.sitwithrotation();
                }
                else
                {
                    anim.sit();
                }
            }
        }
        else if (sitting)
        {
            timer += Time.deltaTime;
            if(timer > 2 * IDLE_IN_THIS_PLACE_TIME)
            {             
                taskCompleted = true;
                //unbook the used chair
                if ((interactionComponent.getCurrentChair() != null))
                {
                    objectManager.unbookObject(interactionComponent.getCurrentChair());
                    interactionComponent.setCurrentChair(null);
                }  
                sitting = false;
            }
        }
    }
    void goWC()
    {
        if (dest == Vector3.zero)
        {
            GameObject targetToilet = objectManager.bookRandomPublicToilet(gameObject);
            if (targetToilet == null)
            {
                taskCompleted = true;
                wcqueued = false;
                callofnature = 0;
            }
            else
            {
                interactionComponent.setTarget(targetToilet);
                interactionComponent.setCurrentToilet(interactionComponent.getTarget());
                // set destination to queue chair
                dest = interactionComponent.getDestToTargetObjectSide(2, 20.0f);
                // move to the queue position received
                moveTo(dest);
            }
         }
        if (arrivedToDestination(10.0f) && !sitting)
        {
            agent.Stop();
            sitting = true;
        }

        if (sitting)
        {
            if (arrivedToDestination(20.0f))
            {
                if (interactionComponent.getTarget())
                {
                    //rotate to look away from the bed so animation will move the player on the bed
                    if (interactionComponent.RotateAwayFrom(interactionComponent.getTarget().transform))
                    {
                        if(!anim.sitting)
                            anim.sit();
                        timer += Time.deltaTime;
                        if (timer > IN_WC)
                        {
                            taskCompleted = true;
                            if ((interactionComponent.getCurrentToilet() != null))
                            {
                                objectManager.unbookObject(interactionComponent.getCurrentToilet());
                                interactionComponent.setCurrentToilet(null);
                            }  
                            sitting = false;
                            wcqueued = false;
                            callofnature = 0;
                        }
                    }
                }
            }
        }
    }

    bool isDestInvalid()
    {
        if(agent.pathStatus == NavMeshPathStatus.PathInvalid)
        {
            return true;
        }
        if(dest.x == Mathf.Infinity || dest.z == Mathf.Infinity)
        {
            return true;
        }
        return false;
    }

    /* Try unstucking with random moving */
    private void tryUnstuck()
    {
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
        if (dest == Vector3.zero || isDestInvalid())
        {
            Vector3 randomDirection = Random.insideUnitSphere * 80.0f;
            randomDirection += transform.position;
            NavMeshHit hit;
            
            NavMesh.SamplePosition(randomDirection, out hit, 80.0f, 1);
            Vector3 finalPosition = hit.position;
            dest = new Vector3(finalPosition.x, transform.position.y, finalPosition.z);
            moveTo(dest);
        }
        if(arrivedToDestination(30.0f))
        {
            taskCompleted = true;
            addStateToQueue(2, prevState);
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
        }
    }

    /* Force patient to move to ward, so it doesn't hang out in
     *  waiting room or doctor after being diagnozed
     */
    private void goToWardArea()
    {

        if(dest == Vector3.zero)
        {
            int rand = Random.Range(0, deathpoints.Length - 1);
            dest = deathpoints[rand];
            moveTo(dest);
        }
        if(arrivedToDestination(100.0f))
        {
            if(tutorial.tutorialOn)
            {
                agent.Stop();
                if (tutorial.tutorialOn)
                {
                    lockstate = true;
                }
            }
            else
            {
                taskCompleted = true;
            }
            
        }
    }

    private void leaveHospital()
    {
        if(dest == Vector3.zero)
        {
            dest = npcManager.spawnPoint;
            moveTo(dest); 
        }
        if (player.GetComponent<PlayerControl>().getTarget() == gameObject)
        {
            GameObject.FindGameObjectWithTag("TextBoxManager").GetComponent<TextBoxManager>().DisableTextBox();
        }
        if (arrivedToDestination(100.0f))
        {
            npcManager.deleteNpcFromList(gameObject);
            if(playersResponsibility)
            {
                npcManager.removeNpcFromPlayersResponsibilities(gameObject);
            }
            Destroy(gameObject);
        }
    }

    private void goToDoc()
    {
        if(!diagnosed)
        {
            doctimer += Time.deltaTime;
            if (dest == Vector3.zero)
            {
                dest = new Vector3(-50.0f, transform.position.y, 393.0f);
                moveTo(dest);
                timer = 0;
                doctimer = 0;
            }
            if (arrivedToDestination(50.0f))
            {
                doctimer = 0;
                timer += Time.deltaTime;
                if (timer > AT_DOC)
                {
                    if(tutorial.tutorialOn)
                    {
                        Item[] ibuprofen = new Item[1];
                        ibuprofen[0] = npcManager.getItemByID(0);
                        InitMedication(ibuprofen);
                        addStateToQueue(2, NPCState.STATE_MOVE_TO_WARD_AREA);
                        diagnosed = true;
                        addNpcToResponsibilities();
                        timer = 0;
                        taskCompleted = true;
                        dest = Vector3.zero;
                        npcManager.setDocFree();
                    }
                    else
                    {
                        int r = Random.Range(1, 10);
                        if (r > 1 && npcManager.currentNpcsInWard < NPCManager.MAX_NPCS_IN_WARD_AREA)
                        {
                            // Randomize 2-4 DIFFERENT problems for the NPC
                            int numProblems = UnityEngine.Random.Range(2, 5);
                            Item[] randMeds = new Item[4];
                            // Fetch random medicine items from database
                            for (int i = 0; i < randMeds.Length; i++)
                            {
                                if (numProblems > 0)
                                {
                                    randMeds[i] = npcManager.RandomItem(randMeds);
                                    numProblems--;
                                }
                                else
                                    randMeds[i] = null;
                            }
                            InitMedication(randMeds);
                            addStateToQueue(2, NPCState.STATE_MOVE_TO_WARD_AREA);
                            diagnosed = true;
                            if (!npcManager.isPlayerResponsibilityLevelFulfilled())
                            {
                                addNpcToResponsibilities();
                                SkipMedCheck();
                                tutorial.ShowNotification("You have a new patient: " + myName + "!", 5f, false);
                            }
                            npcManager.currentNpcsInWard++;
                        }
                        else
                        {
                            addStateToQueue(3, NPCState.STATE_LEAVE_HOSPITAL);
                        }
                        timer = 0;
                        taskCompleted = true;
                        npcManager.setDocFree();
                    }
                }
            }
        }
        else
        {
            taskCompleted = true;
        }
    }

    public void addNpcToResponsibilities()
    {
        npcManager.addNpcToPlayersResponsibilities(gameObject);
        playersResponsibility = true;
        responsibilityIndicatorclone = (GameObject)Instantiate(responsibilityIndicator, transform.position, new Quaternion(0, 0, 0, 0));
    }

    private void debugDrawPath(NavMeshPath path)
    {
        if (path.corners.Length < 2) //if the path has 1 or no corners, there is no need
            return;

        DebugPath.SetVertexCount(path.corners.Length); //set the array of positions to the amount of corners

        for (var i = 1; i < path.corners.Length; i++)
        {
            DebugPath.SetPosition(i, path.corners[i]); //go through each corner and set that to the line renderer's position
        }
    }

    private void sleepOnFloor()
    {
        //slam npc face to floor
        transform.rotation = Quaternion.Euler(90, transform.eulerAngles.y, transform.eulerAngles.z);
        if (!sleeping)
        {
            //stop the navmesh agent movement
            agent.updateRotation = false;
            agent.Stop();
            sleeping = true;
        }
        if (sleeping)
        {

            timer += Time.deltaTime;
            if (timer > 10.0f)
            {
                timer = 0;
                //go back to idle after sleeping, reset all values, lose health
                addStateToQueue(2, NPCState.STATE_IDLE);
                dest = Vector3.zero;
                cantFindBed = false;
                sleeping = false;
                myHp -= 10;
                fatique = 0;
                taskCompleted = true;

                //navmesh agent resume
                agent.updateRotation = true;
                agent.Resume();
            }
        }
    }
    private void sleep()
    {
        //if npc doesn't have bed, try to find one
        if (myBed == null)
        {
            //check if bed is available
            myBed = objectManager.bookBed(gameObject);
            //if still null, bed not found
            if (myBed == null)
            {
                //back to idle
                addStateToQueue(2, NPCState.STATE_IDLE);

                //mark that this npc doesn't have bed, so next loop will not go to sleep(), 
                //but stay in idle until 
                //fatique is too high
                cantFindBed = true;
                taskCompleted = true;
            }
        }

        //if has no destination and has a bed
        if (dest == Vector3.zero && myBed != null)
        {
            interactionComponent.setTarget(myBed);
            dest = interactionComponent.getDestToTargetObjectSide(1, 25.0f);
            if(dest == Vector3.zero)
            {
                cantFindBed = true;
                taskCompleted = true;
            }
            else
            {
                moveTo(dest);
            }
            
        }
        //if at the bed and not sleeping yet, stop navmeshagent and start animation
        if (myBed != null && arrivedToDestination(10.0f) && !sleeping)
        {
            agent.Stop();
            //rotate until looking away from mybed
            if (interactionComponent.RotateAwayFrom(myBed.transform))
            {
                sleeping = true;
            }
        }

        if (sleeping)
        {
            if (!anim.sleeping)
                anim.sleep();
            timer += Time.deltaTime;

            if (timer > SLEEP_TIME)
            {
                if(clock.currentDayTime == ClockTime.DayTime.NIGHT)
                {
                    timer = 0;
                    //randomly wake some people up
                    if(activityRate == 3 && Random.Range(0, 100) > 90)
                    {
                        sleeping = false;
                        taskCompleted = true;
                        timer  = 0;
                        fatique = 0;
                        sleepingqueued = false;
                    }
                }
                else
                {
                    sleeping = false;
                    taskCompleted = true;
                    timer = 0;
                    fatique = 0;
                    sleepingqueued = false;
                }
            }
        }
    }

    private void randomSleep()
    {
        if (myBed == null)
        {
            //check if bed is available
            myBed = objectManager.bookBed(gameObject);
            //if still null, bed not found
            if (myBed == null)
            {
                print("Null bed");
            }
        }
        //if already was sleeping, skip
        if(prevState != NPCState.STATE_RANDOM_SLEEP)
        {
            if (dest == Vector3.zero && myBed != null)
            {
                interactionComponent.setTarget(myBed);
                dest = interactionComponent.getDestToTargetObjectSide(1, 25.0f);
                moveTo(dest);
            }
            //if at the bed and not sleeping yet, stop navmeshagent and start animation
            if (myBed != null && arrivedToDestination(10.0f) && !sleeping)
            {
                agent.Stop();
                //rotate until looking away from mybed
                if (interactionComponent.RotateAwayFrom(myBed.transform))
                {
                    sleeping = true;
                }
            }

            if (sleeping)
            {
                if (!anim.sleeping)
                    anim.sleep();
                timer += Time.deltaTime;
                if (timer > SLEEP_TIME * 2)
                {
                    sleeping = false;
                    taskCompleted = true;
                    timer = 0;
                    fatique = 0;
                }
            }
        }
        else
        {
            if(!sleeping)
            {
                sleeping = true;
            }
            if (sleeping)
            {
                if (!anim.sleeping)
                    anim.sleep();
                timer += Time.deltaTime;
                if (timer > SLEEP_TIME * 2)
                {
                    sleeping = false;
                    taskCompleted = true;
                    timer = 0;
                    fatique = 0;
                }
            }
        }
    }

    //arrives at hospital
    private void arrival()
    {
        // move to reception when NPC first arrives
        if (dest == Vector3.zero)
        {
            dest = receptionPos;
            moveTo(dest);
        }

        // NPC has arrived at reception
        if (arrivedToDestination(50.0f))
        {
            // chill for a while at reception and then move to doctor's queue
            timer += Time.deltaTime;
            if (timer > RECEPTION_WAITING_TIME)
            {
                addStateToQueue(2, NPCState.STATE_QUE);
                timer = 0;
                taskCompleted = true;
            }
        }
    }
    //queue to doctor
    private void queue()
    {
        if(npcManager.isDocBusy())
        {
            if (dest == Vector3.zero)
            {
                GameObject targetChair = objectManager.bookRandomQueueChair(gameObject);
                if(targetChair == null)
                {
                    addStateToQueue(3, NPCState.STATE_LEAVE_HOSPITAL);
                    taskCompleted = true;
                }
                else
                {
                    interactionComponent.setTarget(targetChair);
                    interactionComponent.setCurrentChair(interactionComponent.getTarget());
                    // set destination to queue chair
                    dest = interactionComponent.getDestToTargetObjectSide(0, 20.0f);
                    // move to the queue position received
                    moveTo(dest);
                }

            }

            if (arrivedToDestination(10.0f) && !sitting)
            {
                agent.Stop();
                sitting = true;
            }

            if (sitting)
            {
                if(arrivedToDestination(10.0f))
                {
                    if(interactionComponent.getTarget())
                    {
                        //rotate to look away from the bed so animation will move the player on the bed
                        if (interactionComponent.RotateAwayFrom(interactionComponent.getTarget().transform))
                        {
                            anim.sit();
                        }
                    }

                }
            }
        }
        else
        {         
            taskCompleted = true;
            if((interactionComponent.getCurrentChair() != null))
            {
                objectManager.unbookObject(interactionComponent.getCurrentChair());
                interactionComponent.setCurrentChair(null);
            }
                
            sitting = false;
            if (!agent.enabled)
                agent.enabled = true;
            agent.Resume();
            addStateToQueue(3, NPCState.STATE_GO_TO_DOC);
            npcManager.setDocBusy();
        }
    }
    //just wander around the hospital, if npc has nothing else to do it will go to this state
    private void idle()
    {
        //check if there's something else to do
        timer += Time.deltaTime;
        //if there is a npc in dialogzone and it has target to this, assume it's talking to this

        if (dest == Vector3.zero)
        {
            // move to idle at random position
            Vector3 randomDirection = Random.insideUnitSphere * WALK_RADIUS;
            randomDirection += transform.position;
            NavMeshHit hit;
            NavMesh.SamplePosition(randomDirection, out hit, WALK_RADIUS, 1);
            Vector3 finalPosition = hit.position;
            dest = new Vector3(finalPosition.x, 0, finalPosition.z);
            moveTo(dest);
        }
        else
        {
            if (arrivedToDestination(30.0f))
            {
                if((timer > IDLE_IN_THIS_PLACE_TIME))
                {
                    timer = 0;
                    taskCompleted = true;
                }
            }
        }
    }
    /*
     * This is called when players shift ends.
     */
    public void dayReset()
    {
        if(!lockstate)
        {
            agent.ResetPath();
            if (myBed == null)
            {
                myBed = objectManager.bookBed(gameObject);
            }
            interactionComponent.setTarget(myBed);
            agent.Warp(interactionComponent.getDestToTargetObjectSide(1, 25.0f));
            agent.Stop();
            if (interactionComponent != null && myBed != null)
                interactionComponent.RotateAwayFromNOW(myBed.transform);
            anim.sleep();
            //reset queues
            stateQueue.Clear();
            stateQueue.Add(1, new Queue<NPCState>());
            stateQueue.Add(2, new Queue<NPCState>());
            stateQueue.Add(3, new Queue<NPCState>());
            lockstate = true;
        }
    }
    /*
     * This is called when players shift starts
     */
    public void stopDayReset()
    {
        lockstate = false;
        sitting = false;
        sleeping = true;
        taskCompleted = true;
        addStateToQueue(3, NPCState.STATE_SLEEP);
    }

    /* Pass out */
    private void die()
    {
        lockstate = true;
        interactionComponent.setTarget(null);
        if (!dead)
        {
            //first iteration will set destination to area where fetching nurses can actually move
            if (dest == Vector3.zero)
            {
                objectManager.unbookObject(myBed);
                //get random location where to ISH die
                int rand = Random.Range(0, deathpoints.Length - 1);
                dest = deathpoints[rand];
                agent.SetDestination(dest);
                interactionComponent.setTarget(null);
            }
            //When arrived to good position, set npc to dead and rmeove from npclists
            else if (arrivedToDestination(5.0f))
            {
                dieSound.Play();
                if (playersResponsibility)
                { 
                    GetComponent<FloatTextNPC>().addFloatText("Health critical! Passing out!", false);
                    tutorial.ShowNotification("Your patient " + myName + " has passed out!", 5f, true);
                    GameObject.FindGameObjectWithTag("ScoringSystem").GetComponent<ScoringSystem>().responsibilityNPCDied();
                    npcManager.respNpcsWhoLeftOrDied.Add(new NPCINFO(myName, myHead2d, true));
                }
                npcManager.deleteNpcFromList(gameObject);
                //if this npc is players target, make sure textboxmanager disables UI showing npc status
                if (GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerControl>().getTarget() == gameObject)
                {
                    GameObject.FindGameObjectWithTag("TextBoxManager").GetComponent<TextBoxManager>().DisableTextBox();
                }
                //if this npc was players responsibility, remove from responsibilities
                if(playersResponsibility)
                {
                    playersResponsibility = false;
                    npcManager.removeNpcFromPlayersResponsibilities(gameObject);
                    Destroy(responsibilityIndicatorclone);
                }
                   
                dead = true;
            }
        }
        else
        {
            if (gameObject == null)
                return;
            //if dead and nurses aren't already deployed to fetch someone, release the nurses
            if(!nursesDeployedForMe)
            {
                nursesDeployedForMe = npcManager.spawnNurseToFetchNPC(gameObject);
            }
            //if not already fallen, fall
            if (!anim.falling)
            {
                agent.Stop();
                anim.fall();
            }
        }
    }
    //if player is close and player has target on this npc, talk to player
    private void talkToPlayer()
    {
        if(reguestedToTalk)
        {
            if(!talking)
            {
                agent.Stop();
                talking = true;
            }
            interactionComponent.RotateTowards(player.transform);
        }
    }

    private void talkToNPC()
    {
        /* If this npc was reguested to talk, just stop and rotate*/
        if(reguestedToTalk)
        {
            if (!talking)
            {
                agent.Stop();
                talking = true;
            }
            interactionComponent.RotateTowards(interactionComponent.getTarget().transform);
        }
        /* Else if this npc wants to talk to someone else*/
        else
        {
            //mark this npc to be the talk reguester so it wont be reguestedtotalk by the target npc
            wantsToTalk = true;
            GameObject target = interactionComponent.getTarget();
            //check that the target is actually capable of talking
            if (target == null || target.tag != "NPC" || !target.GetComponent<NPC>().isIdle() || target.GetComponent<NPC>().dead || target.GetComponent<NPC>().sleeping || target.GetComponent<NPC>().sitting)
            {
                findOtherIdleNPC();
            }
            //check if at target & set destination
            else if (walkToTarget())
            {
                if (!talking)
                {
                    talking = true;
                    //stop moving
                    agent.Stop();
                }
                //rotate to look the target
                interactionComponent.RotateTowards(target.transform);
                timer += Time.deltaTime;
                if (timer > MAX_TIME_TALK_TO_OTHER)
                {
                    talking = false;
                    agent.Resume();
                    timer = 0;
                    interactionComponent.setTarget(null);
                    taskCompleted = true;
                    wantsToTalk = false;
                }
            }
        }
    }

    public void setTarget(GameObject target)
    {
        interactionComponent.setTarget(target);
    }

    public GameObject getTarget()
    {
        return interactionComponent.getTarget();
    }

    //returns true if npc is already at target, sets the agent destination
    private bool walkToTarget()
    {
        GameObject target = interactionComponent.getTarget();
        if (Vector3.Distance(transform.position, target.transform.position) < 30.0f)
        {
            return true;
        }
        else
        {
            if(!agent.hasPath)
                moveTo(target.transform.position);
            return false;
        }
    }
    //looks for other npcs who are idle
    private void findOtherIdleNPC()
    {
        List<GameObject> npcs = npcManager.npcList;
        List<GameObject> idlenpcs = new List<GameObject>();
        foreach (GameObject npc in npcs)
        {
            if (npc.gameObject != gameObject)
            {
                NPC script = npc.GetComponent<NPC>();
                if (script.isIdle() && !script.sitting && !script.sleeping)
                {
                    if(!script.talking)
                    {
                        idlenpcs.Add(npc.gameObject);
                    }
                }
            }
        }
        if(idlenpcs.Count > 0)
        {
            interactionComponent.setTarget(idlenpcs[0]);
        }
        else
        {
            taskCompleted = true;
        }
    }


    public bool isIdle()
    {
        if (myState == NPCState.STATE_IDLE)
        {
            return true;
        }
        else return false;
    }

    //checks if navmeshagent is within accuracy zone of his destination
    private bool arrivedToDestination(float accuracy)
    {
        float dist = Vector3.Distance(agent.destination, transform.position);
        if (dist < accuracy)
            return true;
        else
            return false;
    }

    /* Adds state to right priority queue, checks for duplicates */
    public void addStateToQueue(int priority, NPCState state)
    {
        Queue<NPCState> queue = new Queue<NPCState>();
        
        stateQueue.TryGetValue(priority, out queue);
        if(!queue.Contains(state))
        {
            queue.Enqueue(state);
        }
        
    }


    
    /*
     * Sets patient's state
     * If task is completed, find highest priority task
     * If task is completed and nothing is queued, set random idle task according to patients activityrate
     * 
     * if task isn't completed, checks if there's any higher priority tasks queued
     * if found, saves old state and changes state, when the higher priority task is completed, checks if old state was uncompleted
     */
    public void setMyStateFromQueue()
    {
        if(taskCompleted)
        {
            sleeping = false;
            sitting = false;
            talking = false;
            timer = 0;
            //fool proof
            if(interactionComponent.getCurrentChair() != null)
            {
                objectManager.unbookObject(interactionComponent.getCurrentChair());
                interactionComponent.setCurrentChair(null);
            }
            if (prevStateUncompleted)
            {
                anim.StopAll();
                prevStateUncompleted = false;
                addStateToQueue(3, prevState);
                prevState = myState;
                myState = NPCState.STATE_DEFAULT;
                currentTaskPriority = 0;
                dest = Vector3.zero;
                taskCompleted = true;
            }
            else
            {
                Queue<NPCState> queue = new Queue<NPCState>();
                //Dequeue a task from priority 3 queue if it has a task and the current task is less important
                stateQueue.TryGetValue(3, out queue);
                if (queue.Count > 0)
                {
                    anim.StopAll();
                    prevState = myState;
                    myState = queue.Dequeue();
                    dest = Vector3.zero;
                    taskCompleted = false;
                    currentTaskPriority = 3;
                }
                else
                {
                    //Dequeue a task from priority 2 queue if it has a task and the current task is less important
                    stateQueue.TryGetValue(2, out queue);
                    if (queue.Count > 0)
                    {
                        anim.StopAll();
                        prevState = myState;
                        myState = queue.Dequeue();
                        dest = Vector3.zero;
                        taskCompleted = false;
                        currentTaskPriority = 2;
                    }
                    else
                    {
                        //Dequeue a task from priority 2 queue if it has a task and the current task is less important
                        stateQueue.TryGetValue(1, out queue);
                        if (queue.Count > 0)
                        {
                            anim.StopAll();
                            prevState = myState;
                            myState = queue.Dequeue();
                            dest = Vector3.zero;
                            taskCompleted = false;
                            currentTaskPriority = 1;
                        }
                        else
                        {
                            //if nothing to do choose randomly from talking with npcs, sitting, idle walking
                            pickRandomActivity();
                        }
                    }
                }
            }
        }
        else if (!prevStateUncompleted)
        {
            Queue<NPCState> queue = new Queue<NPCState>();
            //Dequeue a task from priority 3 queue if it has a task and the current task is less important
            stateQueue.TryGetValue(3, out queue);
            if (currentTaskPriority < 3 && queue.Count > 0)
            {
                if (myState == queue.Peek())
                    queue.Dequeue();
                else
                {
                    anim.StopAll();
                    //if currenttaskpriority is higher than 1 we will resume the task after new task is complete
                    if (currentTaskPriority > 1)
                        prevStateUncompleted = true;
                    //save current state info so it can be done after the prioritized task is complete
                    prevState = myState;
                    myState = queue.Dequeue();
                    prevTaskPriority = currentTaskPriority;
                    dest = Vector3.zero;
                    taskCompleted = false;
                    timer = 0;
                }

            }
            else 
            {
                //Dequeue a task from priority 2 queue if it has a task and the current task is less important
                stateQueue.TryGetValue(2, out queue);
                if (currentTaskPriority < 2 && queue.Count > 0)
                {
                    if (myState == queue.Peek())
                        queue.Dequeue();
                    else
                    {
                        anim.StopAll();
                        //save current state info so it can be done after the prioritized task is complete
                        prevState = myState;
                        myState = queue.Dequeue();
                        dest = Vector3.zero;
                        prevTaskPriority = currentTaskPriority;
                        taskCompleted = false;
                        timer = 0;
                    }
                }
            }
        }  
    }

    public void pickRandomActivity()
    {
        //Fail proofing
        if (!diagnosed)
        {
            myState = NPCState.STATE_QUE;
            prevState = myState;
            currentTaskPriority = 2;
            dest = Vector3.zero;
        }
        else
        {
            
            if (activityRate == 1)
            {
                prevState = myState;
                myState = NPCState.STATE_RANDOM_SLEEP;
                dest = Vector3.zero;
            }
            if (activityRate == 2)
            {
                if (Random.Range(0, 2) > 0)
                {
                    prevState = myState;
                    myState = NPCState.STATE_RANDOM_SLEEP;
                    dest = Vector3.zero;
                }
                else
                {
                    if (Random.Range(1, 11) > 4)
                    {
                        anim.StopAll();
                        prevState = myState;
                        myState = NPCState.STATE_TALK_TO_OTHER_NPC;
                        dest = Vector3.zero;
                    }
                    else if (Random.Range(1, 11) > 4)
                    {
                        anim.StopAll();
                        prevState = myState;
                        myState = NPCState.STATE_IDLE_SIT;
                        dest = Vector3.zero;
                    }
                    else
                    {
                        prevState = myState;
                        myState = NPCState.STATE_RANDOM_SLEEP;
                        dest = Vector3.zero;
                    }
                }
            }
            if (activityRate == 3)
            {
                if (Random.Range(1, 11) > 7)
                {
                    anim.StopAll();
                    prevState = myState;
                    myState = NPCState.STATE_TALK_TO_OTHER_NPC;
                    dest = Vector3.zero;
                }
                else if (Random.Range(1, 11) > 7)
                {
                    anim.StopAll();
                    prevState = myState;
                    myState = NPCState.STATE_IDLE_SIT;
                    dest = Vector3.zero;
                }
                else
                {
                    anim.StopAll();
                    prevState = myState;
                    myState = NPCState.STATE_IDLE;
                    dest = Vector3.zero;
                }
            }
            currentTaskPriority = 1;
        }
        taskCompleted = false;
    }

    public void Init(string myName, string myId, int myGender)
    {
        this.myName = myName;
        this.myId = myId;
        this.myGender = myGender;
    }

    public void rePath()
    {
        Vector3 temp = agent.destination;
        agent.ResetPath();
        agent.SetDestination(temp);
    }

    /* Returns random dosage found in Item */
    float getRandomDosage(Item medicine)
    {
        float ret = medicine.DefaultDosage;
        int range = 0;
        if (medicine.canSplit == 0)
        {
           range = Random.Range(2, 4);
        }
        else
        {
            range = Random.Range(1, 4);
        }
        
        switch(range)
        {
            case 1:
                ret = medicine.SmallDosage;
                break;
            case 2:
                ret = medicine.MediumDosage;
                break;
            case 3:
                ret = medicine.HighDosage;
                break;
        }
        return ret;
    }

    public void SkipMedCheck()
    {
        //if patient arrives just before next medcheck, ignore it
        if (clock.currentHours == 7 )
            skipNextMedCheck = true;
        else if (clock.currentHours == 13)
            skipNextMedCheck = true;
        else if (clock.currentHours == 15)
            skipNextMedCheck = true;
        else if (clock.currentHours == 20)
            skipNextMedCheck = true;
    }

    // Init 2-4 random problems and their corresponding medicines
    public void InitMedication(Item[] randMeds)
    {

        int drugscount = 0;
        //count how many drugs were generated
        foreach(Item item in randMeds)
        {
            if (item != null)
            {
                drugscount++;
            }
        }
        myMedication = new Item[drugscount];
        myProblems = new string[drugscount];

        if(drugscount == 4)
        {
            activityRate = 1;
        }
        else if(drugscount == 3)
        {
            activityRate = 2;
        }
        else if (drugscount == 2)
        {
            activityRate = 3;
        }

        //move drugs to locals
        for (int i = 0; i < drugscount; i++)
        {
            myMedication[i] = randMeds[i];
            myProblems[i] = randMeds[i].Usage;
        }

        for (int i = 0; i < myMedication.Length; i++)
        {
            float dosage;
            if (tutorial.tutorialOn)
            {
                dosage = myMedication[i].DefaultDosage;
            }
            else
            {
                dosage = getRandomDosage(myMedication[i]);
            }

            //on tutorial just give 1 med for morning with default dosage
            if(tutorial.tutorialOn)
            {
                morningMed[0].title = myMedication[i].Title;
                morningMed[0].dosage = dosage;
                morningMed[1].title = null;
                morningMed[1].dosage = 0;
                morningMed[2].title = null;
                morningMed[2].dosage = 0;
                morningMed[3].title = null;
                morningMed[3].dosage = 0;

                afternoonMed[0].title = null;
                afternoonMed[0].dosage = 0;
                afternoonMed[1].title = null;
                afternoonMed[1].dosage = 0;
                afternoonMed[2].title = null;
                afternoonMed[2].dosage = 0;
                afternoonMed[3].title = null;
                afternoonMed[3].dosage = 0;

                eveningMed[0].title = null;
                eveningMed[0].dosage = 0;
                eveningMed[1].title = null;
                eveningMed[1].dosage = 0;
                eveningMed[2].title = null;
                eveningMed[2].dosage = 0;
                eveningMed[3].title = null;
                eveningMed[3].dosage = 0;

                nightMed[0].title = null;
                nightMed[0].dosage = 0;
                nightMed[1].title = null;
                nightMed[1].dosage = 0;
                nightMed[2].title = null;
                nightMed[2].dosage = 0;
                nightMed[3].title = null;
                nightMed[3].dosage = 0;
            }
            /* If this medication has proposed times per day amount in JSON */
            else if (myMedication[i].timesPerDay > 0)
            {
                /* All day times */
                List<int> daytimes = new List<int>();
                daytimes.Add(0);
                daytimes.Add(1);
                daytimes.Add(2);
                daytimes.Add(3);
                /* Get random daytimes to give this medicine */
                int[] rnddaytimes = new int[myMedication[i].timesPerDay];
                int n = daytimes.Count;
                //shuffle daytimes
                while (n > 1)
                {
                    n--;
                    int k = Random.Range(0, n + 1);
                    int value = daytimes[k];
                    daytimes[k] = daytimes[n];
                    daytimes[n] = value;
                }
                for (int r = 0; r < rnddaytimes.Length; r++)
                {
                    rnddaytimes[r] = daytimes[r];
                }
                for (int r = 0; r < rnddaytimes.Length; r++)
                {
                    daytimes.Remove(rnddaytimes[r]);
                }
                //add medication to each random daytime array
                foreach (int daytime in rnddaytimes)
                {
                    switch(daytime)
                    {
                        case 0:
                            morningMed[i].title = myMedication[i].Title;
                            morningMed[i].dosage = dosage;
                            break;
                        case 1:
                            afternoonMed[i].title = myMedication[i].Title;
                            afternoonMed[i].dosage = dosage;
                            break;
                        case 2:
                            eveningMed[i].title = myMedication[i].Title;
                            eveningMed[i].dosage = dosage;
                            break;
                        case 3:
                            nightMed[i].title = myMedication[i].Title;
                            nightMed[i].dosage = dosage;
                            break;
                    }
                }
                //nullify remaining daytimes for this medicine
                if(daytimes.Count > 0)
                {
                    foreach(int daytime in daytimes)
                    {
                        switch (daytime)
                        {
                            case 0:
                                morningMed[i].title = null;
                                morningMed[i].dosage = 0;
                                break;
                            case 1:
                                afternoonMed[i].title = null;
                                afternoonMed[i].dosage = 0;
                                break;
                            case 2:
                                eveningMed[i].title = null;
                                eveningMed[i].dosage = 0;
                                break;
                            case 3:
                                nightMed[i].title = null;
                                nightMed[i].dosage = 0;
                                break;
                        }
                    }
                }
            }
            /* If timesperday is 0, just randomize how many times it will be given in a day*/
            else
            {
                List<int> daytimes = new List<int>();
                daytimes.Add(0);
                daytimes.Add(1);
                daytimes.Add(2);
                daytimes.Add(3);
                //randomize how many times this med will be given in a day
                int rnd = Random.Range(1, 4);
                //shuffle daytimes
                int n = daytimes.Count;
                while (n > 1)
                {
                    n--;
                    int k = Random.Range(0, n + 1);
                    int value = daytimes[k];
                    daytimes[k] = daytimes[n];
                    daytimes[n] = value;
                }
                int[] rnddaytimes = new int[rnd];
                for(int r = 0; r < rnd; r++)
                {
                    rnddaytimes[r] = daytimes[r];
                }
                for (int r = 0; r < rnddaytimes.Length; r++)
                {
                    daytimes.Remove(rnddaytimes[r]);
                }


                //add medication to each random daytime array
                foreach (int daytime in rnddaytimes)
                {
                    switch (daytime)
                    {
                        case 0:
                            morningMed[i].title = myMedication[i].Title;
                            morningMed[i].dosage = dosage;
                            break;
                        case 1:
                            afternoonMed[i].title = myMedication[i].Title;
                            afternoonMed[i].dosage = dosage;
                            break;
                        case 2:
                            eveningMed[i].title = myMedication[i].Title;
                            eveningMed[i].dosage = dosage;
                            break;
                        case 3:
                            nightMed[i].title = myMedication[i].Title;
                            nightMed[i].dosage = dosage;
                            break;
                    }
                }
                //nullify remaining daytimes for this medicine
                if (daytimes.Count > 0)
                {
                    foreach (int daytime in daytimes)
                    {
                        switch (daytime)
                        {
                            case 0:
                                morningMed[i].title = null;
                                morningMed[i].dosage = 0;
                                break;
                            case 1:
                                afternoonMed[i].title = null;
                                afternoonMed[i].dosage = 0;
                                break;
                            case 2:
                                eveningMed[i].title = null;
                                eveningMed[i].dosage = 0;
                                break;
                            case 3:
                                nightMed[i].title = null;
                                nightMed[i].dosage = 0;
                                break;
                        }
                    }
                }
            }
        }
    }

    /*
     * Tries to find closest position in navmesh to move to if unsuccesfull just force setdestination 
     * if agent isn't enabled, just remember the destination 
    */
    public void moveTo(Vector3 dest)
    {
        if(agent.enabled)
        {
            NavMeshHit hit;
            if (NavMesh.SamplePosition(dest, out hit, 10.0f, agent.areaMask))
                agent.SetDestination(new Vector3(hit.position.x, transform.position.y, hit.position.z));
            else
                agent.SetDestination(new Vector3(dest.x, transform.position.y, dest.z));
        }
        this.dest = dest;
            
    }

    /* Checks if medicine has been given for the current daytime, die if hp == 0, leave hospital if hp == 100 */
    void checkMed()
    {
        if(!paused)
        {
            if (isLosingHp)
            {
                // lose hp if no medicine is active, if hp reaches zero -> die
                deathTimer += Time.deltaTime;
                if (deathTimer >= LOSE_HP_TIME)
                {
                    myHp--;
                    deathTimer = 0;
                }

                // check if medicine has been activated and stop losing hp if so
                ClockTime.DayTime currTime = clock.currentDayTime;
                int count = 0;
                int count2 = 0;
                // MORNING
                if (currTime == ClockTime.DayTime.MORNING)
                {
                    foreach (Medicine med in morningMed)
                    {
                        if (med.title != null)
                        {
                            count++;
                        }
                        if (med.isActive)
                        {
                            count2++;
                        }
                    }
                    if (count == count2)
                    {
                        isLosingHp = false;
                    }
                }
                // AFTERNOON
                if (currTime == ClockTime.DayTime.AFTERNOON)
                {
                    foreach (Medicine med in afternoonMed)
                    {
                        if(med.title != null)
                        {
                            count++;
                        }
                        if (med.isActive)
                        {
                            count2++;
                        }
                    }
                    if(count == count2)
                    {
                        isLosingHp = false;
                    }
                }
                // EVENING
                if (currTime == ClockTime.DayTime.EVENING)
                {
                    foreach (Medicine med in eveningMed)
                    {
                        if (med.title != null)
                        {
                            count++;
                        }
                        if (med.isActive)
                        {
                            count2++;
                        }
                    }
                    if (count == count2)
                    {
                        isLosingHp = false;
                    }
                }
                // NIGHT
                if (currTime == ClockTime.DayTime.NIGHT)
                {
                    foreach (Medicine med in nightMed)
                    {
                        if (med.title != null)
                        {
                            count++;
                        }
                        if (med.isActive)
                        {
                            count2++;
                        }
                    }
                    if (count == count2)
                    {
                        isLosingHp = false;
                    }
                }
                
            }
            if (myHp <= 0)
            {
                addStateToQueue(3, NPCState.STATE_DEAD);
                taskCompleted = true;
            }
            if(myHp >= 100)
            {
                GetComponent<FloatTextNPC>().addFloatText("Health excellent! Leaving Hospital!", true);
                tutorial.ShowNotification("Your patient " + myName + " is healthy and has left the hospital!", 5f, false);
                if (playersResponsibility)
                {
                    GameObject.FindGameObjectWithTag("ScoringSystem").GetComponent<ScoringSystem>().responsibilityNPCLeftHospital();
                    npcManager.respNpcsWhoLeftOrDied.Add(new NPCINFO(myName, myHead2d, false));
                }
                addStateToQueue(3, NPCState.STATE_LEAVE_HOSPITAL);
                objectManager.unbookObject(myBed);
                taskCompleted = true;
                
            }
        }
        
    }
    public bool giveMed(string[] med, float[] dosage)
    {
        // make sure the NPC is diagnosed and player is near enough
        if (diagnosed && dialogZone.playerInZone && myState != NPCState.STATE_DEAD )
        {
            // check the current time of the day to do the correct comparsion
            ClockTime.DayTime currTime = GameObject.FindGameObjectWithTag("Clock").GetComponent<ClockTime>().currentDayTime;
            //incorrect rating, add 1 for every error
            int incorrect = 0;
            int correct = 0;
            Medicine[] meds = new Medicine[4];
            // MORNING
            if (currTime == ClockTime.DayTime.MORNING)
            {
                meds = morningMed;
            }
            else if (currTime == ClockTime.DayTime.AFTERNOON)
            {
                meds = afternoonMed;
            }
            else if (currTime == ClockTime.DayTime.EVENING)
            {
                meds = eveningMed;
            }
            else if (currTime == ClockTime.DayTime.NIGHT)
            {
                meds = nightMed;
            }


            List<string> wrongmeds = new List<string>();
            for (int i = 0; i < med.Length; i++)
            {
                bool found = false;
                //iterate through all morning meds
                for (int j = 0; j < meds.Length; j++)
                {
                    if (string.Equals(med[i], meds[j].title, System.StringComparison.CurrentCultureIgnoreCase))
                    {
                        //if the med was already given, giving more means overdose
                        if(meds[j].isActive)
                        {
                            incorrect += 2;
                            //GetComponent<FloatTextNPC>().addFloatText(FloatText.IncorrectMedicine);
                            GetComponent<FloatTextNPC>().addFloatText("Overdose of " + med[i] + "!", false);
                        }
                        else if (dosage[i] == meds[j].dosage)
                        {
                            found = true;
                            meds[j].isActive = true;
                            GetComponent<FloatTextNPC>().addFloatText("Correct Medicine! " + med[i], true);
                            correct++;
                        }
                        else
                        {
                            if(dosage[i] > meds[j].dosage)
                            {
                                GetComponent<FloatTextNPC>().addFloatText("Overdose of " + med[i] + "!", false);
                                incorrect += 2;
                            }
                            else if (dosage[i] < meds[j].dosage)
                            {
                                GetComponent<FloatTextNPC>().addFloatText("dosage of " + med[i] + " was too small!", false);
                            }
                        }
                    }
                    if (found)
                    {
                        break;
                    }
                }
                if (!found)
                {
                    wrongmeds.Add(med[i]);
                    //GetComponent<FloatTextNPC>().addFloatText(FloatText.IncorrectMedicine);
                    GetComponent<FloatTextNPC>().addFloatText("Incorrect Medicine! " + med[i], false);
                }
            }
            for (int i = 0; i < wrongmeds.Count; i++)
            {
                incorrect += 2;
            }


            //how many meds should the patient be given
            int medslength = 0;

            foreach(Medicine m in meds)
            {
                if(m.title != null)
                {
                    medslength++;
                }
            }

            float givenratio = 0;

            //don't divide by 0
            if (medslength > 0)
            {
                //ratio of how many correct meds were given out of all daytime meds
                givenratio = (float)(medslength - correct) / medslength;
            }

            int medcount = correct + incorrect;

            float temp = correct / medcount;
            float correctratio = temp - (incorrect / medcount);
            
            if (correctratio >= 0)
            {
                correctratio -= givenratio;
                correctSound.Play();
                if (correctratio == 0)
                {
                    GetComponent<FloatTextNPC>().addFloatText("+ 0", true);
                }
                else if (correctratio > 0 && correctratio <= 0.34f)
                {
                    GetComponent<FloatTextNPC>().addFloatText("+ 5", true);
                    if(playersResponsibility)
                        scoreSystem.addToScore(2);
                    myHp += 5;
                }
                else if (correctratio > 0.34f && correctratio <= 0.67f )
                {
                    GetComponent<FloatTextNPC>().addFloatText("+ 10", true);
                    if (playersResponsibility)
                        scoreSystem.addToScore(5);
                    myHp += 10;
                }
                else if (correctratio > 0.67f && correctratio < 1f )
                {
                    GetComponent<FloatTextNPC>().addFloatText("+ 15", true);
                    if (playersResponsibility)
                        scoreSystem.addToScore(6);
                    myHp += 15;
                }
                else if (correctratio >= 1f)
                {
                    GetComponent<FloatTextNPC>().addFloatText("+ 20", true);
                    if (playersResponsibility)
                        scoreSystem.addToScore(8);
                    myHp += 20;
                }
            }
            else if(correctratio < 0)
            {
                wrongSound.Play();
                if (correctratio < 0 && correctratio >= -0.25f)
                {
                    GetComponent<FloatTextNPC>().addFloatText("- 5", false);
                    scoreSystem.addToScore(-2);
                    myHp -= 5;
                }
                else if (correctratio < -0.25f && correctratio >= -0.5f)
                {
                    GetComponent<FloatTextNPC>().addFloatText("- 10", false);
                    scoreSystem.addToScore(-5);
                    myHp -= 10;
                }
                else if (correctratio < -0.5f && correctratio >= -0.75f)
                {
                    GetComponent<FloatTextNPC>().addFloatText("- 15", false);
                    scoreSystem.addToScore(-7);
                    myHp -= 15;
                }
                else if (correctratio < -0.75f)
                {
                    GetComponent<FloatTextNPC>().addFloatText("- 20", false);
                    scoreSystem.addToScore(-10);
                    myHp -= 20;
                }
            }

            return true;
        }
        else return false;
    }

    public void disableAllMeds()
    {
        for(int i = 0; i < morningMed.Length; i++)
        {
            morningMed[i].isActive = false;
        }
        for (int i = 0; i < afternoonMed.Length; i++)
        {
            afternoonMed[i].isActive = false;
        }
        for (int i = 0; i < eveningMed.Length; i++)
        {
            eveningMed[i].isActive = false;
        }
        for (int i = 0; i < nightMed.Length; i++)
        {
            nightMed[i].isActive = false;
        }
    }

    //Can't init at start, because children should be initiated first
    public void initChild()
    {
        dialogZone = transform.FindChild("ContactZone").GetComponent<Dialog>();
    }

    private bool approx(float a, float b, float accuracy)
    {
        float sub = a - b;
        if(Mathf.Abs(sub) < accuracy)
        {
            return true;
        }
        return false;
    }

    /* Draw line for patients path in unity if gizmos are selected */
    void OnDrawGizmosSelected()
    {

        if (agent == null || agent.path == null)
            return;

        var line = this.GetComponent<LineRenderer>();
        if (line == null)
        {
            line = this.gameObject.AddComponent<LineRenderer>();
            line.material = new Material(Shader.Find("Sprites/Default")) { color = Color.yellow };
            line.SetWidth(0.5f, 0.5f);
            line.SetColors(Color.black, Color.black);
        }

        var path = agent.path;

        line.SetVertexCount(path.corners.Length);

        for (int i = 0; i < path.corners.Length; i++)
        {
            line.SetPosition(i, path.corners[i]);
        }

    }
}



