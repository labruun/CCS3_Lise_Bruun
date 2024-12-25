using UnityEngine;
using System.Collections;

public class HighlightOnGaze : MonoBehaviour
{ 
    MeshRenderer mr;

    void Start()
    {
        mr = GetComponent<MeshRenderer>();
    }

    void Update()
    {
        
    }

    // Rotates object hit with gaze tracking raycast
    public void RayHit()
    {
        if(mr.material.color == Color.cyan)
        {
            mr.material.color = Color.magenta;
            StartCoroutine(Wait());
        }
        else{
            mr.material.color = Color.cyan;
            StartCoroutine(Wait());
        }
            
    }

    IEnumerator Wait()
    {
        yield return new WaitForSecondsRealtime(10);
    }

}
