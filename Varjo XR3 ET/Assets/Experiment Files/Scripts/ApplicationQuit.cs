using System.Collections;
using UnityEngine;

public class ApplicationQuit : MonoBehaviour
{
    float timer = 5;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        timer -= Time.deltaTime;
        if(timer < 0)
        {
            UnityEditor.EditorApplication.isPlaying = false;
        }
    }
}
