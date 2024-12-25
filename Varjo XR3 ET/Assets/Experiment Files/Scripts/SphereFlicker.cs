using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
public class SphereFlicker : MonoBehaviour
{
    // keys for changing modulation intensity
    public KeyCode increaseIntensity = KeyCode.UpArrow;
    public KeyCode decreaseIntensity = KeyCode.DownArrow;

    [Header("Show demonstration spheres.")]
    public bool showDemonstrationSpheres = false;

    [Header("This is the sphere demonstrating the main color.")]
    public static GameObject sphereMainColor;

    [Header("This is the sphere demonstrating the high modulation color.")]
    public static GameObject sphereModulationHigh;

    [Header("This is the sphere demonstrating the low modulation color.")]
    public static GameObject sphereModulationLow;

    [Header("This is the main color.")]
    public float[] mainColorHSV = new float[3];

    [Header("What should the flicker frequency be in Hz?")]
    public float flickerFrequency = 20.0f; // 20 Hz from Bailey et al. paper

    [Header("Determine which component to modulate.")]
    public bool changingH = false;
    public bool changingS = false;
    public bool changingV = false;
    private bool luminance = true;
    private float[] mainColorHSL = new float[3];

    public static float flickerIntensity = 0f; // initial flicker intensity
        
    private int colorState = 0;
    private Color mainColor;
    private Color modulationHighColor;
    private Color modulationLowColor;
    private MeshRenderer mr;
    private bool modulationChange = false;
    private bool flickerOn = false;
    private List<FLICKERADJUSTMENT> flickerAdjustments = new List<FLICKERADJUSTMENT>();

    private bool firstRoundAdjustment = true;
    private int firstFlickerAdjustmentStreak = 4;
    private int secondFlickerAdjustmentStreak = 4;
    private int reversalCounter = 0;
    private float firstFlickerAdjustment = 0.005f;
    public static float secondFlickerAdjustment = 0.001f;
    public static bool calibrationDone = false;

   

    private enum FLICKERADJUSTMENT
    {
        UPWARDS,
        DOWNWARDS
    }

    public string ObjectName;
    

    private DateTime startModulationTime;


    void Awake()
    {
        mr = GetComponent<MeshRenderer>();
        sphereMainColor = GameObject.Find("SphereMainColor");
        sphereModulationHigh = GameObject.Find("SphereModulationHigh");
        sphereModulationLow = GameObject.Find("SphereModulationLow");
    }
    
    void Start()
    {
        Log("Start", "Start");
    }
    private static readonly string[] ColumnNamesFlickering = { "U_Frame", "Quadrant", "UpOrDownArrow", "CurrentFlickerValue", "CalibrationRound", "Reversal" };
    void Log(string arrowDirection, string reversal)
    {
        string[] msg = new string[ColumnNamesFlickering.Length];
        msg[0] = Time.frameCount.ToString();
        msg[1] = TargetControllerPilot.currentQuadrant().ToString();

        msg[2] = arrowDirection;

        msg[3] = flickerIntensity.ToString();
        msg[4] = TargetControllerPilot.currentRound().ToString();
        msg[5] = reversal;



        // logger.Record("ObjectHits",msg);
        Logging.Logger.RecordFlickeringCalibration(msg, StudyController.participantID);

    }

    void Update()
    {
        // flickering calibration
        
        if (!StudyController.flickeringCalibrationDone) { 
            string arrowDirection = "";
            string reversal = "No";
            // Change flicker intensity
            if (Input.GetKeyDown(increaseIntensity))
            {
                arrowDirection = "Up";
                flickerAdjustments.Add(FLICKERADJUSTMENT.UPWARDS);
                flickerIntensity += firstRoundAdjustment ? firstFlickerAdjustment : secondFlickerAdjustment;
                modulationChange = true;
            }
            else if(Input.GetKeyDown(decreaseIntensity))
            {
                arrowDirection = "Down";
                flickerAdjustments.Add(FLICKERADJUSTMENT.DOWNWARDS);
                if (flickerIntensity <= secondFlickerAdjustment) return;
                flickerIntensity -= firstRoundAdjustment ? firstFlickerAdjustment : secondFlickerAdjustment;
                modulationChange = true;
            }

            if (flickerAdjustments.Count == 2 )
            {
                if (flickerAdjustments[0] != flickerAdjustments[1]) { //Reversal
                    reversalCounter++;
                    reversal = "Yes";
                    if (reversalCounter == firstFlickerAdjustmentStreak && firstRoundAdjustment)
                    {
                        firstRoundAdjustment = false;
                        reversalCounter = 0;
                        flickerAdjustments = new List<FLICKERADJUSTMENT>();
                    }
                    else if (reversalCounter == secondFlickerAdjustmentStreak && !firstRoundAdjustment)
                    {
                        calibrationDone = true;
                    }            
                }
                if (flickerAdjustments.Count != 0)
                    flickerAdjustments.RemoveAt(0);
            }

            if (arrowDirection != "")
                Log(arrowDirection, reversal);

        }
         


        // Update flicker properties if modulationchange is true
        if (modulationChange)
        {
            
            if(luminance)
            {
                float[] highC_HSV = HSLToHSV_Mod(mainColorHSL, flickerIntensity, "high");
                float[] lowC_HSV = HSLToHSV_Mod(mainColorHSL, flickerIntensity, "low");
                modulationHighColor = Color.HSVToRGB(highC_HSV[0], highC_HSV[1], highC_HSV[2]);
                modulationLowColor = Color.HSVToRGB(lowC_HSV[0], lowC_HSV[1], lowC_HSV[2]);
            }

            // Show one sphere of each color for debugging purposes
            if(showDemonstrationSpheres)
            {
                sphereModulationHigh.GetComponent<MeshRenderer>().material.color = modulationHighColor;
                sphereModulationLow.GetComponent<MeshRenderer>().material.color = modulationLowColor;
            }
        }
        
        modulationChange = false;

        if(flickerOn)
        {
            // Set timing for flicker
            DateTime currentTime = System.DateTime.UtcNow;

            if(((currentTime - startModulationTime).Seconds * 1000 + (currentTime - startModulationTime).Milliseconds) > (1000/flickerFrequency))
            {
                flicker();
                startModulationTime = System.DateTime.UtcNow;
            }
        }
    }

    private void flicker()
    {
       switch(colorState)
        {
            case 0: // original color
                mr.material.color = modulationHighColor;
                colorState = 1;
                break;
            case 1: // high modulation
                mr.material.color = mainColor;
                colorState = 2;                    
                break;
            case 2: // original color
                mr.material.color = modulationLowColor;
                colorState = 3;
                break;
            case 3: // low modulation
                mr.material.color = mainColor;
                colorState = 0;
                break;
        }
    }

    private void resetIntensity()
    {
        flickerIntensity = 0f;
        modulationChange = true;
    }

    public void setFlickerProperties(CONDITION condition, float intensity, Color color)
    {
        mainColor = color;
        mr.material.color = mainColor;
        float H_v, H_l, S_v, S_l, L, V;

        Color.RGBToHSV(mainColor, out H_v, out S_v, out V);
        HSVToHSL(H_v, S_v, V, out H_l, out S_l, out L);
        mainColorHSL[0] = H_l;
        mainColorHSL[1] = S_l;
        mainColorHSL[2] = L;
        


        if(!showDemonstrationSpheres)
        {
            if(sphereMainColor is not null)
            {
                sphereMainColor.SetActive(false);
            }
            if(sphereModulationHigh is not null)
            {
                sphereModulationHigh.SetActive(false);
            }
            if(sphereModulationLow is not null)
            {
                sphereModulationLow.SetActive(false);
            }   
        }
        else
        {

            sphereMainColor.GetComponent<MeshRenderer>().material.color = mainColor;

            sphereModulationHigh.GetComponent<MeshRenderer>().material.color = mainColor;

            sphereModulationLow.GetComponent<MeshRenderer>().material.color = mainColor;
        }


        switch (condition)
        {
            case CONDITION.RAND_COMPONENT: case CONDITION.DIST_COMPONENT: case CONDITION.CLOSE_COMPONENT:
                
                luminance = true;
                flickerIntensity = intensity;
                modulationChange = true;
                break;
            case CONDITION.MANUAL:
                changingH = true;
                changingS = false;
                changingV = false;
                resetIntensity();
                break;
        }
    }

    public void startFlicker()
    {
        flickerOn = true;
    }

    public void stopFlicker()
    {
        flickerOn = false;
    }

    private void HSLToHSV(float h, float s, float l, out float H_v, out float S_v, out float V_v)
    {
        H_v = h;
        V_v = l + s * Mathf.Min(l, 1-l);
        if(V_v == 0)
        {
            S_v = 0;
        }
        else
        {
            S_v = 2 * (1-l/V_v);
        }
    }

    private void HSVToHSL(float h, float s, float v, out float H_l, out float S_l, out float L)
    {
        H_l = h;
        L = v * (1 - s/2);
        if(L==0 || L==1)
        {
            S_l = 0;
        }
        else
        {
            S_l = (v-L)/Mathf.Min(L, 1-L);
        }
    }

    private float[] HSLToHSV_Mod(float[] hsl, float intensity, string direction)
    {
        float[] hsv = new float[3];
        float H, S, V;
    //    Debug.Log("IN h: " + hsl[0] + " s: " + hsl[1] + " l: " + hsl[2]);
        if(direction.Equals("high"))
        {
            if((hsl[2] + intensity) > 1)
            {
                HSLToHSV(hsl[0], hsl[1], hsl[2], out H, out S, out V); 
            }
            else
            {
                HSLToHSV(hsl[0], hsl[1], hsl[2] + intensity, out H, out S, out V);
//                Debug.Log("OUT h: " + hsl[0] + " s: " + hsl[1] + " l: " + (hsl[2] + intensity));
            }
        }
        else if(direction.Equals("low"))
        {
            if((hsl[2] - intensity) < 0)
            {
                HSLToHSV(hsl[0], hsl[1], hsl[2], out H, out S, out V); 
            }
            else
            {
                HSLToHSV(hsl[0], hsl[1], hsl[2] - intensity, out H, out S, out V);
  //              Debug.Log("OUT h: " + hsl[0] + " s: " + hsl[1] + " l: " + (hsl[2] - intensity));
            }
        }
        else
        {
            HSLToHSV(hsl[0], hsl[1], hsl[2], out H, out S, out V);
        }
        hsv[0] = H;
        hsv[1] = S;
        hsv[2] = V;
        return hsv;
    }
}

*/