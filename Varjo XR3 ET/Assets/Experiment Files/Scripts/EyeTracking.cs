using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using System;
using Varjo.XR;

public class EyeTracking : MonoBehaviour
{
    public static EyeTracking Tracker { get; private set; }
    
    private void Awake()
    {
        // If there is an instance, and it's not me, delete myself.
        Debug.Log("ET awake");

        if (Tracker != null && Tracker != this)
        {
            Debug.LogWarning("Only one tracking instance allowed");
            Destroy(this);
        }
        else
        {
            Tracker = this;
        }
    }

    public struct GazeInfo
    {
        public Vector3 fixationPoint;
        public Vector3 direction;
        public Vector3 rayOrigin;

        public Vector3 combinedGazeForward;

        public RaycastHit hit;
        public float distance;
    }

    private struct DummyTrans
    {
        public Vector3 position;
        public Quaternion rotation;
    }

    private List<Action> callbacks = new List<Action>();

    [Header("Gaze calibration settings")]
    public VarjoEyeTracking.GazeCalibrationMode gazeCalibrationMode = VarjoEyeTracking.GazeCalibrationMode.Fast;

    [Header("Gaze output filter settings")]
    public VarjoEyeTracking.GazeOutputFilterType gazeOutputFilterType = VarjoEyeTracking.GazeOutputFilterType.Standard;

    [Header("Gaze data output frequency")]
    public VarjoEyeTracking.GazeOutputFrequency frequency;

    [Header("XR camera")]
    public Camera xrCamera;

    [Header("Gaze point indicator")]
    public GameObject gazeTarget;

    [Header("Default path is Logs under application data path/ETHighlight_Components/Logs.")]
    public bool useCustomLogPath = false;
    public string customLogPath = "";

    [Header("Print gaze data framerate while logging.")]
    public bool printFramerate = false;

    private DummyTrans[] eyes;
    private VarjoEyeTracking.GazeData gazeData;

    public float gazeRadius = 0.01f;

    public GazeInfo gazeInfo;

    public List<VarjoEyeTracking.GazeData> dataSinceLastUpdate;
    public List<VarjoEyeTracking.EyeMeasurements> eyeMeasurementsSinceLastUpdate;

    private static readonly string[] ColumnNames = { "U_Frame", "CaptureTime", "LogTime", "HMDPosition", "HMDRotation", "GazeStatus", "CombinedGazeForward", "CombinedGazePosition", "InterPupillaryDistanceInMM", "LeftEyeStatus", "LeftEyeForward", "LeftEyePosition", "LeftPupilIrisDiameterRatio", "LeftPupilDiameterInMM", "LeftIrisDiameterInMM", "RightEyeStatus", "RightEyeForward", "RightEyePosition", "RightPupilIrisDiameterRatio", "RightPupilDiameterInMM", "RightIrisDiameterInMM", "FocusDistance", "FocusStability", "FixationPoint" };
    private int participantID;
    private const string ValidString = "VALID";
    private const string InvalidString = "INVALID";
    int gazeDataCount = 0;
    float gazeTimer = 0f;
    public bool showGaze = false;
    public float targetOffset = 10f;
    public float floatingGazeTargetDistance = 1.0f;

    public bool loggingOn = true;
    // Start is called before the first frame update
    void Start()
    {
        eyes = new DummyTrans[2] { new DummyTrans(), new DummyTrans() };
        if(loggingOn)
        {
            Logging.Logger.RecordGaze(ColumnNames, StudyController.participantID);
        }
    }

    // Update is called once per frame
    void Update()
    {
        gazeData = VarjoEyeTracking.GetGaze();
        var validHit = false;
        if (gazeData.status != VarjoEyeTracking.GazeStatus.Invalid)
        {
            // GazeRay vectors are relative to the HMD pose so they need to be transformed to world space
            if (gazeData.leftStatus != VarjoEyeTracking.GazeEyeStatus.Invalid)
            {
                eyes[0].position = xrCamera.transform.TransformPoint(gazeData.left.origin);
                eyes[0].rotation = Quaternion.LookRotation(xrCamera.transform.TransformDirection(gazeData.left.forward));
            }

            if (gazeData.rightStatus != VarjoEyeTracking.GazeEyeStatus.Invalid)
            {
                eyes[1].position = xrCamera.transform.TransformPoint(gazeData.right.origin);
                eyes[1].rotation = Quaternion.LookRotation(xrCamera.transform.TransformDirection(gazeData.right.forward));
            }

            // Set gaze origin as raycast origin
            gazeInfo.rayOrigin = xrCamera.transform.TransformPoint(gazeData.gaze.origin);

            // Set gaze direction as raycast direction
            gazeInfo.direction = xrCamera.transform.TransformDirection(gazeData.gaze.forward);

            // Store gaze direction for saccade detection
            gazeInfo.combinedGazeForward = gazeData.gaze.forward;

            // Fixation point can be calculated using ray origin, direction and focus distance
            gazeInfo.fixationPoint = gazeInfo.rayOrigin + gazeInfo.direction * gazeData.focusDistance;
            validHit = Physics.SphereCast(gazeInfo.rayOrigin, gazeRadius, gazeInfo.direction, out gazeInfo.hit);
            if (!validHit)
            {
                gazeInfo.hit = new RaycastHit();
            }
        }
        if (showGaze)
        {
            // Raycast to world from VR Camera position towards fixation point
            if (validHit)
            {
                // Put target on gaze raycast position with offset towards user
                gazeTarget.transform.position = gazeInfo.hit.point - gazeInfo.direction * targetOffset;

                // Make gaze target point towards user
                gazeTarget.transform.LookAt(gazeInfo.rayOrigin, Vector3.up);

                // Scale gazetarget with distance so it apperas to be always same size
                gazeInfo.distance = gazeInfo.hit.distance;
                //gazeTarget.transform.localScale = Vector3.one * gazeInfo.distance;
            }
            else
            {
                // If gaze ray didn't hit anything, the gaze target is shown at fixed distance
                gazeTarget.transform.position = gazeInfo.rayOrigin + gazeInfo.direction * floatingGazeTargetDistance;
                gazeTarget.transform.LookAt(gazeInfo.rayOrigin, Vector3.up);
                //gazeTarget.transform.localScale = Vector3.one * floatingGazeTargetDistance;
            }
        }
        int dataCount = VarjoEyeTracking.GetGazeList(out dataSinceLastUpdate, out eyeMeasurementsSinceLastUpdate);
        if (printFramerate) gazeDataCount += dataCount;
        foreach (Action a in callbacks)
        {
            a();
        }
        for (int i = 0; i < dataCount; i++)
        {
            if(loggingOn)
            {
                LogGazeData(dataSinceLastUpdate[i], eyeMeasurementsSinceLastUpdate[i]);
            }
        }
    }
    void LogGazeData(VarjoEyeTracking.GazeData data, VarjoEyeTracking.EyeMeasurements eyeMeasurements)
    {
        string[] logData = new string[24];

        // Gaze data frame number
        logData[0] = Time.frameCount.ToString();

        // Gaze data capture time (nanoseconds)
        logData[1] = data.captureTime.ToString();

        // Log time (milliseconds)
        logData[2] = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond).ToString();

        // HMD
        logData[3] = xrCamera.transform.localPosition.ToString("F6");
        logData[4] = xrCamera.transform.localRotation.ToString("F6");

        // Combined gaze
        bool invalid = data.status == VarjoEyeTracking.GazeStatus.Invalid;
        logData[5] = invalid ? InvalidString : ValidString;
        logData[6] = invalid ? "" : data.gaze.forward.ToString("F6");
        logData[7] = invalid ? "" : data.gaze.origin.ToString("F6");

        // IPD
        logData[8] = invalid ? "" : eyeMeasurements.interPupillaryDistanceInMM.ToString("F6");

        // Left eye
        bool leftInvalid = data.leftStatus == VarjoEyeTracking.GazeEyeStatus.Invalid;
        logData[9] = leftInvalid ? InvalidString : ValidString;
        logData[10] = leftInvalid ? "" : data.left.forward.ToString("F6");
        logData[11] = leftInvalid ? "" : data.left.origin.ToString("F6");
        logData[12] = leftInvalid ? "" : eyeMeasurements.leftPupilIrisDiameterRatio.ToString("F6");
        logData[13] = leftInvalid ? "" : eyeMeasurements.leftPupilDiameterInMM.ToString("F6");
        logData[14] = leftInvalid ? "" : eyeMeasurements.leftIrisDiameterInMM.ToString("F6");

        // Right eye
        bool rightInvalid = data.rightStatus == VarjoEyeTracking.GazeEyeStatus.Invalid;
        logData[15] = rightInvalid ? InvalidString : ValidString;
        logData[16] = rightInvalid ? "" : data.right.forward.ToString("F6");
        logData[17] = rightInvalid ? "" : data.right.origin.ToString("F6");
        logData[18] = rightInvalid ? "" : eyeMeasurements.rightPupilIrisDiameterRatio.ToString("F6");
        logData[19] = rightInvalid ? "" : eyeMeasurements.rightPupilDiameterInMM.ToString("F6");
        logData[20] = rightInvalid ? "" : eyeMeasurements.rightIrisDiameterInMM.ToString("F6");

        // Focus
        logData[21] = invalid ? "" : data.focusDistance.ToString();
        logData[22] = invalid ? "" : data.focusStability.ToString();

        logData[23] = invalid ? "" : gazeInfo.fixationPoint.ToString("F6");

        Logging.Logger.RecordGaze(logData, participantID);
    }

    public void Register(Action a)
    {
        callbacks.Add(a);
    }

    public void Unregister(Action a)
    {
        callbacks.Remove(a);
    }

    public void Calibrate()
    {
        VarjoEyeTracking.RequestGazeCalibration(gazeCalibrationMode);
    }

    public void setParticipantID(int id)
    {
        participantID = id;
    }
}