using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.WSA;
using static StudyController;

public class AudioManager : MonoBehaviour
{
    [Header("_________________________________________________________________________________________________________")]
    [Header("Set audio source.")]
    public AudioSource audioSource;         // Reference to the AudioSource component

    [Header("_________________________________________________________________________________________________________")]
    [Header("Set a folder path for each category.")]
    public string[] folderPaths;           // Array of folder paths to load audio clips from

    private AudioClip[] audioClips;         // Array of audio clips

    /*
    private AudioClip[] clips_humanoid_true;
    private AudioClip[] clips_humanoid_false;

    private AudioClip[] clips_things_true;
    private AudioClip[] clips_things_false;


    private AudioClip[] clips_vehicles_true;
    private AudioClip[] clips_vehicles_false;

    private AudioClip[] clips_animals_true;
    private AudioClip[] clips_animals_false;
    */

    List<AudioClip> clips_humanoid_true = new List<AudioClip>();
    List<AudioClip> clips_humanoid_false = new List<AudioClip>();
    List<AudioClip> clips_things_true = new List<AudioClip>();
    List<AudioClip> clips_things_false = new List<AudioClip>();
    List<AudioClip> clips_vehicles_true = new List<AudioClip>();
    List<AudioClip> clips_vehicles_false = new List<AudioClip>();
    List<AudioClip> clips_animals_true = new List<AudioClip>();
    List<AudioClip> clips_animals_false = new List<AudioClip>();

    private int statements_per_category = 12;



    private List<int> randOrder = new List<int>();
    private List<bool> correctness = new List<bool>();

    private bool isAnswered = false;        // Tracks if the participant has answered
    private bool participantAnswer;         // Tracks the participant's answer


    private bool inputEvent = false;        // Tracks if a key was pressed

    //private List<int> index_true = new List<int>() { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };

    //private List<int> index_false = new List<int>() { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };

    public CATEGORIES[] categoryTypes = new CATEGORIES[4];
    private int currentCategory = 0;
    private int currentIndex = 0;           // Index of the current audio clip

    private bool audioPlayed = false;       // Tracks if the audio was played

    private DateTime startTime;

    public string[] logThis = new string[4];

    public enum CATEGORIES
    {
        HUMANOIDS,
        THINGS,
        VEHICLES,
        ANIMALS
    }

    public bool isComplete = false; // Flag to signal completion
    

    public bool GetIsComplete()
    {
        Debug.Log("GetIsComplete() called in AudioManager");
        return isComplete;
    }

    public void ResetIsComplete()
    {
        Debug.Log("ResetIsComplete() called in AudioManager");
        isComplete = false;

        
    }

    public string[] updateLoggingData()
    {
        
        if (currentCategory >= categoryTypes.Length)
        {
            string[] msg = { "None", "None", "None", "None" };
            return msg;
        }
        else
        {
            string[] msg = new string[4];
            msg[0] = categoryTypes[currentCategory].ToString();      // category
            
            // statement ID (it logged the next statement id before i did this)
            msg[1] = (randOrder[currentIndex]).ToString();                     
            
            
            msg[2] = correctness[currentIndex].ToString();           // correctness

            // answer
            if (isAnswered)
            {

                msg[3] = participantAnswer.ToString(); 
            }
            else
            {
                msg[3] = "None";
            }
            
            return msg;
        }

        

    }



    void Start()
    {

        // audioClips = Resources.LoadAll<AudioClip>("AudioClips");

        //Debug.Log("categoryTypes: " + categoryTypes.ToString()); 

        // assign audio clips
        int c = 0;
        foreach (string path in folderPaths)
        {
            string path_true = path + "/true";
            string path_false = path + "/false";

            AudioClip[] clips_true = Resources.LoadAll<AudioClip>(path_true);
            AudioClip[] clips_false = Resources.LoadAll<AudioClip>(path_false);

            Debug.Log("Length of true clip list: " + clips_true.Length);   // returns 12

            switch (c)
            {
                case 0:                         
                    clips_humanoid_true.AddRange(clips_true);
                    clips_humanoid_false.AddRange(clips_false);
                    break;
                case 1:
                    clips_things_true.AddRange(clips_true);
                    clips_things_false.AddRange(clips_false);
                    break;
                case 2:
                    clips_vehicles_true.AddRange(clips_true);
                    clips_vehicles_false.AddRange(clips_false);
                    break;
                case 3:
                    clips_animals_true.AddRange(clips_true);
                    clips_animals_false.AddRange(clips_false);
                    break;
            }
            c++;
        }
        
        // make list of indices in random order
        // + list of bools to indicate if the statement should be taken from the "true list" or "false list"
        for (int i = 0; i < statements_per_category; i++)
        {
            randOrder.Add(i);

            int half = statements_per_category / 2;
            if (i < half)
            {
                correctness.Add(true);
                correctness.Add(false);
            }
        }
        
        //Debug.Log("Correctness: " + correctness.Count);   // returns 12

        // Shuffle list order
        for (int i = randOrder.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1); // Random index between 0 and i
            // Swap elements
            int temp = randOrder[i];
            randOrder[i] = randOrder[j];
            randOrder[j] = temp;
        }

        // shuffle correctness
        for (int i = correctness.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1); // Random index between 0 and i
            // Swap elements
            bool temp = correctness[i];
            correctness[i] = correctness[j];
            correctness[j] = temp;
        }



    }


    void Update()
    {
        isAnswered = false;

        if (currentCategory >= 4)   // it has gone through all categories, and ends on 3+1 = 4
        {
            
            isComplete = true;
            return;
        }



        // Check for UP or DOWN key input
        if (Input.GetKeyUp(KeyCode.UpArrow) || Input.GetKeyUp(KeyCode.DownArrow))
        {
            inputEvent = true;

            if (Input.GetKeyUp(KeyCode.UpArrow))
            {
                //Debug.Log("UP key pressed!");
                // Handle UP key action here
                participantAnswer = true;
            }

            if (Input.GetKeyUp(KeyCode.DownArrow))
            {
                //Debug.Log("DOWN key pressed!");
                // Handle DOWN key action here
                participantAnswer = false;
            }
            isAnswered = true;
            // Optional: Stop the current audio or handle custom behavior
        }

        if (!audioPlayed) 
        {
            // save timestamp here
            startTime = DateTime.Now;   // log this

            //Debug.Log("Current index: " + currentIndex);

            switch (currentCategory)
            {
                case 0:         // Humanoids
                    // play audio clip
                    if (correctness[currentIndex]) 
                    { 
                        //Debug.Log("Length of true clip list: " + clips_humanoid_true.Length);
                        PlayCurrentClip(clips_humanoid_true);
                    }
                    else
                    {
                        //Debug.Log("Length of false clip list: " + clips_humanoid_false.Length);
                        PlayCurrentClip(clips_humanoid_false);
                    }

                    break;
                case 1:         // Things
                    if (correctness[currentIndex])
                    {
                        PlayCurrentClip(clips_things_true);
                    }
                    else
                    {
                        PlayCurrentClip(clips_things_false);
                    }
                    break;
                case 2:         // Vehicles 
                    if (correctness[currentIndex])
                    {
                        PlayCurrentClip(clips_vehicles_true);
                    }
                    else
                    {
                        PlayCurrentClip(clips_vehicles_false);
                    }
                    break;
                case 3:         // Animals
                    if (correctness[currentIndex])
                    {
                        PlayCurrentClip(clips_animals_true);
                    }
                    else
                    {
                        PlayCurrentClip(clips_animals_false);
                    }
                    break;     
            }
        }

        DateTime currentTime = DateTime.Now;
        TimeSpan timeDifference = currentTime - startTime;

        logThis = updateLoggingData();

        if (inputEvent || (timeDifference.TotalSeconds>8))
        { 
            currentIndex++;
            DateTime endTime = DateTime.Now;   // log this

            audioPlayed = false;
            

            if (currentIndex >= statements_per_category)
            {
                currentIndex = 0;
                currentCategory++;
                //Debug.Log("Current category incremented to: " + currentCategory);
                
            }
        }

        inputEvent = false;
    }


    // Play the current audio clip + handles switch to next category
    private void PlayCurrentClip(List<AudioClip> clips)  //AudioClip[] clips)
    {
        if (currentIndex < clips.Count)   //.Length)
        {
            audioSource.clip = clips[randOrder[currentIndex]];
            audioSource.Play();

            //StartCoroutine(WaitAndPlayNext(8f)); // Start the timer to play the next clip
        }
        else
        {
            Debug.Log("All clips in category " + currentCategory  + " has played.");
        }

        audioPlayed = true;

    }











    private IEnumerator WaitAndPlayNext(float waitTime)
    {
        float elapsedTime = 0f;

        while (elapsedTime < waitTime)
        {
            if (inputEvent) // Exit if a key was pressed
            {
                inputEvent = false; // Reset for the next clip
                yield break;
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Play the next clip if no key was pressed
        currentIndex++;
        if (currentIndex < audioClips.Length)
        {
            //PlayCurrentClip(audioClips);
        }
        else
        {
            Debug.Log("All clips completed.");
        }
    }

   


    private List<AudioClip> SelectRandomClips(List<AudioClip> clips, int count)
    {
        List<AudioClip> randomClips = new List<AudioClip>();
        if (clips.Count == 0) return randomClips;

        // Shuffle the list to randomize the order
        List<AudioClip> shuffledClips = new List<AudioClip>(clips);
        for (int i = 0; i < shuffledClips.Count; i++)
        {
            int randomIndex = UnityEngine.Random.Range(i, shuffledClips.Count);
            AudioClip temp = shuffledClips[i];
            shuffledClips[i] = shuffledClips[randomIndex];
            shuffledClips[randomIndex] = temp;
        }

        // Take the first `count` clips
        for (int i = 0; i < Mathf.Min(count, shuffledClips.Count); i++)
        {
            randomClips.Add(shuffledClips[i]);
        }

        return randomClips;
    }

}
