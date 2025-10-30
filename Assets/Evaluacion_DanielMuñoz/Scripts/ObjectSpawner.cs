using System.Collections.Generic;
using UnityEngine;

public class ObjectSpawner : MonoBehaviour
{
    public GameObject[] collectiblePrefabs; // Prefabs de moneda, joya, gema
    public List<Transform> spawnPositions; // 50 posiciones predefinidas

    void Start()
    {
        SpawnObjects();
    }

    void SpawnObjects()
    {
        List<Transform> selectedPositions = new List<Transform>(spawnPositions);
        for (int i = 0; i < 15; i++)
        {
            int randomIndex = Random.Range(0, selectedPositions.Count);
            Transform spawnPosition = selectedPositions[randomIndex];
            selectedPositions.RemoveAt(randomIndex);

            GameObject collectible = collectiblePrefabs[Random.Range(0, collectiblePrefabs.Length)];
            Instantiate(collectible, spawnPosition.position, Quaternion.identity);
        }
    }
}
