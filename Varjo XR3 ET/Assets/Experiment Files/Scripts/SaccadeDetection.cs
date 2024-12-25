using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR;
using Varjo.XR;


public class SaccadeDetection : MonoBehaviour
{
    public static SaccadeDetection Detector { get; private set; }

    public SaccadeDetection(int id)
    {
        Detector = new SaccadeDetection(id);
        participantID = id;
    }

    // Press space to start calibratiion
    public KeyCode calibrationRequestKey = KeyCode.Space;

    [Header("Set to true for saccade detection from a file.")]
    public bool detectFromFile = true;
    [Header("Specficy the path for saccade detection from a file.")]
    public string filePath = ""; //"/ETHighlight_Components/", "sac_samples.csv";
    private string defaultFilePath = "/ETHighlight_Components/sac-samples-2023-07-26-17-25.csv";

    [Header("Set to true for online saccade detection.")]
    public bool detectOnline = false;

    // bufferSize in seconds
    [Header("Enter buffer size for median calculation in seconds.")]
    public int bufferSize = 5;

    [Header("Enter lambda for saccade detection.")]
    public float lambda = 6;

    [Header("Enter filter for saccade detection.")]
    public float filterS = 0.5f;

    [Header("Enter milliseconds for saccade duration threshold.")]
    public int saccadeDurationThreshold = 100;

    [Header("Enable eye tracking logging.")]
    public bool eyeTrackingLogging = false;

    [Header("Enable saccade logging.")]
    public bool saccadeLogging = false;

    [HideInInspector]
    public static bool saccadeDetected; 

    [HideInInspector]
    public static bool ongoingSaccade;

    [HideInInspector]
    public static Vector3 currentPos; // current gaze position

    // Logging Saccade Detection
    private readonly static string[] sac_ColumnNames = { "U_Frame",  "x", "y", "velocities x", "velocities y", "power x", "power y", "median of Pow(x)", "median of Pow(y)", "pow of Median(x)", "pow of Median(y)", "median SD x", "median SD y", "radius x", "radius y", "saccade"};
    private int participantID;

    // Variables for saccade detection
    private Vector3[] mdnSDPositions; // Array that builds up over X seconds to enhance saccade detection over time, 
    private DateTime saccadeStartTime;
    
    // Variables for saccade detection from file
    private static int csv_counter = 0;
    private ReadCSVFile reader;
    private Tuple<List<float>, List<float>, List<float>> csv_data;
    private List<float> values_columnA, values_columnB, values_columnC;
    private bool endOfFile;

    // Variables for online saccade detection
    private EyeTracking tracker;

    void Start()
    {
        // Start eye tracker
        tracker = EyeTracking.Tracker;

        saccadeDetected = false;
        ongoingSaccade = false;
        
        if (detectFromFile)
        {
            reader = new ReadCSVFile();
            if(filePath.Equals(""))
            {
                csv_data = reader.ReadCSVFileToColumnsABC(Application.dataPath, defaultFilePath);
            }
            else
            {
                csv_data = reader.ReadCSVFileToColumnsABC(Application.dataPath, filePath);
            }
            
            values_columnA = csv_data.Item1;
            values_columnB = csv_data.Item2;
            values_columnC = csv_data.Item3;
            csv_counter = 0;
            endOfFile = false;

            currentPos = new Vector3(values_columnA[csv_counter], values_columnB[csv_counter], values_columnC[csv_counter]);
        }

        if(detectOnline)
        {
            currentPos = tracker.gazeInfo.combinedGazeForward;
        }

        if(detectFromFile || detectOnline)
        {
            mdnSDPositions = new Vector3[1];
            mdnSDPositions[0] = currentPos;
        }

        if(saccadeLogging)
        {
            // Initialize logger for saccade logging
            Logging.Logger.RecordSaccades(sac_ColumnNames, StudyController.participantID);
            print(sac_ColumnNames);
            LogSaccade(currentPos, new Vector3(), new Vector3(), new Vector3(), new Vector3(), new Vector3(), new Vector3(), saccadeDetected); // first row is empty
        }

        if(!eyeTrackingLogging)
        {
            tracker.loggingOn = false; // Turn ET Logging off
        }
    }

    // Update is called once per frame
    void Update()
    {        
        // Request gaze calibration
        if (Input.GetKeyDown(calibrationRequestKey))
        {
            tracker.Calibrate();
        }

        // Vector3 currentPos = gazeTarget.transform.position;

        if(detectFromFile || detectOnline)
        {
            
            if(detectOnline)
            {
                currentPos = tracker.gazeInfo.combinedGazeForward;
                runDetection();
            }
            else if(detectFromFile)
            {
                if(csv_counter == values_columnA.Count)
                {
                    endOfFile = true;
                }
                
                if (!endOfFile)
                {
                    currentPos = new Vector3(values_columnA[csv_counter], values_columnB[csv_counter], values_columnC[csv_counter]);
                    runDetection();
                }
                else
                {
                    Debug.Log("End of file: stopping detection");
                }
            }            
        }        
    }

    void runDetection()
    {
        // Fill array for median calculations
        if(mdnSDPositions.Length < bufferSize * 200) // assuming 1 second = 200 Hz
        {
        // If array is smaller than buffer size just add the value, if not add new value and discard oldest one
           mdnSDPositions = expand(mdnSDPositions, currentPos);
        }
        else
        {
            mdnSDPositions = addNewValue(mdnSDPositions, currentPos);
        }

        // Calculate horizontal and vertical velocities with 3-point moving window
        Vector3[] velocities = filterVector(mdnSDPositions, filterS);
        Vector3 currentVelocity =velocities[velocities.Length-1];

        // Calculate median-based standard deviation:
        // 1. Get power of each velocity value Vxy_1^2, Vxy_2^2, ..., Vxy_n^2
        Vector3[] pows = power(velocities, 2);
            
        // 2. Get median of velocity power values: median(Vxy_1^2, Vxy_2^2, ..., Vxy_n^2)
        Vector3 medianOfPows = getMedianOfPows(pows);

        // 3. Get power of median of velocity vector: power(median(Vxy_1, Vxy_2, ..., Vxy_n))
        Vector3 powOfMedians = powOfMedian(velocities);

        // 4. Get sqrt of the median of velocity powers minus the power of the velocity median:
        // sqrt( median(Vxy_1^2, Vxy_2^2, ..., Vxy_n^2) - power(median(Vxy_1, Vxy_2, ..., Vxy_n)) )
        Vector3 sqrtOfMedians = sqrtOfMedian(medianOfPows, powOfMedians);

        // Multiply with lambda to get threshold
        Vector3 radius_Threshold = sqrtOfMedians * lambda;

        // Detect saccade at current pos
        bool saccadeDetectionResult = (System.Math.Pow(currentVelocity.x/radius_Threshold.x, 2) + System.Math.Pow(currentVelocity.y/radius_Threshold.y, 2)) > 1;
                
        DateTime currentTime = System.DateTime.UtcNow;
        if(ongoingSaccade) // if there is an ongoing saccade, we ignore detection result and wait for time buffer
        {
            if((currentTime - saccadeStartTime).Milliseconds < saccadeDurationThreshold)
            {
                ongoingSaccade = true;
                saccadeDetected = false;
            }
            else
            {
                ongoingSaccade = false;
            }
        }
        else if(saccadeDetectionResult) // new saccade is detected
        {
            saccadeDetected = true;
            saccadeStartTime = currentTime;
            ongoingSaccade = true;
        }
        else // no saccade detected
        {
            saccadeDetected = false;
            ongoingSaccade = false;
        }

        if(saccadeLogging)
        {
            LogSaccade(currentPos, velocities[velocities.Length-1], pows[pows.Length-1], medianOfPows, powOfMedians, sqrtOfMedians, radius_Threshold, saccadeDetected);
        }

        saccadeDetected = false;            

        if(detectFromFile)
        {
            csv_counter++;
        }
    }

    private void LogSaccade(Vector3 currentPos, Vector3 velocities, Vector3 powers, Vector3 medianOfPows, Vector3 powOfMedians, Vector3 median_sds, Vector3 radius_Threshold, bool sacc)
    {
        // Get current frame number

        string[] logData = new string[16];

        logData[0] = Time.frameCount.ToString();
        logData[1] = currentPos.x.ToString();
        logData[2] = currentPos.y.ToString();
        logData[3] = velocities.x.ToString();
        logData[4] = velocities.y.ToString();
        logData[5] = powers.x.ToString();
        logData[6] = powers.y.ToString();
        logData[7] = medianOfPows.x.ToString();
        logData[8] = medianOfPows.y.ToString();
        logData[9] = powOfMedians.x.ToString();
        logData[10] = powOfMedians.y.ToString();
        logData[11] = median_sds.x.ToString();
        logData[12] = median_sds.y.ToString();
        logData[13] = radius_Threshold.x.ToString();
        logData[14] = radius_Threshold.y.ToString();
        logData[15] = sacc.ToString();

        Logging.Logger.RecordSaccades(logData, StudyController.participantID);
    }

    private float filter(float[] vals, float filter)
    {
        // vals[0]: -filter, val[1]: 0, val[2]: filter
        return filter*(-1.0f)*vals[0] + filter*vals[2];
    }

    private Vector3[] filterVector(Vector3[] vals, float filter)
    {
        if(vals.Length >= 3)
        {
            Vector3[] outVals = new Vector3[vals.Length];
            for (int i = 1; i < vals.Length-1; i++)
            {
                outVals[i].x = filter*(-1.0f)*vals[i-1].x + filter*vals[i+1].x;
                outVals[i].y = filter*(-1.0f)*vals[i-1].y + filter*vals[i+1].y;
                outVals[i].z = filter*(-1.0f)*vals[i-1].z + filter*vals[i+1].z;
            }
            // get rid of NAs
            outVals[0].x = outVals[1].x;
            outVals[0].y = outVals[1].y;
            outVals[0].z = outVals[1].z;
            outVals[outVals.Length-1].x = outVals[outVals.Length-2].x;
            outVals[outVals.Length-1].y = outVals[outVals.Length-2].y;
            outVals[outVals.Length-1].z = outVals[outVals.Length-2].z;

            return outVals;
        }
        else
        {
            Debug.LogWarning("Vector too small for filter, returning incoming vector");
            return vals;
        }
        
    }

    private Vector3[] power(Vector3[] vals, int powVal)
    {
        Vector3[] outVals = new Vector3[vals.Length];
        for(int i=0; i<vals.Length; i++)
        {
            outVals[i].x = (float) System.Math.Pow(vals[i].x, powVal);
            outVals[i].y = (float) System.Math.Pow(vals[i].y, powVal);
            outVals[i].z = (float) System.Math.Pow(vals[i].z, powVal);
        }

        return outVals;
    }

    private Vector3 sqrtOfMedian(Vector3 mdnOfPows, Vector3 powOfMedians)
    {
        Vector3 outVal = new Vector3();
        outVal.x = (float) System.Math.Sqrt(mdnOfPows.x - powOfMedians.x);
        outVal.y = (float) System.Math.Sqrt(mdnOfPows.y - powOfMedians.y);
        outVal.z = (float) System.Math.Sqrt(mdnOfPows.z - powOfMedians.z);

        return outVal;
    }

    private Vector3 powOfMedian(Vector3[] vals)
    {
        Vector3 outVal = new Vector3();

        // temp arrays to store each coordinate in an array
        float[] temp_x = new float[vals.Length];
        float[] temp_y = new float[vals.Length];
        float[] temp_z = new float[vals.Length];
        for (int i = 0; i < vals.Length; i++)
        {
            temp_x[i] = vals[i].x;
            temp_y[i] = vals[i].y;
            temp_z[i] = vals[i].z;
        }

        float mdn_x = getMedian(temp_x);
        float mdn_y = getMedian(temp_y);
        float mdn_z = getMedian(temp_z);
        outVal.x = (float) System.Math.Pow(mdn_x, 2);
        outVal.y = (float) System.Math.Pow(mdn_y, 2);
        outVal.z = (float) System.Math.Pow(mdn_z, 2);

        return outVal;   
    }

    private float[] addNewValue(float[] vals, float newVal)
    {
        float[] outVals = new float[vals.Length];
        for(int i = 0; i < outVals.Length-1; i++)
        {
            outVals[i] = vals[i+1];
        }
        outVals[outVals.Length-1] = newVal;

        return outVals;
    }

    private Vector3[] addNewValue(Vector3[] vals, Vector3 newVal)
    {
        Vector3[] outVals = new Vector3[vals.Length];
        for(int i=0; i < vals.Length -1; i++)
        {
            outVals[i] = vals[i+1];
        }
        outVals[outVals.Length-1] = newVal;

        return outVals;
    }

    private Vector3[] expand(Vector3[] vals, Vector3 newVal)
    {
        Vector3[] outVals = new Vector3[vals.Length+1];
        for(int i=0; i < vals.Length; i++)
        {
            outVals[i] = vals[i];
        }
        outVals[outVals.Length-1] = newVal;

        return outVals;
    }

    private Vector3 getMedianOfPows(Vector3[] vals)
    {
        Vector3 outVal = new Vector3();

        // temp arrays to store each coordinate in an array
        float[] temp_x = new float[vals.Length];
        float[] temp_y = new float[vals.Length];
        float[] temp_z = new float[vals.Length];
        for (int i = 0; i < vals.Length; i++)
        {
            temp_x[i] = vals[i].x;
            temp_y[i] = vals[i].y;
            temp_z[i] = vals[i].z;
        }

        outVal.x = (float) getMedian(temp_x);
        outVal.y = (float) getMedian(temp_y);
        outVal.z = (float) getMedian(temp_z);

        return outVal;   
    }

    private float getMedian(float[] vals)
    {
        float[] sortedArray = (float[]) vals.Clone();
        Array.Sort(sortedArray);

        int size = sortedArray.Length;

        int mid = size/2;

        float median = (size % 2 != 0) ? (float) sortedArray[mid] : ((float)sortedArray[mid] + (float)sortedArray[mid-1])/2;

        return median;
    }

    public void setParticipantID(int id)
    {
        participantID = id;
    }
}
