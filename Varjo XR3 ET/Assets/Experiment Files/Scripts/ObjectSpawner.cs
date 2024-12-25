using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectSpawner : MonoBehaviour
{
    public Transform prefabToSpawn;
    public int objectCount = 50;
    public float spawnRadius = 5;
    public float spawnCollisionCheckRadius;

    void Start()
    {
        for (int loop=0; loop < objectCount; loop++)
        {
            Vector3 spawnPoint = Random.insideUnitSphere * spawnRadius;
            //Vector3 spawnPoint = RandomPointInBounds(GetComponent<Collider>().bounds);
            if(!Physics.CheckSphere(spawnPoint, spawnCollisionCheckRadius))
            {
                Instantiate(prefabToSpawn, spawnPoint, Random.rotation);
            }
        }
        
    }


    public static Vector3 RandomPointInBounds(Bounds bounds)
    {
        return new Vector3(
            Random.Range(bounds.min.x, bounds.max.x),
            Random.Range(bounds.min.y, bounds.max.y),
            Random.Range(bounds.min.z, bounds.max.z)
        );
    }
}
