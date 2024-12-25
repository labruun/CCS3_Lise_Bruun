 using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Random = UnityEngine.Random;
using Setup;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine.Rendering.Universal.Internal;

/*
public class TargetControllerPilot : MonoBehaviour
{
    public static TargetControllerPilot Controller { get; private set; }
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
    public GameObject[] targetPrefabs;
    public Color targetColor1;
    public Color targetColor2;
    private List<Color> colors;
    public Transform space;

    [HideInInspector]
    public static List<Target> Targets = new List<Target>();
    [HideInInspector]
    public static List<Target> Distractors = new List<Target>();

    private EyeTracking gaze;
    private KeyCode triggerSelection = KeyCode.Return;
    private float displayTime = 10f;
    private bool textActive = true;

    public GameObject FlickeringCubes;
    public List<GameObject> FlickeringCubesList;

    private float FlickerHigh = 0.1f;
    private float FlickerLow = 0.001f;

    [SerializeField]
    public static GameObject CrossFaded;

    public static int CalibrationRound = 0;
    public static int CalibrationTarget = 0;

    public List<List<(int, Color, int)>> orderTargets = new List<List<(int, Color, int)>>();

    private Target hitObject;
    private int targetCounter;
    private static readonly string[] ColumnNamesFlickering = { "U_Frame", "Quadrant", "UpOrDownArrow", "CurrentFlickerValue", "CalibrationRound", "Reversal" };

    // Start is called before the first frame update
    void Start()
    {
        Logging.Logger.RecordFlickeringCalibration(ColumnNamesFlickering, StudyController.participantID);
        CrossFaded = GameObject.Find("CrossFaded");
        //CrossFaded.transform.position = Camera.main.transform.position + Camera.main.transform.forward * 1f;
        space.transform.SetParent(Camera.main.transform, false);
        space.transform.localPosition = new Vector3(0.115000002f, -0.0649999976f, 0); // Vector3(0.115000002,-0.0649999976,0)
        CrossFaded.transform.localPosition = Camera.main.transform.position + Vector3.forward * 2.15f;

        gaze = EyeTracking.Tracker;
        gaze.Register(onGazeUpdate);
        targetCounter = 0;

        foreach (Transform child in FlickeringCubes.transform)
        {
            FlickeringCubesList.Add(child.gameObject);
        }

        colors = new List<Color>();
        colors.Add(targetColor2);
        colors.Add(targetColor1);

        orderTargets = new List<List<(int, Color, int)>>();
        //(0, RGBA(0.098, 0.098, 0.902, 0.000), 0)
        List<(int, Color, int)> targetposWithInt = new List<(int, Color, int)>();
        targetposWithInt.Add((0, colors[1], 0));
        targetposWithInt.Add((1, colors[0], 0));
        targetposWithInt.Add((0, colors[1], 1));
        targetposWithInt.Add((1, colors[0], 1));
        orderTargets.Add(targetposWithInt);
        targetposWithInt = new List<(int, Color, int)>();
        targetposWithInt.Add((0, colors[0], 0));
        targetposWithInt.Add((1, colors[1], 0));
        targetposWithInt.Add((0, colors[0], 1));
        targetposWithInt.Add((1, colors[1], 1));
        orderTargets.Add(targetposWithInt);
        //for (int i = 0; i < 2; i++)
        //{
        //    // Initialize a new list for each iteration of the outer loop
        //    List<(int, Color, int)> targetposWithInt = new List<(int, Color, int)>();
        //    print("Creating new list");
        //    for (int j = 0; j < 2; j++)
        //    {
        //        for (int k = 0; k < 2; k++)
        //        {
        //            // Calculate the index for alternating between 0 and 1
        //            int intValue = (j + i) % 2;
        //            // Add tuple with prefab and color
        //            targetposWithInt.Add((k, colors[intValue], intValue));
        //        }
        //    }
        //    // Shuffle the list
        //    targetposWithInt = targetposWithInt.OrderBy(x => Random.value).ToList();
        //    // Add the shuffled list to the orderTargets list
        //    orderTargets.Add(targetposWithInt);
        //}



        Localise();
        Restart();
    }

    void Update()
    {
        

        if (displayTime < 0 && textActive)
        {
            GameObject.Find("PracticeText").SetActive(false);
            textActive = false;
        }
        else
        {
            displayTime -= Time.deltaTime;        
        }
    }

    List<(List<Vector3>, int)> GeneratePermutations2(List<Vector3> vectors)
    {
        List<(List<Vector3>, int)> permutations = new List<(List<Vector3>, int)>();

        for (int i = 0; i < 16; i++) {
            List<Vector3> perm = new List<Vector3>();
            for (int j = 0; j < 4; j++) {
                perm.Add(vectors[(i >> (2 * j)) & 3]);
            }
            permutations.Add((perm, i));
        }
        

        return permutations;
    }

    List<(List<Vector3>, int)> GeneratePermutations(List<Vector3> vectors)
    {
        List<(List<Vector3>, int)> permutations = new List<(List<Vector3>, int)>();

        GeneratePermutationsHelper(vectors, new List<Vector3>(), permutations, 0);
        GeneratePermutationsHelper(vectors, new List<Vector3>(), permutations, 1);
        //Append an int between 1-4 to end of each permutation
        

        return permutations;
    }

    void GeneratePermutationsHelper(List<Vector3> remaining, List<Vector3> currentPermutation, List<(List<Vector3>, int)> permutations, int flag)
    {
        if (remaining.Count == 0)
        {
            permutations.Add((new List<Vector3>(currentPermutation), flag));
            return;
        }

        for (int i = 0; i < remaining.Count; i++)
        {
            Vector3 vector = remaining[i];
            List<Vector3> newRemaining = new List<Vector3>(remaining);
            newRemaining.RemoveAt(i);

            List<Vector3> newPermutation = new List<Vector3>(currentPermutation);
            newPermutation.Add(vector);

            GeneratePermutationsHelper(newRemaining, newPermutation, permutations, flag);
        }
    }

    public void Restart()
    {
        // Destroy all targets
        foreach (Target t in Targets)
        {
            Destroy(t.getGameObject());
        }
        InstantiateTargets(targetpos);
        InstantiateDistractors(distractorpos);
        //print("Restarting :)");
    }
    // 0 == top left, 1 == bottom left, 2 == bottom right, 3 == top right
    public enum Quadrant {
        TopLeft = 0,
        BottomLeft = 1,
        BottomRight = 2,
        TopRight = 3
    }


    public static string currentQuadrant()
    {
        return ((Quadrant)CalibrationTarget).ToString();
    }

    public static string currentRound()
    {
        return CalibrationRound.ToString();
    }

    public void RestartRound()
    {
        // Destroy all targets
        foreach (Target t in Targets)
        {
            Destroy(t.getGameObject());
        }

        CalibrationTarget++;
        if ( CalibrationTarget == 4)
        {
            CalibrationRound++;
            CalibrationTarget = 0;
        }
        InstantiateTargetsCalibration(targetpos);
    }


    void OnDisable()
    {
         space?.gameObject.SetActive(false);
    }

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

    void InstantiateTargetsCalibration(List<Vector3> targetpos)
    {
        Targets = new List<Target>();
        for (int i = 0; i < targetpos.Count; i++)
        {
            if (i == CalibrationTarget % targetpos.Count)
            {
                Targets.Add(Instantiate(orderTargets[CalibrationRound][CalibrationTarget].Item1, targetpos[i], orderTargets[CalibrationRound][CalibrationTarget].Item2));
                SphereFlicker.flickerIntensity = orderTargets[CalibrationRound][CalibrationTarget].Item3 == 0 ? FlickerLow : FlickerHigh;
                FlickeringCubesList[i].SetActive(true);
            }
            else
            {
                FlickeringCubesList[i].SetActive(false);
                // Random Color from colors varialbe
                Color randomColor = colors[Random.Range(0, colors.Count)];
                Targets.Add(Instantiate(Random.Range(0, 2), targetpos[i], randomColor));
            }

        }
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
        obj.getGameObject().transform.localRotation = Quaternion.Euler(0, 0, 0);

        return obj;
    }

    void Localise()
    {
        Vector3 pos = space.transform.position;
        Vector3 camPos = Camera.main.transform.position;
        //space.transform.position = new Vector3(camPos.x, camPos.y,camPos.z);
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
//0.11
*/