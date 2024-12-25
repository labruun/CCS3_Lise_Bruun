using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class ArcCreater : MonoBehaviour
{
    public Camera mainCamera;
    public GameObject objectToSpawn;
    public int numberOfObjectsToSpawn = 10;
    public float innerRadius = 2f;
    public float outerRadius = 5f;
    public float sectorAngle = 120f;
    public float objectSpacing = 1.5f; // Minimum distance between objects
    public float averageDistanceFromCamera = 4f; // Average distance from the camera
    public int numberOfSets = 1; // Number of sets of positions to create
    public bool savePositions = false; // Whether to save positions
    public bool spawnObjects = false; // Whether to spawn objects

    private List<List<Vector3>> allSpawnedPositions = new List<List<Vector3>>();
    private List<GameObject> currentSpawnedObjects = new List<GameObject>();
    private int currentSetIndex = 0;
    private float objectRadius; // Radius of the object to spawn

    void Start()
    {
        // Assuming the object is roughly spherical for simplicity
        objectRadius = objectToSpawn.GetComponent<Renderer>().bounds.extents.magnitude;

        for (int i = 0; i < numberOfSets; i++)
        {
            List<Vector3> spawnedPositions = new List<Vector3>();
            SpawnObjectsInAnnularSector(spawnedPositions);
            if (savePositions)
            {
                allSpawnedPositions.Add(spawnedPositions);
            }
        }

        if (allSpawnedPositions.Count > 0)
        {
            if (spawnObjects)
                SpawnObjectsFromSet(currentSetIndex);
        }
    }
    // Hey
    void Update()
    {
        //if (Input.GetKeyDown(KeyCode.RightArrow))
        //{
        //    DespawnCurrentObjects();
        //    currentSetIndex = (currentSetIndex + 1) % allSpawnedPositions.Count;
        //    SpawnObjectsFromSet(currentSetIndex);
        //}

        //if (Input.GetKeyDown(KeyCode.F))
        //{
        //    RegenerateCurrentSet();
        //}

        //if (Input.GetKeyDown(KeyCode.T))
        //{
        //    SaveCurrentSetToFile();
        //}
    }

    void SpawnObjectsInAnnularSector(List<Vector3> spawnedPositions)
    {
        int attempts = 0;
        while (spawnedPositions.Count < numberOfObjectsToSpawn && attempts < numberOfObjectsToSpawn * 100)
        {
            Vector3 spawnPosition = GetRandomPositionInAnnularSector();
            if (IsPositionValid(spawnPosition, spawnedPositions))
            {
                spawnedPositions.Add(spawnPosition);
            }
            attempts++;
        }
    }

    void SpawnObjectsFromSet(int setIndex)
    {
        if (setIndex < 0 || setIndex >= allSpawnedPositions.Count) return;

        List<Vector3> positions = allSpawnedPositions[setIndex];
        foreach (Vector3 position in positions)
        {
            GameObject spawnedObject = Instantiate(objectToSpawn, position, Quaternion.identity);
            currentSpawnedObjects.Add(spawnedObject);
        }
    }

    void DespawnCurrentObjects()
    {
        foreach (GameObject obj in currentSpawnedObjects)
        {
            Destroy(obj);
        }
        currentSpawnedObjects.Clear();
    }

    void RegenerateCurrentSet()
    {
        DespawnCurrentObjects();
        List<Vector3> newPositions = new List<Vector3>();
        SpawnObjectsInAnnularSector(newPositions);
        allSpawnedPositions[currentSetIndex] = newPositions;
        SpawnObjectsFromSet(currentSetIndex);
    }

    void SaveCurrentSetToFile()
    {
        if (currentSetIndex < 0 || currentSetIndex >= allSpawnedPositions.Count) return;

        List<Vector3> positions = allSpawnedPositions[currentSetIndex];
        string fileName = $"Assets/Set_{currentSetIndex}.txt";
        using (StreamWriter writer = new StreamWriter(fileName))
        {
            foreach (Vector3 position in positions)
            {
                writer.WriteLine($"{position.x},{position.y},{position.z}");
            }
        }
        Debug.Log($"Set {currentSetIndex} saved to {fileName}");
    }

    Vector3 GetRandomPositionInAnnularSector()
    {
        float angle = Random.Range(-sectorAngle / 2, sectorAngle / 2);
        float radius = Random.Range(innerRadius, outerRadius);

        // Adjust radius to make the average distance from the camera
        radius += averageDistanceFromCamera - (innerRadius + outerRadius) / 2;

        // Convert angle and radius to Cartesian coordinates
        float angleInRadians = angle * Mathf.Deg2Rad;
        Vector3 direction = new Vector3(Mathf.Sin(angleInRadians), 0, Mathf.Cos(angleInRadians));
        Vector3 position = direction * radius;

        // Get the position in the world space relative to the camera
        Vector3 cameraPosition = mainCamera.transform.position;
        position += new Vector3(cameraPosition.x, 0, cameraPosition.z);

        // Adjust Y position to add some variation
        position.y = Random.Range(-1f, 1f);

        return position;
    }

    bool IsPositionValid(Vector3 position, List<Vector3> spawnedPositions)
    {
        foreach (Vector3 spawnedPosition in spawnedPositions)
        {
            if (Vector3.Distance(position, spawnedPosition) < objectSpacing)
            {
                return false;
            }
        }

        // Check if the whole object is directly visible from the camera
        if (!IsVisible(position, objectToSpawn.transform.GetComponent<Collider>().bounds.size, mainCamera))
        {
            return false;
        }

        return true;
    }

    bool IsVisible(Vector3 pos, Vector3 boundSize, Camera camera)
    {
        var bounds = new Bounds(pos, boundSize);
        var planes = GeometryUtility.CalculateFrustumPlanes(camera);
        return GeometryUtility.TestPlanesAABB(planes, bounds);
    }

    bool IsObjectFullyVisibleFromCamera(Vector3 position)
    {
        Vector3 cameraPosition = mainCamera.transform.position;
        Vector3[] pointsToCheck = new Vector3[]
        {
            position + new Vector3(objectRadius, 0, 0),
            position - new Vector3(objectRadius, 0, 0),
            position + new Vector3(0, objectRadius, 0),
            position - new Vector3(0, objectRadius, 0),
            position + new Vector3(0, 0, objectRadius),
            position - new Vector3(0, 0, objectRadius),
        };

        foreach (Vector3 point in pointsToCheck)
        {
            Ray ray = new Ray(point, cameraPosition - point);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.collider.gameObject != mainCamera.gameObject)
                {
                    return false;
                }
            }
        }

        return true;
    }

    // Optional: Method to get the saved positions if needed
    public List<List<Vector3>> GetSavedPositions()
    {
        return allSpawnedPositions;
    }
}
