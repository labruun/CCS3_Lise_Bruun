using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine;
using System.Collections;

public class Grid_object_manager : MonoBehaviour
{
    public GameObject[] objects; // Assign your 4 objects in the Inspector
    private int currentIndex = 0;
    public bool isComplete = false; // Flag to signal completion

    // public event Action OnTaskCompleted;

    
    public void CompleteTask()
    {
        // Your logic for completing the task
        Debug.Log("Event: Task completed in Grid_object_manager");

        // Invoke the event
        //OnTaskCompleted?.Invoke();
    }

    public bool GetIsComplete()
    {
        Debug.Log("GetIsComplete() called in Grid_object_manager");
        return isComplete;
    }

    public void ResetIsComplete()
    {
        Debug.Log("ResetIsComplete() called in Grid_object_manager");
        isComplete = false;
    }

    void Start()
    {
        if (objects.Length != 4)
        {
            Debug.LogError("Please assign exactly 4 objects in the Inspector.");
            return;
        }

        StartCoroutine(ManageObjects());
    }

    IEnumerator ManageObjects()
    {
        // Ensure all objects are initially inactive
        foreach (GameObject obj in objects)
        {
            obj.SetActive(false);
        }

        // Display one object at a time for 30 seconds
        foreach (GameObject obj in objects) 
        {
            obj.SetActive(true);

            yield return new WaitForSeconds(30f); // Wait for 30 seconds

            obj.SetActive(false);
            
        }

        // Activate all objects for 60 seconds
        foreach (GameObject obj in objects)
        {
            obj.SetActive(true);
        }

        yield return new WaitForSeconds(60f);  // Wait for 60 seconds

        // Deactivate all objects at the end of the 60 seconds (optional cleanup)
        foreach (GameObject obj in objects)
        {
            obj.SetActive(false);
        }

        // Debug.Log("All pictures are hidden, and i send the flag."); // this works - the flag is sent
        isComplete = true; // Signal that the behavior is complete
        // CompleteTask();
    }
}
