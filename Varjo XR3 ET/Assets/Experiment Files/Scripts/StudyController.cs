using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.VisualScripting;
using UnityEditor.Rendering;
using System.Linq;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SearchService;

public class StudyController : MonoBehaviour
{ 
    
    
    // Store participant info
    
    public static int participantID;
    [Header("_________________________________________________________________________________________________________")]
    [Header("Set participant ID.")]
    public int participantIDInput;


    [Header("_________________________________________________________________________________________________________")]
    [Header("Set number of trials.")]
    public int numberOfTrials = 2;


    [Header("Set number of conditions.")]
    public CONDITION[] conditionTypes = new CONDITION[2];  // Lise: conditions are central fixation and free-looking.

    [Header("Define condition order.")]
    public CONDITION[] conditions = new CONDITION[2];    // "conditions" are the conditions in order. Right now, they are identical to conditionTypes.
    


    [Header("Enable study logging.")]
    public bool logging = false;

    
    /*
    [Header("Use txt file for vectors")]
    public bool useTxtFile = false;

    [Header("Set the path to the txt file")]
    public string PathToFolder = "Assets/Experiment Files/TargetPositionFiles/";

    [Header("Save vectors to txt file")]
    public bool saveVectors = true;
    */
    

    

    [Header("Perform eye tracking validation")]
    public bool validationScene;

    public EyeTrackingValidation eyeTrackingValidation;

    public ArcCreater ArcCreater;
    private bool useArcCreater = false;

    private List<List<Vector3>> targetScenes = new List<List<Vector3>>();
    
    

    private KeyCode inputKey = KeyCode.Return;
    private KeyCode continueKey = KeyCode.RightArrow;
    private bool inputEvent;

    
    private SaccadeDetection saccadeDetection;
    private EyeTracking eyeTracking;

    private int state;

    public GameObject fixationCross;

    public Grid_object_manager grid_object_manager; // Reference to ObjectManager for the picture grid
    public bool grid_loaded = false;
    public bool grid_isComplete = false;

    
    
    
    public bool condition_loaded = false;

    private int currentTrialNumb; // indicates the current trial number
    private int currentCondition; // indicates current condition number

    private AudioManager audio_manager;

    private bool recall_isComplete = false;






    private bool newTrial;

    private bool studyDone;
    private bool postConditionLoaded;

    //private bool targetInstructionsLoaded;
    private bool continueB, practiceCompleted;
    private bool practiceSelectionCompleted;
    private float timeRemaining = 5;
    private float timeRemainingTrial = 5;
    //public bool SET_FLICKERING_HIGH = false;

    public float timePerTrial = 10;
    private float questionnaireBufferTime = 1;

    

    //private string vectorPositionsFileName = $"Assets/ETHighlight_Components/Targets/TargetPositions_";

    

    private enum SCENES
    {
        PICTUREGRID_1,
        PICTUREGRID_2,
        FREELOOKING,
        CENTRALFIXATION,
        PRESTUDYINSTRUCTIONS,
        ENCODINGINSTRUCTIONS,
        RECALLINSTRUCTIONS,
        TARGETINSTRUCTION,      // old
        PRACTICE,               // old
        ETCALIBRATION,          // old ?
        LOADINGCONDITION,       // old ?
        CONDITION1,             // old
        NEWTRIAL,               // old
        POSTCONDITION,          // old ?
        POSTSTUDY,
        QUESTIONNAIRE,          // old ?
        FLICKERINGCALIBRATION,  // old
        ETVALIDATION
    }

    public enum CONDITION
    {
        CentralFixation,
        FreeLooking
    }

    // logs to study_log_participantX
    //private static readonly string[] ColumnNamesStudy = { "U_Frame", "LogTime", "TrialNumber", "SceneIndex", "TargetDistractorColorShape","Condition", "FlickerIntensity", "ValidationTrackerAbsolute", "ValidationTrackerProcent"};  // Lise: Change this!
    private static readonly string[] ColumnNamesStudy = { "U_Frame", "LogTime", "Encoding_phase", "Recall_phase", "ConditionNumber", "Condition_name", "Category", "Statement_ID", "Correctness", "Answer", "ValidationTrackerAbsolute", "ValidationTrackerProcent" };


    private void Awake()
    {
        participantID = participantIDInput;
    }


    // Start is called before the first frame update
    void Start()
    {
        
        // conditions = new CONDITION[numberOfTrials];     // Lise wrote "2"
        //targetSceneIndexOrder = new int[numberOfTrials];
        int trialsPerCondition = numberOfTrials / conditions.Length;
        for (int i = 0; i < numberOfTrials; i++)
        {
            // Calculate condition index and trial index within that condition
            int conditionIndex = i / trialsPerCondition;
            int trialIndexWithinCondition = i % trialsPerCondition;
            
            // Assign condition
            conditions[i] = conditionTypes[conditionIndex];
            
            
        }

        
        saccadeDetection = SaccadeDetection.Detector;
        eyeTracking = EyeTracking.Tracker;

        // saccadeDetection.setParticipantID(participantID);
        // eyeTracking.setParticipantID(participantID);
        

        inputEvent = false;
        newTrial = false;
        studyDone = false;
        postConditionLoaded = false;
        //targetInstructionsLoaded = false;
        continueB = false; 
        
        state = 0;
        currentTrialNumb = 0;
        currentCondition = 0;
        //shuffle(targetOrder);
        int seed = Random.Range(0, 1000);
        shuffle(conditions, true, seed);   // Lise: use this for radomizing order of statements in recalling phase?



        // Start Scene with instructions
        if (validationScene)
        {
            loadScene(SCENES.ETVALIDATION);
            
            //if (eyeTrackingValidation == null && validationScene)
            //    eyeTrackingValidation = GameObject.Find("ETValScriptManager").GetComponent<EyeTrackingValidation>();
            
        }
        else
        {
            loadScene(SCENES.PRESTUDYINSTRUCTIONS);
            
        }


        if(logging)
        {
            // enable logging for modulation
            Logging.Logger.RecordStudyInfo(ColumnNamesStudy, participantID);
            
        }
    }

    /*void ChangeTargetColors() {
        if (timeRemainingTrial != 5) // Trial is just starting and it will only change color once
            return;
        int rnd = Random.Range(0,2);
        if (rnd == 0){
            Color tempColor = targetColor1;
            targetColor1 = targetColor2;
            targetColor2 = tempColor;
        }
        GameObject.Find("Target_Instructions1").GetComponent<MeshRenderer>().material.color = targetColor1;
        GameObject.Find("Target_Instructions2").GetComponent<MeshRenderer>().material.color = targetColor2;
        GameObject.Find("Distractor_Instructions1").GetComponent<MeshRenderer>().material.color = targetColor2;
        GameObject.Find("Distractor_Instructions2").GetComponent<MeshRenderer>().material.color = targetColor1;

    } */

    //{ "U_Frame", "Unix Time",  "TrialNumber", "SceneIndex", "TargetDistractorColorShape","Condition", "FlickerIntensity"};

    void Log(){

        /* 
        ColumnNamesStudy = { 
            "U_Frame",                          // 0
            "LogTime",                          // 1
            "Encoding_phase"                    // 2
            "Recall_phase"                      // 3
            "ConditionNumber",                  // 4
            "Condition_name",                   // 5
            "Category",                         // 6
            "Statement_ID",                     // 7
            "Correctness",                      // 8
            "Answer",                           // 9
            "ValidationTrackerAbsolute",        // 10
            "ValidationTrackerProcent"          // 11
            };
        */

        if (currentTrialNumb == numberOfTrials)
            return;
        string[] msg = new string[ColumnNamesStudy.Length];
        msg[0] = Time.frameCount.ToString();
        msg[1] = (System.DateTime.Now.Ticks / System.TimeSpan.TicksPerMillisecond).ToString();

        msg[2] = grid_loaded.ToString();
        msg[3] = condition_loaded.ToString(); 

        msg[4] = (currentCondition).ToString();

        msg[5] = conditions[currentCondition].ToString();


        // get from AudioManager - if recall isnt active, it should be "None"
        if (audio_manager != null)
        {
            string[] recall_data = audio_manager.logThis;
            Debug.Log("Length of recall_data: " + recall_data.Length);
            msg[6] = recall_data[0]; // category
            msg[7] = recall_data[1]; // statement ID
            msg[8] = recall_data[2]; // correctness
            msg[9] = recall_data[3]; // answer
        }
        else
        {
            msg[6] = "None"; // category
            msg[7] = "None"; // statement ID
            msg[8] = "None"; // correctness
            msg[9] = "None"; // answer
        }
        


        //msg[3] = targetSceneIndexOrder[currentTrialNumb].ToString();
        // msg[4] = $"sphere_{targetColor1}_rectangle_{targetColor2}"; 

        //msg[5] = conditions[currentTrialNumb].ToString();
        //msg[6] = SphereFlicker.flickerIntensity.ToString();

        msg[10] = eyeTrackingValidation ? (eyeTrackingValidation.totalAverage).ToString() : "None";
        msg[11] = eyeTrackingValidation ? (1 - ((eyeTrackingValidation.totalAverage) / eyeTrackingValidation.distanceThreshold)).ToString() : "None";


        // logger.Record("ObjectHits",msg);
        Logging.Logger.RecordStudyInfo(msg,participantID);
        
    }


    public void LoadPictureGridScene(string scene_name)
    {
        // Load the scene additively
        SceneManager.LoadSceneAsync(scene_name, LoadSceneMode.Additive).completed += (operation) =>
        {
            // Get the root objects in the loaded scene
            UnityEngine.SceneManagement.Scene pictureGridScene = SceneManager.GetSceneByName(scene_name);
            GameObject[] rootObjects = pictureGridScene.GetRootGameObjects();

            // Loop through the root objects to find Grid_object_manager
            foreach (GameObject obj in rootObjects)
            {
                Grid_object_manager manager = obj.GetComponentInChildren<Grid_object_manager>();
                if (manager != null)
                {
                    // Do something with the manager, for example, call a method
                    grid_isComplete = manager.GetIsComplete();
                    grid_object_manager = manager;
                    break;
                }
            }
        };
    }

    // finds the correct audio_manager object
    public void LoadConditionScene(string scene_name)
    {
        // Load the scene additively
        SceneManager.LoadSceneAsync(scene_name, LoadSceneMode.Additive).completed += (operation) =>
        {
            // Get the root objects in the loaded scene
            UnityEngine.SceneManagement.Scene conditionScene = SceneManager.GetSceneByName(scene_name);
            GameObject[] rootObjects = conditionScene.GetRootGameObjects();

            // Loop through the root objects to find Grid_object_manager
            foreach (GameObject obj in rootObjects)
            {
                AudioManager manager = obj.GetComponentInChildren<AudioManager>();
                if (manager != null)
                {
                    // Do something with the manager, for example, call a method
                    recall_isComplete = manager.GetIsComplete();
                    audio_manager = manager;
                    audio_manager.ResetIsComplete();
                    break;
                }
            }
        };
    }



    // Update is called once per frame
    void Update()
    {
        /*
        if (SET_FLICKERING_HIGH)
        {
            SphereFlicker.flickerIntensity = 0.5f;
            SET_FLICKERING_HIGH = false;
        }
        */
        
        // Record the transform of the positions of each child from the scene gameobject
        /*if (Input.GetKeyDown(KeyCode.R))
        {
            
            string data = "";
            foreach (Transform child in sceneTargets.transform)
            {
                data += child.localPosition.x + " " + child.localPosition.y + " " + child.localPosition.z + "\n";
            }
            FileStream fs = new FileStream(vectorPositionsFileName + "_" + targetSceneIndexOrder[currentTrialNumb - 1].ToString() + ".txt", FileMode.Create);
            StreamWriter writer = new StreamWriter(fs);
            writer.WriteLine(data);
            writer.Close();
            //FileCounter++;
        }*/

        if (eyeTrackingValidation == null && validationScene ) //&& !flickeringCalibration)
            eyeTrackingValidation = GameObject.Find("ETValScriptManager")?.GetComponent<EyeTrackingValidation>();

        if (Input.GetKeyUp(inputKey))
        {
            
           
            if (!validationScene || (eyeTrackingValidation != null ? eyeTrackingValidation.finishedValidation : true))  // Lise: added a parentheses around the second statement af ||
                inputEvent = true;
                
        }

        // test
        //Debug.Log("Grid manager exists (above switch cases): " + (grid_object_manager != null).ToString());  // false apparently
        //Debug.Log("Flag: " + grid_object_manager.isComplete);  // this makes it non-reactive to enter...
        
        // ensures that we move on to state 2 - will not move on from grid without this
        if ((grid_object_manager != null) && grid_object_manager.isComplete && state==1)
        {
            Debug.Log("Picture grid is complete, above switch case");
            state = 2;
        }

         
        if (logging)
        {
            Log();
        }

        // if(Input.GetKeyUp(continueKey))
        // {
        //     continueB = true;
        // }
        continueB = Input.GetKeyUp(continueKey);

        switch (state)
        {
            case 0: // pre study instructions scene loaded
                // Validation has already run in Start(). 
                if (inputEvent)
                {


                    if (validationScene) {
                        unloadScene(SCENES.ETVALIDATION);
                        loadScene(SCENES.PRESTUDYINSTRUCTIONS);
                        validationScene = false;  // this will make it go to "else" below in next Update iteration
                    }
                    else {
                        unloadScene(SCENES.PRESTUDYINSTRUCTIONS);
                        loadScene(SCENES.ENCODINGINSTRUCTIONS);

                        state = 1;
                    }
                }

                break;
            case 1: // picture grid, move to state 2 when grid is complete

                if (!grid_loaded && inputEvent)
                {
                    unloadScene(SCENES.ENCODINGINSTRUCTIONS);
                    if (conditions[currentCondition] == CONDITION.CentralFixation) 
                    {
                        LoadPictureGridScene("Picture_grid");
                    }
                    else if (conditions[currentCondition] == CONDITION.FreeLooking) 
                    {
                        LoadPictureGridScene("Picture_grid_order2");
                    }

                    
                    grid_loaded = true;
                }
                
                if (grid_loaded)
                {
                    if (grid_object_manager.isComplete)
                    {
                        Debug.Log("Picture grid is complete, in state 1");  // does NOT reach this
                        state = 2;
                    }

                }
                

                
                break;

            case 2: // recall instructions

                if (grid_loaded)
                {
                    if (conditions[currentCondition] == CONDITION.CentralFixation) { unloadScene(SCENES.PICTUREGRID_1); }
                    else if (conditions[currentCondition] == CONDITION.FreeLooking) { unloadScene(SCENES.PICTUREGRID_2); }

                    //unloadScene(SCENES.PICTUREGRID);
                    loadScene(SCENES.RECALLINSTRUCTIONS);
                    grid_loaded = false;
                }
                

                if (inputEvent)
                {
                    state = 3;
                }

                break;
            case 3: // recall phase
                //Debug.Log("Current condition" + conditions[currentCondition]);
                if ((conditions[currentCondition] == CONDITION.CentralFixation) && !condition_loaded)  // change to currentTrialNumb ?
                {
                    Debug.Log("Current condition: " + conditions[currentCondition]);
                    

                    unloadScene(SCENES.RECALLINSTRUCTIONS);
                    //loadScene(SCENES.CENTRALFIXATION);
                    LoadConditionScene("Recall_central_fixation");  // also updates audio_manager
                    condition_loaded = true;

                }
                else if ((conditions[currentCondition] == CONDITION.FreeLooking) && !condition_loaded)
                {
                    Debug.Log("Current condition: " + conditions[currentCondition]);
                    

                    unloadScene(SCENES.RECALLINSTRUCTIONS);
                    //loadScene(SCENES.FREELOOKING);
                    LoadConditionScene("Recall_free_looking");  // also updates audio_manager
                    condition_loaded = true;

                    // put function that plays statements with 8 seconds intervals here

                }
                
                
                if (condition_loaded && audio_manager.isComplete)
                {
                    Debug.Log("Condition " + currentCondition + " is complete, in state 3");
                    // unload the current condition and load instructions for next grid
                    if (conditions[currentCondition] == CONDITION.CentralFixation) { unloadScene(SCENES.CENTRALFIXATION); }
                    else if (conditions[currentCondition] == CONDITION.FreeLooking) { unloadScene(SCENES.FREELOOKING); }
                    loadScene(SCENES.ENCODINGINSTRUCTIONS);
                    condition_loaded = false;

                    audio_manager.ResetIsComplete();
                    Destroy(audio_manager); 


                    Debug.Log("Current condition number: " + currentCondition);
                    currentCondition++; 
                    currentTrialNumb++;
                    if (currentCondition == conditions.Length)
                    {
                        // All conditions have been completed
                        Debug.Log("All conditions completed. Current condition number: " + currentCondition);  // should be 2

                        state = 4;
                        break;
                    }
                    else
                    {
                        state = 1;  // back to encoding phase, as one condition is completed
                        
                        
                        grid_object_manager.ResetIsComplete();

                    }
                    

                }
                
                break; 

            case 4:  // post study instruction
                if(!studyDone)
                {

                    // unload the current condition
                    /*
                    if (conditions[1] == CONDITION.CentralFixation) 
                    { unloadScene(SCENES.CENTRALFIXATION); }
                    else if (conditions[1] == CONDITION.FreeLooking) 
                    { unloadScene(SCENES.FREELOOKING); } */
                    
                    unloadScene(SCENES.ENCODINGINSTRUCTIONS);

                    loadScene(SCENES.POSTSTUDY);
                    studyDone = true;
                }
                break;
        }

        inputEvent = false;    
    }

    private void loadScene(SCENES scenes, int trialNumb = 0)
    {
        switch (scenes)
        {
            case SCENES.PICTUREGRID_1:
                SceneManager.LoadScene("Picture_grid", LoadSceneMode.Additive);
                break;
            case SCENES.PICTUREGRID_2:
                SceneManager.LoadScene("Picture_grid_order2", LoadSceneMode.Additive);
                break;
            case SCENES.FREELOOKING:
                SceneManager.LoadScene("Recall_free_looking", LoadSceneMode.Additive);
                break;
            case SCENES.CENTRALFIXATION:
                SceneManager.LoadScene("Recall_central_fixation", LoadSceneMode.Additive);
                break;
            case SCENES.PRESTUDYINSTRUCTIONS:
                SceneManager.LoadScene("PreStudyInstructions", LoadSceneMode.Additive);
                break;
            case SCENES.ENCODINGINSTRUCTIONS:
                SceneManager.LoadScene("Picture_encoding_instructions", LoadSceneMode.Additive);
                break;
            case SCENES.RECALLINSTRUCTIONS:
                SceneManager.LoadScene("Recall_instructions", LoadSceneMode.Additive);
                break;
            case SCENES.TARGETINSTRUCTION:
                SceneManager.LoadSceneAsync("TargetInstruction", LoadSceneMode.Additive);
                break;
            //case SCENES.PRACTICE:
            //    SceneManager.LoadSceneAsync("PracticeScene", LoadSceneMode.Additive);
            //    break;
            //case SCENES.ETCALIBRATION:
            //    SceneManager.LoadSceneAsync("ETCalibration", LoadSceneMode.Additive);
            //    break;
            case SCENES.LOADINGCONDITION:
                SceneManager.LoadScene("LoadingNextCondition", LoadSceneMode.Additive);
                break;
            //case SCENES.FLICKERINGCALIBRATION:
            //    SceneManager.LoadScene("Pilot", LoadSceneMode.Additive);
            //    break;
            case SCENES.ETVALIDATION:
                SceneManager.LoadScene("ET Validation", LoadSceneMode.Additive);
                break;
            /*case SCENES.CONDITION1:
                // load targets
                GameObject.Find("StudyObjects").SetActiveRecursively(true);
                //GameObject.Find("TriggerObjects").SetActive(false);
                GameObject.Find("DemonstratcionSpheres").SetActive(false);
                //targetController = TargetController.Controller;
                //targetController.DestroyRemaining();
                //targetController.setTargetColor(targetColor1, targetColor2);
                //targetController.Restart(); 
                print($"Loading trial: {trialNumb}, scene index {trialNumb}, and condition {conditions[trialNumb]}");  // Lise: what is targetSceneIndexOrder[trialNumb] ?
                //targetController.Restart(targetScenes[targetSceneIndexOrder[trialNumb]]);
                
                // load modulation for condition
                //triggerModulation = TriggerModulation.TModulation;
                //flickerIntensity = SphereFlicker.flickerIntensity;
                //triggerModulation.Restart(conditions[trialNumb], selectionByGaze, SphereFlicker.flickerIntensity);
                if (logging)
                    Log();
                newTrial = false;
                break; */
            case SCENES.QUESTIONNAIRE:
                SceneManager.LoadScene("Questionnaire", LoadSceneMode.Additive);
                break;
            case SCENES.NEWTRIAL:
                SceneManager.LoadScene("LoadingNextTrial", LoadSceneMode.Additive);
                break;
            case SCENES.POSTCONDITION:
                SceneManager.LoadScene("PostCondition", LoadSceneMode.Additive);
                break;
            case SCENES.POSTSTUDY:
                SceneManager.LoadScene("PostStudy", LoadSceneMode.Additive);
                break;
        }
    }

    private void unloadScene(SCENES scenes)
    {
        switch (scenes)
        {
            case SCENES.PICTUREGRID_1:
                SceneManager.UnloadSceneAsync("Picture_grid");
                break;
            case SCENES.PICTUREGRID_2:
                SceneManager.UnloadSceneAsync("Picture_grid_order2");
                break;
            case SCENES.FREELOOKING:
                SceneManager.UnloadSceneAsync("Recall_free_looking");
                break;
            case SCENES.CENTRALFIXATION:
                SceneManager.UnloadSceneAsync("Recall_central_fixation");
                break;
            case SCENES.ENCODINGINSTRUCTIONS:
                SceneManager.UnloadSceneAsync("Picture_encoding_instructions");
                break;
            case SCENES.RECALLINSTRUCTIONS:
                SceneManager.UnloadSceneAsync("Recall_instructions");
                break;
            case SCENES.PRESTUDYINSTRUCTIONS:
                SceneManager.UnloadSceneAsync("PreStudyInstructions");
                break;
            case SCENES.LOADINGCONDITION:
                SceneManager.UnloadSceneAsync("LoadingNextCondition");
                break;
            case SCENES.PRACTICE:
                SceneManager.UnloadSceneAsync("PracticeScene");
                break;
            case SCENES.ETCALIBRATION:
                SceneManager.UnloadSceneAsync("ETCalibration");
                break;
            case SCENES.TARGETINSTRUCTION:
                SceneManager.UnloadSceneAsync("TargetInstruction");
                break;
            case SCENES.FLICKERINGCALIBRATION:
                SceneManager.UnloadSceneAsync("Pilot");
                break;
            case SCENES.ETVALIDATION:
                SceneManager.UnloadSceneAsync("ET Validation");
                break;
            /*case SCENES.CONDITION1:
                GameObject studyObjects = GameObject.Find("StudyObjects");
                studyObjects.transform.GetChild(0).gameObject.SetActive(false);
                studyObjects.transform.GetChild(1).gameObject.SetActive(false);
                studyObjects.transform.GetChild(2).gameObject.SetActive(false);
                //targetController = TargetController.Controller;
                //targetController.DestroyRemaining();
                break; */
            case SCENES.NEWTRIAL:
                SceneManager.UnloadSceneAsync("LoadingNextTrial");
                break;
            case SCENES.POSTCONDITION:
                SceneManager.UnloadSceneAsync("PostCondition");
                break;
            case SCENES.QUESTIONNAIRE:
                SceneManager.UnloadSceneAsync("Questionnaire");
                break;
        }
        
    }

    void shuffle<T>(T[] arr, bool set_seed = false, int seed = 0)
    {
        if (set_seed)
        {
            Random.InitState(seed);
        }
        // Knuth shuffle algorithm
        for (int t = 0; t < arr.Length; t++)
        {
            var tmp = arr[t];
            int r = Random.Range(t, arr.Length);
            arr[t] = arr[r];
            arr[r] = tmp;
        }
    }
    
}

