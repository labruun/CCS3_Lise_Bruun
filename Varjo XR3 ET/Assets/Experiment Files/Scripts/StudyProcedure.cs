using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

using Setup;

//Generic Script for testing random shit. Remove after use
public class StudyProcedure : MonoBehaviour
{
    public UnityEvent ResetScene;
    // public UnityEvent LoadCondition;
    // public UnityEvent Interact;
    // Start is called before the first frame update
    void Start()
    {
        print(SceneSetup.Layout);
    }

    // Update is called once per frame
    void Update()
    {
    }

    void OnGUI()
    {
        if (GUI.Button(new Rect(10, 10, 100, 50), "Reload Scene"))
        {
            print("You clicked the button!");
            ResetScene.Invoke();
        }
        // if (GUI.Button(new Rect(110, 10, 100, 50), "Add Effects"))
        // {
        //     print("You clicked the other button!");
        //     LoadCondition.Invoke();
        // }
        // if (GUI.Button(new Rect(210, 10, 100, 50), "Interact "))
        // {
        //     print("You clicked yet another button!");
        //     Interact.Invoke();
        // }
    }
}
