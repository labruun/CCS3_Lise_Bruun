using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Random = UnityEngine.Random;
using Setup;
using System.Linq;

/*
public class TargetControllerPractice : MonoBehaviour
{

    public static TargetControllerPractice Controller { get; private set; }
    private void Awake()
    {
        // If there is an instance, and it's not me, delete myself.

        if (Controller != null && Controller != this)
        {
            Debug.LogWarning("Only one controller instance allowed");
            Destroy(this);
        }
        else
        {
            Controller = this;
        }
    }

    public List<Vector3> targetpos, distractorpos;
    public GameObject SpaceGO;
    public GameObject[] targetPrefabs;
    public Color targetColor1;
    public Color targetColor2;
    public Transform space;

    [HideInInspector]
    private static List<Target> Targets = new List<Target>();
    [HideInInspector]
    private static List<Target> Distractors = new List<Target>();

    private EyeTracking gaze;
    private KeyCode triggerSelection = KeyCode.Return;
    private float displayTime = 3f;
    private bool textActive = true;

    private Target hitObject;
    private int targetCounter;

    // Start is called before the first frame update
    void Start()
    {
        gaze = EyeTracking.Tracker;
        gaze.Register(onGazeUpdate);
        targetCounter = 0;
        
        Localise();
        Restart();
    }

    void Update()
    {
        
        if(displayTime < 0 && textActive)
        {
            GameObject.Find("PracticeText").SetActive(false);
            textActive = false;
            SpaceGO.SetActive(true);
        }
        else
        {
            displayTime -= Time.deltaTime;        
        }
    }

    public void Restart()
    {
        InstantiateTargets(targetpos);
        InstantiateDistractors(distractorpos);
    }


    //void OnDisable()
    //{
    //    if (space != null)
    //     space?.gameObject.SetActive(false);
    //}

    public void onGazeUpdate()
    {
        //ACCOUNT FOR INaccuracy and make nicer to access
        if (gaze.gazeInfo.hit.transform is null)
        {
            hitObject = null;
            return;
        }
        GameObject hit = gaze.gazeInfo.hit.transform.gameObject;
        
        bool target = Contains(Targets, hit);
        bool distractor = Contains(Distractors, hit);

        if (target||distractor) // check if hit object is a target or a distractor
        {
            Target t = null;
            if(Input.GetKeyDown(triggerSelection))
            {
                if(target)
                {
                    t = HitTarget(Targets, hit);
                }
                else if(distractor)
                {
                    t = HitTarget(Distractors, hit);
                }
            }
            if(t is not null)
            {
                t.select();
                targetCounter++;
            }
        }
        else
        {
            hitObject = null;
        }
    }

    private bool Contains(List<Target> list, GameObject obj)
    {
        bool contains = false;
        for (int i = 0; i < list.Count; i++)
        {
            if(!list[i].hasBeenSelected() && list[i].getGameObject().Equals(obj))
            {
                contains = true;
            }
        }

        return contains;
    }

    private Target HitTarget(List<Target> list, GameObject obj)
    {
        for (int i = 0; i < list.Count; i++)
        {
            if(!list[i].hasBeenSelected() && list[i].getGameObject().Equals(obj))
            {
                return list[i];
            }
        }
        return null;
    }
        
    void InstantiateTargets(List<Vector3> targetpos)
    {
        Targets = new List<Target>();
        for (int i = 0; i < targetpos.Count/2; i++)
        {
            Targets.Add(Instantiate(0, targetpos[i], targetColor1));
        }
        for (int i = targetpos.Count/2; i < targetpos.Count; i++)
        {
            Targets.Add(Instantiate(1, targetpos[i], targetColor2));
        }
    }
    void InstantiateDistractors(List<Vector3> distractorpos)
    {
        Distractors = new List<Target>();
        for (int i = 0; i < distractorpos.Count/2; i++)
        {
            Distractors.Add(Instantiate(0, distractorpos[i], targetColor2));
        }
        for (int i = distractorpos.Count/2; i < distractorpos.Count; i++)
        {
            Distractors.Add(Instantiate(1, distractorpos[i], targetColor1));
        }
    }

    Target Instantiate(int prefab, Vector3 position, Color color)
    {
        Target obj = new Target(Instantiate(targetPrefabs[prefab], position, Quaternion.identity, space), color);
        obj.getGameObject().transform.localPosition = position;
        obj.getGameObject().GetComponent<Renderer>().material.SetColor("_BaseColor", color);
        obj.getGameObject().transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
       
        return obj;
    }

    void Localise()
    {
        Vector3 pos = space.transform.position;
        Vector3 camPos = Camera.main.transform.position;
        //space.transform.position = new Vector3(camPos.x, camPos.y, camPos.z + 1f);
        //Vector3 forward = Camera.main.transform.forward;
        //space.transform.LookAt(camPos + new Vector3(forward.x, 0, forward.z));
    }

    public bool getCompletionStatus()
    {
        if(targetCounter == targetpos.Count)
            return true;
        else
            return false;
    }
}
*/