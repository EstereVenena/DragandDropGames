using UnityEngine;

public class ObsticlesSpawnScript : MonoBehaviour
{
    public GameObject[] cloudsPrefabs;
    public GameObject[] obstaclesPrefabs;
    public Transform spawnPoint;
    public float cloudSpawnInterval = 3f;
    public float obstacleSpawnInterval = 3f;

    public float minY = -540f;
    public float maxY = 540f;

    public float cloudMinSpeed = 1.5f;
    public float cloudMaxSpeed = 150f;

    public float obstaclesMinSpeed = 2f;
    public float obstaclesMaxSpeed = 200f;



    void Start()
    {
        InvokeRepeating(nameof(SpawnCloud), 0f, cloudSpawnInterval);
        InvokeRepeating(nameof(SpawnObsticles), 0f, obstacleSpawnInterval);
    }

    void SpawnCloud()
    {
        if (cloudsPrefabs.Length == 0)
        {
            return;

            GameObject cloudPrefab = cloudsPrefabs[Random.Range(0, cloudsPrefabs.Length)];
            float y = Random.Range(minY, maxY);
            Vector3 spawnPosition = new Vector3 (spawnPoint.position.x, y, spawnPoint.position.z);
            GameObject cloud = Instantiate(cloudPrefab, spawnPosition, Quaternion.identity, spawnPoint);
            float movmentSpeed = Random.Range(cloudMinSpeed, cloudMaxSpeed);
            ObstaclesControlerScript controler = cloud.GetComponent<ObstaclesControlerScript>();
            controler.speed = movmentSpeed;
        }
    }

    void SpawnObsticles()
    {
        if (cloudsPrefabs.Length == 0)
        {
            return;

            GameObject ObsticlesPrefab = obstaclesPrefabs[Random.Range(0, obstaclesPrefabs.Length)];
            float y = Random.Range(minY, maxY);
            Vector3 spawnPosition = new Vector3 (-spawnPoint.position.x, y, spawnPoint.position.z);
            GameObject obstacle = Instantiate(obstaclesPrefab, spawnPosition, Quaternion.identity, spawnPoint);
            float movmentSpeed = Random.Range(obstaclesMinSpeed, obstaclesMaxSpeed);
            ObstaclesControlerScript controler = obstacle.GetComponent<ObstaclesControlerScript>();
            controler.speed = -movmentSpeed;
        }
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
