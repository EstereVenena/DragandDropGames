using UnityEngine;

public class FlyingObjectSpawnScript : MonoBehaviour
{
    ScreenBoundriesScript screenBoundriesScript;
    public GameObject[] cludsPrefabs;
    public GameObject[] objectPrefabs;
    public Transform spawnPoint;

    public float cloudSpawnInterval = 2f;
    public float objectSpawnInterval = 3f;
    private float minY, maxY;
    public float cloudMinSpeed = 1.5f;
    public float cloudMaxSpeed = 150f;
    public float objectMinSpeed = 2f;
    public float objectMaxSpeed = 200f;



    void Start()
    {
        screenBoundriesScript = FindFirstObjectByType<ScreenBoundriesScript>();
        minY = screenBoundriesScript.minY;
        maxY = screenBoundriesScript.maxY;
        InvokeRepeating(nameof(SpawnCloud), 0f, cloudSpawnInterval);
        InvokeRepeating(nameof(SpawnObject), 0f, objectSpawnInterval);
    }

    void SpawnCloud()
    {
        if (cludsPrefabs.Length == 0)
            return;

        GameObject cloudPrefab = cludsPrefabs[Random.Range(0, cludsPrefabs.Length)];
        float y = Random.Range(minY, maxY);
        Vector3 spawnPosition = new Vector3(spawnPoint.position.x, y, spawnPoint.position.z);
        GameObject cloud =
            Instantiate(cloudPrefab, spawnPosition, Quaternion.identity, spawnPoint);
        float movementSpeed = Random.Range(cloudMinSpeed, cloudMaxSpeed);
        FlyingObjectsControllerScript controller =
            cloud.GetComponent<FlyingObjectsControllerScript>();
        controller.speed = movementSpeed;

    }

    void SpawnObject()
    {
        if (objectPrefabs.Length == 0)
            return;

        GameObject objectPrefab = objectPrefabs[Random.Range(0, objectPrefabs.Length)];
        float y = Random.Range(minY, maxY);

        Vector3 spawnPosition = new(-spawnPoint.position.x, y, spawnPoint.position.z);

        GameObject flyingObject =
            Instantiate(objectPrefab, spawnPosition, Quaternion.identity, spawnPoint);
        float movementSpeed = Random.Range(objectMinSpeed, objectMaxSpeed);
        FlyingObjectsControllerScript controller =
            flyingObject.GetComponent<FlyingObjectsControllerScript>();
        controller.speed = -movementSpeed;
    }
}