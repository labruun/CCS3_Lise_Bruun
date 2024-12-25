using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.VisualScripting;


/*
public enum CONDITION
{
    BASELINE,
    RAND_COMPONENT,
    DIST_COMPONENT,
    CLOSE_COMPONENT,
    MANUAL
}
public class TriggerModulation : MonoBehaviour
{
    public static TriggerModulation TModulation { get; private set; }
    private void Awake()
    {
        // If there is an instance, and it's not me, delete myself.

        if (TModulation != null && TModulation != this)
        {
            Debug.LogWarning("Only one target modulation instance allowed");
            Destroy(this);
        }
        else
        {
            TModulation = this;
        }
    }

    public bool pilotStudy = false;

    public StudyController studyController;

    // Keys for triggering modulation in object
    public KeyCode triggerLeft = KeyCode.LeftArrow;
    public KeyCode triggerRight = KeyCode.RightArrow;

    // set current condition
    public CONDITION CurrentCondition;

    public bool calibrationFlickeringDone = false;

    public GameObject calibrationEndScreen;
    // Logging
    private string[] study_ColumnNames =  { "x", "y"};
    private int participantID;

    // Get eye tracker
    private EyeTracking tracker;


    // Get Target controller
    private TargetController targetController;

    private Vector3 fixationPoint;

    private List<Vector3> lastFixationPoints;

    // private List<GameObject> targetStimuli;
    private List<Target> targetStimuli;
    private bool foundTargetStimuli;

    private bool modulationOngoing, startNextModulation; // indicates whether a modulation is currently ongoing

    // condition-specific components
    private bool conditionStarted;
    private int remainingTargetsToModulate;

    // private GameObject targetObject;
    public Target targetObject;
    public Vector3 currentTargetPosition;
    private float flickerIntensity;

    private bool trialCompleted; // Indicate whether current condition has been completed, true means that it has been completed, whereas false means that it hasn't
    private bool selectionByGaze;
    Dictionary<int, Vector3> currentTargetFlickeringCalibration = new Dictionary<int, Vector3>();

    // Start is called before the first frame update
    void Start()
    {

        lastFixationPoints = new List<Vector3>();
        // Start eye tracker
        tracker = EyeTracking.Tracker;

        modulationOngoing = false;
        startNextModulation = true;
        foundTargetStimuli = false;
        
        // Set variables for conditions
        conditionStarted = false;
        trialCompleted = false;

            
       


        // if(logging)
        // {
        //     Logging.Logger.RecordStudyInfo(study_ColumnNames, participantID);
        //     // LogStudyInfo(currentPos, new Vector3(), new Vector3(), new Vector3(), new Vector3(), new Vector3(), new Vector3(), saccadeDetected); // first row is empty
        // }
    }

    public void Restart(CONDITION condition, bool selectionByGaze, float flickerIntensity)
    {
        modulatedTargets = 0;
        CurrentCondition = condition;
        this.selectionByGaze = selectionByGaze;
        this.flickerIntensity = flickerIntensity;

        modulationOngoing = false;
        foundTargetStimuli = false;
        startNextModulation = true;
        
        // Set variables for conditions
        conditionStarted = false;
        trialCompleted = false;
    }

    public void findTargetObject()
    {
        // Load Condition
        switch (CurrentCondition)
        {
            case CONDITION.BASELINE:
                break;
            case CONDITION.RAND_COMPONENT:
                loadRandomCondition();
                break;
            case CONDITION.DIST_COMPONENT:
                loadDistantCondition();
                break;
            case CONDITION.CLOSE_COMPONENT:
                loadCloseCondition();
                break;
            case CONDITION.MANUAL:
                loadManualCondition();
                break;
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Get current fixation point
        fixationPoint = tracker.gazeInfo.fixationPoint;

        lastFixationPoints.Add(fixationPoint);

        if (lastFixationPoints.Count > 2)
        {
            lastFixationPoints.RemoveAt(0);
        }
        

        // Get target spheres
        if(!foundTargetStimuli) // targets are loaded dynamically, therefore have to wait for first update call to get targets
        {
            targetStimuli = TargetController.Targets;
            if (pilotStudy)
            {
                targetStimuli = TargetControllerPilot.Targets;
                foreach (Target target in targetStimuli)
                {
                    target.getGameObject().SetActive(true);
                }
            }

            if(targetStimuli.Count > 0)
            {
                for(int i = 0; i < targetStimuli.Count; i++)
                {
                    targetStimuli[i].setModulation(false);
                }
                targetObject = null;
                foundTargetStimuli = true;
            }
            return;

        }
        
        //Check if trial has been completed
        bool allModulated = true;
        bool allSelected = true;

        for(int i=0; i<targetStimuli.Count; i++)
        {
            if(!targetStimuli[i].hasBeenModulated())
            {
                allModulated = false;
            }
            if(!targetStimuli[i].hasBeenSelected())
            {
                allSelected = false;
            }
        }

        if (Input.GetKeyDown(KeyCode.G))
        {
            trialCompleted = true;
        }

        if (pilotStudy)
        {
            loadPilotCondition();
            StopModulationPilot(TargetControllerPilot.CrossFaded);
            return;
        }

        // Select modulation condition
        if(selectionByGaze && allSelected)
        {
            trialCompleted = true;
        }
        else if(!selectionByGaze && allModulated)
        {
            trialCompleted = true;
        }
        else
        {
            findTargetObject();
        }
        if(modulationOngoing && Time.frameCount > targetFrameAquired + 1)
        {
            // Disable modulation if saccade is detected
            if(SaccadeDetection.ongoingSaccade) // stop if saccade is executed towards the target
            {
                float distanceToTarget = (Mathf.Abs(targetObject.getGameObject().transform.position.z - Camera.main.transform.position.z));
                Vector3 PlaneCoordinates = new Vector3(Camera.main.transform.position.x, Camera.main.transform.position.y, Camera.main.transform.position.z - (-distanceToTarget));
                Plane plane = new Plane(Camera.main.transform.forward, PlaneCoordinates);
                Ray fixationRayOrigin = new Ray(Camera.main.transform.position, lastFixationPoints[0] - Camera.main.transform.position);
                Ray fixationRayDirection = new Ray(Camera.main.transform.position, lastFixationPoints[1] - Camera.main.transform.position);
                if (plane.Raycast(fixationRayOrigin, out float distance))
                {
                    Vector2 originPoint = new Vector2(fixationRayOrigin.GetPoint(distance).x, fixationRayOrigin.GetPoint(distance).y); 
                    Vector2 directionPoint = new Vector2(fixationRayDirection.GetPoint(distance).x, fixationRayDirection.GetPoint(distance).y);
                    Vector2 directionVector = (directionPoint - originPoint).normalized;
                    Vector2 directionVectorTarget = (new Vector2(targetObject.getGameObject().transform.position.x, targetObject.getGameObject().transform.position.y) - originPoint).normalized;
                    float angle = Vector2.Angle(directionVector, directionVectorTarget);
                    if (angle < 20)
                    {
                        //Debug.Log("angle too small, stop modulation");
                        StopModulation(targetObject); 
                    }
                }
                // float angle = Vector3.Angle(fixationPoint, targetObject.getGameObject().transform.localPosition);
                // Debug.Log("target: " + targetObject.getGameObject().transform.position + ", angle: " + angle + ", distance: " + Vector3.Distance(SaccadeDetection.currentPos, targetObject.getGameObject().transform.position));
                // if(angle < 20)
                // {
                //     Debug.Log("angle too small, stop modulation");
                //     StopModulation(targetObject);
                // }
            }
            if(targetObject.getGameObject() is not null && Vector3.Distance(SaccadeDetection.currentPos, targetObject.getGameObject().transform.position) < 0.25) // stop if gaze is too close to target
            {
                Debug.Log("gaze too close");
                StopModulation(targetObject);
            }
           
            

            // Check if any target has been selected
            // if(targetObject.hasBeenSelected()) // target has been selected
            // {
            //     modulationOngoing = false;
            //     startNextModulation = true;
            // }
            
        }

        if(TargetController.objectSelected) // target has been selected
        {
            TargetController.objectSelected = false;
            if (TargetController.ObjectSelectedGameObject != targetObject?.getGameObject())
                StopModulation(targetObject);
            TargetController.ObjectSelectedGameObject = null;
            modulationOngoing = false;
            startNextModulation = true;
        }
        

        // LogStudyInfo(int condition, int trial)
    }

    private void StopModulationPilot(GameObject Cross)
    {
        Vector3 fixationPoint = tracker.gazeInfo.fixationPoint;
        // Check if fixationpoint is close to Cross
        if (Vector2.Distance(new Vector2(fixationPoint.x, fixationPoint.y), new Vector2(Cross.transform.position.x, Cross.transform.position.y)) < 0.6)
        {
            StartModulation(targetObject, CONDITION.DIST_COMPONENT, SphereFlicker.flickerIntensity);
        }
        else
        {
            StopModulation(targetObject);
        }


    }

    public bool getTrialStatus()
    {
        return trialCompleted;
    }

    public void setTrialStatus(bool status)
    {
        trialCompleted = status;
    }

    private void loadRandomCondition()
    {
        if(!conditionStarted) // Called once at start of modulation
        {
            remainingTargetsToModulate = targetStimuli.Count;
            conditionStarted = true;
        }

        if(!modulationOngoing) // Check if modulation is currently ongoing
        {
            if(startNextModulation)
            {
                List<Target> tempList = new List<Target>();
                for(int i=0; i<targetStimuli.Count; i++)
                {
                    if(!targetStimuli[i].hasBeenSelected())// && !targetStimuli[i].hasBeenModulated())
                    {
                        if (!targetOnScreen(targetStimuli[i]))
                            continue;
                        tempList.Add(targetStimuli[i]);
                    }
                }

                targetObject = tempList[Random.Range(0, tempList.Count-1)];
                StartModulation(targetObject, CONDITION.RAND_COMPONENT, flickerIntensity); // Modulate new target
                lastTargetSetForModulation = targetObject.getGameObject();
                startNextModulation = false;
                targetFrameAquired = Time.frameCount;
                remainingTargetsToModulate--;
            }
        }
    }

    public GameObject lastTargetSetForModulation;
    private void loadDistantCondition()
    {

        if(!conditionStarted) // Called once at start of modulation
        {
            remainingTargetsToModulate = targetStimuli.Count;
            conditionStarted = true;

            int rnd = Random.Range(0, 2);
            if(rnd == 0)
            {
                targetObject = GetTargetPos("LEFT");
                currentTargetPosition = targetObject.getGameObject().transform.position;
            }
            else if(rnd == 1)
            {
                targetObject = GetTargetPos("RIGHT");
                currentTargetPosition = targetObject.getGameObject().transform.position;
            }
        }
        
        if(!modulationOngoing) // Check if modulation is currently ongoing
        {
            if(startNextModulation)
            {

                List<Target> tempList = new List<Target>();
                for(int i=0; i<targetStimuli.Count; i++)
                {
                    if(!targetStimuli[i].hasBeenSelected())// && !targetStimuli[i].hasBeenModulated())
                    {
                        tempList.Add(targetStimuli[i]);
                    }
                }
                if (TargetController.selectedObjectPosition != Vector3.zero)
                    currentTargetPosition = TargetController.selectedObjectPosition;
                targetObject  = getDistantTarget(currentTargetPosition, tempList);
                //currentTargetPosition = targetObject.getGameObject().transform.position;
                
                StartModulation(targetObject, CONDITION.DIST_COMPONENT, flickerIntensity); // Modulate new target
                lastTargetSetForModulation = targetObject.getGameObject();
                startNextModulation = false;
                remainingTargetsToModulate--;
                targetFrameAquired = Time.frameCount;
            }
        }
    }

    private int targetFrameAquired = 0;

    private int targetPilotCounter = 0;
    private bool pilotConditionStarted = true;
    private int calibrationRound = 0;
    private bool nextPermutation = false;
    private Vector3 desiredPosition;
    private bool setTargets = false;


    private List<float> calibrationNumbers = new List<float>();
    private void loadPilotCondition()
    {
        if (!setTargets){
            currentTargetFlickeringCalibration.Add(0, targetStimuli[0].getGameObject().transform.localPosition); // 0 == top left, 1 == bottom left, 2 == bottom right, 3 == top right
            currentTargetFlickeringCalibration.Add(1, targetStimuli[1].getGameObject().transform.localPosition);
            currentTargetFlickeringCalibration.Add(2, targetStimuli[2].getGameObject().transform.localPosition);
            currentTargetFlickeringCalibration.Add(3, targetStimuli[3].getGameObject().transform.localPosition);
            setTargets = true;
        }
        if (Input.GetKeyUp(KeyCode.RightControl) || SphereFlicker.calibrationDone) {
            targetPilotCounter++;
            SphereFlicker.calibrationDone = false;
            nextPermutation = true;
            StopModulation(targetObject);
            calibrationNumbers.Add(SphereFlicker.flickerIntensity + SphereFlicker.secondFlickerAdjustment / 2);
            pilotConditionStarted = true;
            if (targetPilotCounter % 4 == 0) {
                calibrationRound++;
            }
        }
        if (!pilotConditionStarted) return;
        pilotConditionStarted = false;
        if (nextPermutation) {
            if (targetPilotCounter >= 8) {
                float sum = 0;
                foreach (float number in calibrationNumbers) {
                    sum += number;
                }
                float average = sum / calibrationNumbers.Count;
                print("Calibration finished");
                print("Calibration number average: " + average);
                SphereFlicker.flickerIntensity = average;
                calibrationEndScreen.SetActive(true);
                calibrationFlickeringDone = true;
                pilotStudy = false;
                return;
            }
            
            TargetControllerPilot targetControllerPilot = GameObject.Find("ScripManager").GetComponent<TargetControllerPilot>();
            targetControllerPilot.RestartRound();
            nextPermutation = false;
            pilotConditionStarted = true;
            foundTargetStimuli = false;
            
            return;
        }

        
        Vector3 targetToBeModulated; 
        foreach (Target target in targetStimuli) {
            Vector3 positionOfTarget = currentTargetFlickeringCalibration[targetPilotCounter % 4];
            if (target.getGameObject().transform.localPosition == positionOfTarget) {
                targetToBeModulated = target.getGameObject().transform.localPosition;
                targetObject = target;
                break;
            }
            // int indexForTargetObject = targetStimuli.FindIndex(target => target.getGameObject().transform.localPosition == desiredPosition);
            // print(indexForTargetObject);
            // targetObject = targetStimuli[indexForTargetObject];
        }

        StopModulationPilot(TargetControllerPilot.CrossFaded);
        //StartModulation(targetObject, CONDITION.DIST_COMPONENT, SphereFlicker.flickerIntensity);    
    }

    private void loadCloseCondition()
    {
        if(!conditionStarted) // Called once at start of modulation
        {
            remainingTargetsToModulate = targetStimuli.Count;
            conditionStarted = true;

            int rnd = Random.Range(0, 1);
            if(rnd == 0)
            {
                targetObject = GetTargetPos("LEFT");
                currentTargetPosition = targetObject.getGameObject().transform.position;
            }
            else if(rnd == 1)
            {
                targetObject = GetTargetPos("RIGHT");
                currentTargetPosition = targetObject.getGameObject().transform.position;
            }
        }
        
        if(!modulationOngoing) // Check if modulation is currently ongoing
        {
            if(startNextModulation)
            {
                List<Target> tempList = new List<Target>();
                for(int i=0; i<targetStimuli.Count; i++)
                {
                    if(!targetStimuli[i].hasBeenSelected())// && !targetStimuli[i].hasBeenModulated())
                    {
                        tempList.Add(targetStimuli[i]);
                    }
                }
                if (TargetController.selectedObjectPosition != Vector3.zero)
                    currentTargetPosition = TargetController.selectedObjectPosition;
                targetObject  = getCloseTarget(currentTargetPosition, tempList);
                currentTargetPosition = targetObject.getGameObject().transform.position;

                StartModulation(targetObject, CONDITION.CLOSE_COMPONENT, flickerIntensity); // Modulate new target
                lastTargetSetForModulation = targetObject.getGameObject();
                startNextModulation = false;
                targetFrameAquired = Time.frameCount;
                remainingTargetsToModulate--;
            }
        }
    }

    public void nextModulation(bool b)
    {
        startNextModulation = b;
    }

    private Target getCloseTarget(Vector3 startTargetPos, List<Target> tempList)
    {
        float distance = Mathf.Infinity;
        Target endTarget = null;

        for(int i = 0; i < tempList.Count; i++)
        {
            float currentDist = Vector3.Distance(tempList[i].getGameObject().transform.position, startTargetPos);
            if(currentDist < distance)
            {
                distance = currentDist;
                endTarget = tempList[i];
            }
        }

        return endTarget;
    }

    private Target getDistantTarget(Vector3 startTargetPos, List<Target> tempList)
    {
        float distance = 0;
        Target endTarget = null;
        for(int i = 0; i < tempList.Count; i++)
        {
            if (!targetOnScreen(tempList[i]))
                continue;
            float currentDist = Vector3.Distance(tempList[i].getGameObject().transform.position, startTargetPos);
            if(currentDist > distance)
            {
                distance = currentDist;
                endTarget = tempList[i];
            }
        }

        return endTarget;
    }

    private bool targetOnScreen(Target target)
    {
        // Check if currently modulated stimulus is on screen
        Vector3 screenPoint = Camera.main.WorldToViewportPoint(target.getGameObject().transform.position);
        bool onScreen = screenPoint.z > 0.05 && screenPoint.x > 0.05 && screenPoint.x < 0.95 && screenPoint.y > 0 && screenPoint.y < 0.95;

        return onScreen;
    }

    private void loadManualCondition()
    {   
        if (Input.GetKeyDown(triggerLeft)) // Get left most target position
        {
            if(targetObject is not null)
            {
                StopModulation(targetObject);
            }
            targetObject = GetTargetPos("LEFT");
            if(targetObject is not null)
            {
                StartModulation(targetObject, CONDITION.MANUAL, 0f);
            }
        }
        else if(Input.GetKeyDown(triggerRight)) // Get right most target position
        {
            if(targetObject is not null)
            {
                StopModulation(targetObject);
            }
            targetObject = GetTargetPos("RIGHT");
            if(targetObject is not null)
            {
                StartModulation(targetObject, CONDITION.MANUAL, 0f);
            }
        }

        // stop modulation when user is performing a saccade into the direction of the target
        // check if there is an ongoing saccade
        // check if the angle of the saccade is too close to target
        if(modulationOngoing)
        {
            if(targetObject.hasBeenSelected())
            {
                modulationOngoing = false;
            }
            else if(SaccadeDetection.ongoingSaccade)
            {
                float angle = Vector3.Angle(fixationPoint, targetObject.getGameObject().transform.position);
                if(angle < 10)
                {
                    Debug.Log("angle too small, stop modulation");
                    StopModulation(targetObject);
                }
            }
            else if(Vector3.Distance(SaccadeDetection.currentPos, targetObject.getGameObject().transform.position) < 0.1)
            {
                Debug.Log("gaze too close");
                StopModulation(targetObject);
            }
        }
    }
    public int modulatedTargets = 0;
    
    private void StartModulation(Target targetObject, CONDITION condition, float intensity)
    {
        if(!targetObject.hasBeenSelected())
        {
            modulatedTargets++;
            targetObject.getGameObject().tag = "IsFlickering";
            targetObject.getGameObject().GetComponent<SphereFlicker>().enabled = true;
            SphereFlicker sphereFlicker = targetObject.getGameObject().GetComponent<SphereFlicker>();
            sphereFlicker.setFlickerProperties(condition, intensity, targetObject.getDefaultColor());
            targetObject.getGameObject().GetComponent<SphereFlicker>().startFlicker();
            targetObject.setModulation(true);
            modulationOngoing = true;
        }
    }

    private void StopModulation(Target targetObject)
    {
        //if (targetObject is null)
        //{
        //    return;
        //}
        if (!targetObject.hasBeenSelected())
        {
            targetObject.getGameObject().GetComponent<SphereFlicker>().enabled = false; // stop flicker component on object
            targetObject.modulationStopped(); // reset object color to "has been modulated"
        }

        modulationOngoing = false;
    }

    private Target GetTargetPos(string direction)
    {
        // Check if there are targets
        if(targetStimuli.Count == 0)
        {
            Debug.Log("There are no target stimuli.");
            return null;
        }

        Vector3 tempPos = fixationPoint;
        Target outVal = null;
        
        if(direction.Equals("LEFT")) // Return left most target
        {
            for (int i = 0; i < targetStimuli.Count; i++)
            {
                Target tempStimulus = targetStimuli[i];

                if(!tempStimulus.hasBeenSelected())
                {
                    if(tempStimulus.getGameObject().transform.position.x < tempPos.x && targetOnScreen(tempStimulus)) // look forleft most target position that is still in a user's field of view
                    {
                        tempPos = tempStimulus.getGameObject().transform.position;
                        outVal = tempStimulus;
                    }
                }
            }
            return outVal;
        }
        else // Return right most target
        {
            for (int i = 0; i < targetStimuli.Count; i++)
            {
                Target tempStimulus = targetStimuli[i];

                if(!tempStimulus.hasBeenSelected())
                {
                    if(tempStimulus.getGameObject().transform.position.x > tempPos.x && targetOnScreen(tempStimulus))
                    {
                        tempPos = tempStimulus.getGameObject().transform.position;
                        outVal = tempStimulus;
                    }
                }
            }
            return outVal;
        }
    }

    public void setParticipantID(int id)
    {
        participantID = id;
    }

    private void LogStudyInfo(int condition, int trial)
    {

    }

    private void LogStudySettings(int condition, int trial)
    {
        string[] logData = new string[]{"test", "test"};
        logData[0] = condition.ToString();
        logData[1] = trial.ToString();
    }
}
*/