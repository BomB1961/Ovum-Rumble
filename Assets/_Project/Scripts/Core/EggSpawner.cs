using System.Collections.Generic;
using UnityEngine;

public class EggSpawner : MonoBehaviour
{
    [SerializeField] private EggController eggPrefab;
    [SerializeField] private Transform player1Root;
    [SerializeField] private Transform player2Root;
    [SerializeField] private int eggsPerPlayer = 6;
    [SerializeField] private float spacing = 1.1f;
    [SerializeField] private Vector3 player1StartCenter = new Vector3(0f, 0.5f, -3f);
    [SerializeField] private Vector3 player2StartCenter = new Vector3(0f, 0.5f, 3f);

    private readonly List<EggController> spawnedEggs = new List<EggController>();

    public IReadOnlyList<EggController> SpawnedEggs => spawnedEggs;

    public void SpawnAll()
    {
        if (eggPrefab == null)
        {
            Debug.LogError($"{nameof(EggSpawner)} requires an egg prefab.", this);
            return;
        }

        ClearSpawnedEggs();
        SpawnPlayerEggs(1, player1Root, player1StartCenter);
        SpawnPlayerEggs(2, player2Root, player2StartCenter);
    }

    public void ClearSpawnedEggs()
    {
        for (int i = spawnedEggs.Count - 1; i >= 0; i--)
        {
            EggController egg = spawnedEggs[i];
            if (egg == null)
            {
                continue;
            }

            if (Application.isPlaying)
            {
                Destroy(egg.gameObject);
            }
            else
            {
                DestroyImmediate(egg.gameObject);
            }
        }

        spawnedEggs.Clear();
    }

    private void SpawnPlayerEggs(int ownerPlayerId, Transform parent, Vector3 center)
    {
        for (int i = 0; i < eggsPerPlayer; i++)
        {
            Vector3 position = GetGridPosition(center, i);
            EggController egg = Instantiate(eggPrefab, position, Quaternion.identity, parent);
            egg.Initialize(ownerPlayerId);
            spawnedEggs.Add(egg);
        }
    }

    private Vector3 GetGridPosition(Vector3 center, int index)
    {
        int column = index % 3;
        int row = index / 3;

        float xOffset = (column - 1) * spacing;
        float zOffset = (row - 0.5f) * spacing;

        return center + new Vector3(xOffset, 0f, zOffset);
    }
}
