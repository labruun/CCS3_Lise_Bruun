using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR;
using Varjo.XR;
using static Varjo.XR.VarjoEyeTracking;
using System.Collections;

public class EyeTrackingValidation : MonoBehaviour
{
    public GameObject space;
    // Get eye tracker
    private EyeTracking tracker;

    public GameObject text;

    public GameObject text2;

    public GameObject text3;

    public List<GameObject> validationTargets = new List<GameObject>();

    public int FRAMES_COUNT = 300;

    private int frameCounter = 0;

    private GameObject currentTargetLooking;

    public float distanceThreshold = 0.5f;

    private float distanceAverage = 0;

    public float totalAverage = 0;


    private int targetCompletedCounter = 0;

    public bool calibrated = false;

    private bool validationStarted = false;

    public bool finishedValidation = false;

    private List<Vector2> fixationPoints = new List<Vector2>();

    private static readonly string[] validationColumns = { "validationTargetName", "validationTargetPosition", "fixationPoints"};

    private Vector3 fixationPoint;
    // Start is called before the first frame update
    void Start()
    {
        tracker = EyeTracking.Tracker;
        space.SetActive(false);
        foreach (GameObject target in validationTargets)
        {
            target.GetComponent<MeshRenderer>().material.color = Color.red;
            
        }
        Logging.Logger.RecordValidationInfo(validationColumns, StudyController.participantID);
    }

    // Update is called once per frame
    void Update()
    {
        //foreach (GameObject target in validationTargets)
        //{
        //    print(target.name + ": " + target.transform.position.ToString());
        //}
        if (finishedValidation)
        {
            text3.SetActive(true);
        }
        if (Input.GetKeyDown(KeyCode.Space))
        {
            calibrated = true;
            space.transform.SetParent(Camera.main.transform, true);
            space.transform.localPosition = new Vector3(0, 0, 1.5f);
            space.transform.localRotation = Quaternion.Euler(0, 0, 0);
        }
        
        if (!calibrated)
        {
            return;
        }

        if (validationStarted == false)
        {
            text.SetActive(false);
            // Sleep for 3 seconds
            System.Threading.Thread.Sleep(3000);
            text2.SetActive(true);
            validationStarted = true;
            return;
        }

        if (Input.GetKeyDown(KeyCode.Return))
        {
            text2.SetActive(false);
            space.SetActive(true);
        }

        if (space.activeSelf == false)
        {
            return;
        }

        if (targetCompletedCounter == validationTargets.Count)
        {
            if (!finishedValidation)
                print("Final average distance: " + totalAverage + ", in procent: " + (1- (totalAverage / distanceThreshold)));
            finishedValidation = true;
            space.SetActive(false);
            return;
        }
        fixationPoint = tracker.gazeInfo.fixationPoint;
        foreach (GameObject target in validationTargets)
        {
            if (target.GetComponent<MeshRenderer>().material.color == Color.white)
            {
                continue;
            }
            float distanceToTarget = (Mathf.Abs(target.transform.position.z - Camera.main.transform.position.z));
            Vector3 PlaneCoordinates = new Vector3(Camera.main.transform.position.x, Camera.main.transform.position.y, Camera.main.transform.position.z - (-distanceToTarget));
            Plane plane = new Plane(Camera.main.transform.forward, PlaneCoordinates);
            Ray fixationRayOrigin = new Ray(Camera.main.transform.position, fixationPoint - Camera.main.transform.position);
            if (plane.Raycast(fixationRayOrigin, out float distance))
            {
                Vector2 originPoint = new Vector2(fixationRayOrigin.GetPoint(distance).x, fixationRayOrigin.GetPoint(distance).y);
                if (Vector2.Distance(originPoint, new Vector2(target.transform.position.x, target.transform.position.y)) < distanceThreshold)
                {
                    if (currentTargetLooking != null && currentTargetLooking == target)
                    {
                        frameCounter++;
                        distanceAverage += Vector2.Distance(originPoint, new Vector2(target.transform.position.x, target.transform.position.y));
                        fixationPoints.Add(originPoint);
                        // Make it greener for every consequent frame
                        target.GetComponent<MeshRenderer>().GetComponent<Renderer>().material.color = Color.Lerp(Color.red, Color.green, (float)frameCounter / FRAMES_COUNT);
                        if (frameCounter == FRAMES_COUNT)
                        {
                            LogValidation(target.name, new Vector2(target.transform.position.x, target.transform.position.y), fixationPoints);
                            target.GetComponent<MeshRenderer>().material.color = Color.white;
                            frameCounter = 0;
                            targetCompletedCounter++;
                            distanceAverage /= FRAMES_COUNT;
                            totalAverage += distanceAverage;
                            totalAverage /= targetCompletedCounter;
                            distanceAverage = 0;
                            fixationPoints.Clear();
                        }
                    }
                    else if (currentTargetLooking != target && target.GetComponent<Renderer>().material.color != Color.white)
                    {
                        distanceAverage = 0;
                        frameCounter = 0;
                        target.GetComponent<MeshRenderer>().material.color = Color.red;
                        fixationPoints.Clear();
                    }
                    currentTargetLooking = target;
                }
            }
        }
    }
    void LogValidation(string validationTarget, Vector2 validationTarget2D, List<Vector2> fixationPoints)
    {
        string[] msg = new string[validationColumns.Length];
        msg[0] = validationTarget;
        msg[1] = validationTarget2D.ToString();
        msg[2] = "[" + string.Join(",", fixationPoints) + "]";

        Logging.Logger.RecordValidationInfo(msg, StudyController.participantID);
    }
}



