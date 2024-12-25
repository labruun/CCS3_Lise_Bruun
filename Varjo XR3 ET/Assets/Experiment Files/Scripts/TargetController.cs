using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Random = UnityEngine.Random;
using Setup;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor.PackageManager;

/*
public class Target : MonoBehaviour
{
    private GameObject gameObject;
    private bool modulated, selected; // indicates whether target has been modulated or selected
    private Color defaultColor, modulationStoppedColor;
    private EyeTracking tracker;

    public string TargetID;
    

    public Target(GameObject gameObject, Color color)
    {
        this.gameObject = gameObject;
        defaultColor = color;
        tracker = EyeTracking.Tracker;

        // Set default values
        modulationStoppedColor = defaultColor;
        modulated = false;
        selected = false;
    }

    public GameObject getGameObject()
    {
        return gameObject;
    }

    public bool hasBeenModulated()
    {
        return modulated;
    }

    public bool hasBeenSelected()
    {
        return selected;
    }
    
    public Color getDefaultColor()
    {
        return defaultColor;
    }

    public void setModulation(bool modulation)
    {
        this.modulated = modulation;
    }

    public void resetDefaultColor()
    {
        //gameObject.GetComponent<MeshRenderer>().material.color = defaultColor;
        this.getGameObject().GetComponent<MeshRenderer>().material.color = Color.red;
    }

    public void modulationStopped()
    {
        gameObject.GetComponent<MeshRenderer>().material.color = modulationStoppedColor;
    }

    public void select()
    {        
        gameObject.GetComponent<SphereFlicker>().enabled = false; // stop flicker component on object
        this.modulationStopped(); // reset object color to "has been modulated"
        selected = true;
        UnityEngine.Object.Destroy(this.gameObject);
    }
}

public class TargetController : MonoBehaviour
{
    public TriggerModulation triggerModulation;
    public static TargetController Controller { get; private set; }
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

    [HideInInspector]
    public static List<Target> Targets = new List<Target>();
    [HideInInspector]
    public static List<Target> Distractors = new List<Target>();
    [HideInInspector]
    public bool updated = false;

    public int RemainingTargets;
    public int RemainingDistractors;

    public GameObject[] targetPrefabs;
    public Transform searchSpace;
    public bool localise = true;

    public bool neighbours = true;

    private Target debugTarget;

    private Vector3[] positions;
    private Vector3[] sizes;

    private EyeTracking gaze;
    private Logging logger;
    private KeyCode triggerSelection = KeyCode.Return;
    

    private string[] msg = new string[5];

    // private GameObject hitObject;
    private Target hitObject;
    //private bool hitTarget = false;
    private bool triggeredRemove = false;

    private Color targetColor1, targetColor2;
    private int tType;

    public bool logging = true;

    //private static readonly string[] ColumnNames = { "U_Frame", "LogTime", "Target", "Object Location","Object Local", "Object Colour", "Removed","Remaining Targets", "Remaining Distractors"};
    //Which object flickered, flickering object position, object being looked at (target id), position of object being looked at, object selected, object selected position.
    private static readonly string[] ColumnNames = { "U_Frame", "LogTime", "ObjectFlickeringID", "ObjectFlickeringPosition" , "ObjectHitID", "ObjectHitPosition", "ObjectHitColor" , "ObjectSelectedID", "ObjectSelectedPositionID", "ObjectSelectedColor", "ObjectSelectedWasFlickering"};
    // Start is called before the first frame update
    void Start()
    {
        gaze = EyeTracking.Tracker;
        gaze.Register(onGazeUpdate);
        logger = Logging.Logger;
        // logger.CreateLogger("Target", "ObjectHits");
        // logger.Record("ObjectHits",ColumnNames);
        if (logging)
        Logging.Logger.RecordTargetHits(ColumnNames, StudyController.participantID);
        // Restart();
    }

    public void Restart()
    {
        Restart(Random.Range(0,1));//DONE THIS WAY INSTEAD OF DEFAULTS TO ENABLE EDITOR TESTING
    }
    public void Restart(int type)
    {
        DestroyRemaining();
        InitialisePoses();
        InstantiateTargets(SceneSetup.TargetCount);
        InstantiateDistractors(SceneSetup.DistractorCount);
        Sort(new Vector3(0, 0, 0));
    }

    public void Restart(List<Vector3> positions)
    {
        DestroyRemaining();
        Localise();
        InstantiateAll(positions);
    }

    public static bool objectSelected = false;
    public static Vector3 selectedObjectPosition = new Vector3(0, 0, 0);
    public static GameObject ObjectSelectedGameObject = null;

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
                objectSelected = true;
                ObjectSelectedGameObject = t.getGameObject();
                selectedObjectPosition = t.getGameObject().transform.position;
                t.select();
            }

            // hitObject = hit;
            hitObject = new Target(hit, hit.GetComponent<MeshRenderer>().material.color);
            // Log(hit, target, false);
            if (logging)
                Log(hitObject);
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
    private bool Contains(List<Target> list, Target obj)
    {
        bool contains = false;
        for (int i = 0; i < list.Count; i++)
        {
            if(list[i].Equals(obj))
            {
                contains = true;
            }
        }

        return contains;
    }

    // private static readonly string[] ColumnNames = { "U_Frame", "LogTime", "ObjectHitID", "ObjectHitPosition", "ObjectHitColor" , "ObjectSelectedID", "ObjectSelectedPositionID", "ObjectSelectedColor"};
    void Log(Target hit){
        string[] msg = new string[ColumnNames.Length];
        msg[0] = Time.frameCount.ToString();
        msg[1] = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond).ToString();

        msg[2] = triggerModulation.targetObject?.getGameObject().name;
        msg[3] = triggerModulation.targetObject?.getGameObject().transform.position.ToString("F6");

        msg[4] = hit.getGameObject().name;
        msg[5] = hit.getGameObject().transform.position.ToString("F6");
        msg[6] = hit.getDefaultColor().ToString("F6");

        msg[7] = objectSelected ? hitObject.getGameObject().name : "None";
        msg[8] = objectSelected ? selectedObjectPosition.ToString("F6") : "None";

        msg[9] = objectSelected ? hitObject.getDefaultColor().ToString("F6") : "None";

        msg[10] = objectSelected ? (hitObject.getGameObject().name == triggerModulation.targetObject?.getGameObject().name ? "True" : "False") : "None";

        // logger.Record("ObjectHits",msg);
        Logging.Logger.RecordTargetHits(msg, StudyController.participantID);
        
    }
    //Ensure frame accuracy
    void LateUpdate()
    {
        if (triggeredRemove)
        {
            RemoveTarget(hitObject);
            triggeredRemove = false;
        }
    }

    public void RemoveGaze()
    {
        //triggeredRemove = true;
        //NOT FRAME PERFECT STILL TO SOLVE
        RemoveTarget(hitObject);
    }

    public void TestHit()
    {
        if (Targets.Count > 0)
        {
            var o = Targets[0];
            RemoveTarget(o, true);
        }
    }

    // public void RemoveTarget(GameObject o)
    public void RemoveTarget(Target o)
    {
        bool target = Contains(Targets, o);
        
        if (target||Contains(Distractors, o))
        {
            RemoveTarget(o, target);
            Log(o);
        }
    }

    // private void RemoveTarget(GameObject o, bool target)
    private void RemoveTarget(Target o, bool target)
    {
        if (target)
        {
            Targets.Remove(o);
            // Destroy(o);
            Destroy(o.getGameObject());
        }
        else
        {
            Distractors.Remove(o);
            // Destroy(o);
            Destroy(o.getGameObject());
        }
    }

    void Sort(Vector3 p)
    {
        Vector3 loc = p;
        //naive full search. Can we do better?
        for (int i = 0; i < Targets.Count; i++)
        {
            int pos = i + findMin(Targets.Skip(i).ToArray(), loc);
            //print(pos+":"+i);
            var tmp = Targets[i];
            Targets[i] = Targets[pos];
            Targets[pos] = tmp;
            // if (neighbours) loc = Targets[i].transform.position;
            if (neighbours) loc = Targets[i].getGameObject().transform.position;
        }

    }

    public void DestroyRemaining()
    {
        foreach (Target o in Targets)
        {
            Destroy(o.getGameObject());
        }
        foreach (Target o in Distractors)
        {
            Destroy(o.getGameObject());
        }
        Targets = new List<Target>();
        Distractors = new List<Target>();
    }

    void Distances()
    {
        // foreach (GameObject target in Targets)
        foreach (Target target in Targets)
        {
            for (int i = 0; i < Targets.Count; i++)
            {
                float d = Vector3.Distance(((Target)Targets[i]).getGameObject().transform.position, target.getGameObject().transform.position);
                if (d < SceneSetup.TargetSize) print(d);
            }
            for (int i = 0; i < Distractors.Count; i++)
            {
                float d = Vector3.Distance(((Target)Distractors[i]).getGameObject().transform.position, target.getGameObject().transform.position);
                if (d < SceneSetup.TargetSize) print(d);
            }

        }
        // foreach (GameObject target in Distractors)
        foreach (Target target in Distractors)
        {
            for (int i = 0; i < Targets.Count; i++)
            {
                float d = Vector3.Distance(((Target)Targets[i]).getGameObject().transform.position, target.getGameObject().transform.position);
                if (d < SceneSetup.TargetSize) print(d);
            }
            for (int i = 0; i < Distractors.Count; i++)
            {
                float d = Vector3.Distance(((Target)Distractors[i]).getGameObject().transform.position, target.getGameObject().transform.position);
                if (d < SceneSetup.TargetSize) print(d);
            }

        }
    }
    // Update is called once per frame
    void Update()
    {
        // counter++;
        if (RemainingTargets != Targets.Count) updated = true;
        else updated = false;
        RemainingDistractors = Distractors.Count;
        RemainingTargets = Targets.Count;
        // if (counter == 120) {
        //     Debug.Log("Position: " + debugTarget.getGameObject().transform.localPosition); // REMOVE THIS
        //     counter = 0;
        //     debugTarget.resetDefaultColor();
        // }
        
        if (debugTarget.getGameObject() != null)
        {
            if (Input.GetKeyDown(KeyCode.W))
            {
                debugTarget.getGameObject().transform.position += new Vector3(0, 0.1f, 0);
            }
            if (Input.GetKeyDown(KeyCode.S))
            {
                debugTarget.getGameObject().transform.position += new Vector3(0, -0.1f, 0);
            }
            if (Input.GetKeyDown(KeyCode.A))
            {
                debugTarget.getGameObject().transform.position += new Vector3(-0.1f, 0, 0);
            }
            if (Input.GetKeyDown(KeyCode.D))
            {
                debugTarget.getGameObject().transform.position += new Vector3(0.1f, 0, 0);
            }
        }
    }
    void Localise()
    {
        if (localise)
        {
            Vector3 pos = searchSpace.transform.position;
            Vector3 camPos = Camera.main.transform.position;
            searchSpace.transform.position = new Vector3(camPos.x, camPos.y, camPos.z);
            //Vector3 forward = Camera.main.transform.forward;
            //searchSpace.transform.LookAt(camPos + new Vector3(forward.x, 0, forward.z));
        }
    }

    void InitialisePoses()
    {
        Localise();
        positions = new Vector3[SceneSetup.TargetCount + SceneSetup.DistractorCount];
        switch (SceneSetup.Distribution)
        {
            case Distribution.RANDOM:
                int n = 0;
                //Just dart throwing
                float startTime = Time.fixedTime;
                while (n < positions.Length)
                {
                    if ((Time.fixedTime - startTime) > 1) n = 0;
                    if ((Time.fixedTime - startTime) > 5) Debug.LogError("Couldn't find solution in timelimit");
                    Vector3 testPos = new Vector3(Random.value, Random.value, Random.value);
                    testPos = rng2World(testPos);
                    bool valid = true;
                    for (int i = n; i >= 0; i--)
                    {
                        if (valid) valid = Vector3.Distance(testPos, positions[i]) > SceneSetup.TargetSize * 1.1f;//SHOULDN'T NEED THIS BIG A MARGIN;
                    }
                    if (!valid) continue;
                    positions[n++] = testPos;
                }
                break;
            case Distribution.ROWS_RAND:
                int total = 0;
                int min = 6;
                int inRow = Random.Range(min, positions.Length - SceneSetup.NRows*min-total);
                int row = 0;
                total += inRow;
                for (int i = 0; i < positions.Length; i++)
                {
                    print((SceneSetup.Dimensions.x / SceneSetup.TargetSize));
                    positions[i] = rng2World(new Vector3((inRow--)/(SceneSetup.Dimensions.x/SceneSetup.TargetSize)+0.01f, (float)row/(float)SceneSetup.NRows, 0.5f));//ONLY WORKS FOR BOX...
                    if (inRow < 0)
                    {
                        row++;
                        inRow = Random.Range(min, positions.Length - SceneSetup.NRows * min - total);//TAKE CARE OF ACTUALLY GETTING ALL THE POSITIONS FILLED within nrows
                        total += inRow;
                    }

                }
                print("Rows:"+row);
                break;
            default:
                print(SceneSetup.Distribution);
                Debug.LogWarning("Unimplemented Distribution");
                return;
        }
        shuffle(positions);
    }

    void InstantiateAll(List<Vector3> positions)
    {
        for(int i = 0; i < positions.Count/4; i++)
        {
            Targets.Add(Instantiate(0, positions[i], targetColor1, i, "Target"));
            Targets.Add(Instantiate(1, positions[i+positions.Count/4], targetColor2, i, "Target"));
            Distractors.Add(Instantiate(0, positions[i+positions.Count/2], targetColor2, i, "Distractor"));
            Distractors.Add(Instantiate(1, positions[i+(int)(positions.Count*0.75)], targetColor1, i, "Distractor"));
        }
        // In case of even number of positions
        Targets.Add(Instantiate(1, positions[positions.Count/2 - 1], targetColor2, 12, "Target"));
        Distractors.Add(Instantiate(1, positions[positions.Count - 1], targetColor2, 12, "Distractor"));
        debugTarget = Targets[0];
    }

    void InstantiateTargets(int number)
    {
        Targets = new List<Target>();
        for (int i = 0; i < SceneSetup.TargetCount/2; i++)
        {
            Targets.Add(Instantiate(0, i, targetColor1));
            Debug.Log("Target "+i);
            if (i == 0) debugTarget = Targets[0];
        }
        for (int i = SceneSetup.TargetCount/2; i < SceneSetup.TargetCount; i++)
        {
            Targets.Add(Instantiate(1, i, targetColor2));
        }
    }
    void InstantiateDistractors(int number)
    {
        Distractors = new List<Target>();
        for (int i = SceneSetup.TargetCount; i < (SceneSetup.TargetCount + SceneSetup.DistractorCount/2); i++)
        {
            Distractors.Add(Instantiate(0, i, targetColor2));
        }
        for (int i = (SceneSetup.TargetCount + SceneSetup.DistractorCount/2); i < (SceneSetup.TargetCount + SceneSetup.DistractorCount); i++)
        {
            Distractors.Add(Instantiate(1, i, targetColor1));
        }
    }

    public void CreateExample(Color colour, int type, Vector3 position)
    {
        Localise();
        InstantiateExampleTarget(colour, type, position);
    }
    void InstantiateExampleTarget(Color color, int type, Vector3 position)
    {
        positions = new Vector3[] { rng2World(position) };
        Target example = Instantiate(type, 0, color);
        example.getGameObject().transform.localScale = example.getGameObject().transform.localScale * 3f ;
        Targets.Add(example);
    }

    Target Instantiate(int prefab, Vector3 position, Color color, int i, string type)
    {
        Target obj = new Target(Instantiate(targetPrefabs[prefab], position, Quaternion.identity, searchSpace), color);
        obj.getGameObject().transform.localPosition = position;
        obj.getGameObject().GetComponent<Renderer>().material.SetColor("_BaseColor", color);
        obj.getGameObject().transform.localScale = new Vector3(obj.getGameObject().transform.localScale.x * SceneSetup.TargetSize,
                                                obj.getGameObject().transform.localScale.y * SceneSetup.TargetSize,
                                                obj.getGameObject().transform.localScale.z * SceneSetup.TargetSize);
        obj.TargetID = $"{targetPrefabs[prefab].name}_{type}_{i}";
        obj.getGameObject().name = obj.TargetID;
        return obj;
    }
    Target Instantiate(int prefab, int position, Color color)
    {
        Target obj = new Target(Instantiate(targetPrefabs[prefab], positions[position], Quaternion.identity, searchSpace), color);
        obj.getGameObject().transform.localPosition = positions[position];
        obj.getGameObject().GetComponent<Renderer>().material.SetColor("_BaseColor", color);
        obj.getGameObject().transform.localScale = new Vector3(obj.getGameObject().transform.localScale.x * SceneSetup.TargetSize,
                                                obj.getGameObject().transform.localScale.y * SceneSetup.TargetSize,
                                                obj.getGameObject().transform.localScale.z * SceneSetup.TargetSize);
        switch (SceneSetup.Variance) {
            case Variances.NONE:
                break;
            case Variances.SIZE:
                obj.getGameObject().transform.localScale += new Vector3(obj.getGameObject().transform.localScale.x * Random.Range(-0.1f,0.1f),
                                                        obj.getGameObject().transform.localScale.y * Random.Range(-0.1f, 0.1f),
                                                        obj.getGameObject().transform.localScale.z * Random.Range(-0.1f, 0.1f));
                break;
            default:
                Debug.LogWarning("Umimplemented Variance");
                break;
        }


        return obj;
    }

    void shuffle<T>(T[] arr)
    {
        // Knuth shuffle algorithm
        for (int t = 0; t < arr.Length; t++)
        {
            var tmp = arr[t];
            int r = Random.Range(t, arr.Length);
            arr[t] = arr[r];
            arr[r] = tmp;
        }
    }

    private Vector3 rng2World(Vector3 rngPos)
    {
        switch (SceneSetup.Layout)
        {
            case Layout.BOX:
                return (Vector3.Scale(rngPos, SceneSetup.Dimensions) - SceneSetup.Dimensions / 2f) + SceneSetup.Offset;//Needs adjusted for user facing.
            case Layout.FRUSTRUM:
                Vector3 pos = new Vector3(0, 0, rngPos.z * ((SceneSetup.Limits.y-0.5f) - (SceneSetup.Limits.x-0.5f)) + (SceneSetup.Limits.x-0.5f));
                float angleX = SceneSetup.Angles.x * (rngPos.x - 0.5f);
                float angleY = SceneSetup.Angles.y * (rngPos.y - 0.5f);
                pos = Quaternion.Euler(angleY, angleX, 0) * pos;
                return pos;
            default:
                Debug.LogWarning("Unimplemented Layout Conversion");
                break;
        }
        return rngPos;
    }

    // int findMin(GameObject[] points, Vector3 point)
    int findMin(Target[] points, Vector3 point)
    {
        //print(points.Length);
        float m = Mathf.Infinity;
        int p = -1;
        for (int i = 0; i < points.Length; i++)
        {
            //Could also do nn in 2D
            // Vector3 altP = points[i].transform.position;
            Vector3 altP = points[i].getGameObject().transform.position;
            if (Vector3.Distance(point, altP) < m)
            {
                m = Vector3.Distance(point, altP);
                p = i;
            }
        }
        return p;
    }

    public void setTargetColor(Color color1, Color color2)
    {
        targetColor1 = color1;
        targetColor2 = color2;
    }
}
*/ 