using System.Collections.Generic;
using UnityEngine;
using DinoAlkkagi.Environment;

public class EggSpawner : MonoBehaviour
{
    [SerializeField] private EggController player1EggPrefab;
    [SerializeField] private EggController player2EggPrefab;
    [SerializeField] private Transform player1Root;
    [SerializeField] private Transform player2Root;
    [SerializeField] private int eggsPerPlayer = 6;
    [SerializeField] private float spacing = 1.6f;
    [SerializeField] private Vector3 player1StartCenter = new Vector3(0f, 0.75f, -3f);
    [SerializeField] private Vector3 player2StartCenter = new Vector3(0f, 0.75f, 3f);
    [SerializeField] private bool spawnOnStart;

    private readonly List<EggController> spawnedEggs = new List<EggController>();
    private IBoardSurface boardSurface;

    public IReadOnlyList<EggController> SpawnedEggs => spawnedEggs;

    public void SetBoardSurface(IBoardSurface surface)
    {
        boardSurface = surface;
    }

    private void Start()
    {
        if (spawnOnStart)
        {
            SpawnAll();
        }
    }

    public void SpawnAll()
    {
        if (player1EggPrefab == null || player2EggPrefab == null)
        {
            Debug.LogError($"{nameof(EggSpawner)} requires both player egg prefabs.", this);
            return;
        }

        ClearSpawnedEggs();
        int eggIdCounter = 0;

        if (boardSurface != null)
        {
            eggIdCounter = SpawnFromBoardSurface(1, player1Root, eggIdCounter);
            SpawnFromBoardSurface(2, player2Root, eggIdCounter);
            return;
        }

        eggIdCounter = SpawnPlayerEggs(1, player1Root, player1StartCenter, eggIdCounter);
        SpawnPlayerEggs(2, player2Root, player2StartCenter, eggIdCounter);
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

    private EggController GetPrefabForPlayer(int playerId)
    {
        return playerId == 2 ? player2EggPrefab : player1EggPrefab;
    }

    private int SpawnPlayerEggs(int ownerPlayerId, Transform parent, Vector3 center, int startId)
    {
        EggController prefab = GetPrefabForPlayer(ownerPlayerId);
        int currentId = startId;
        for (int i = 0; i < eggsPerPlayer; i++)
        {
            Vector3 position = GetGridPosition(center, i);
            position.y += 1.0f;

            EggController egg = Instantiate(prefab, position, Quaternion.identity, parent);
            egg.Initialize(ownerPlayerId);
            egg.SetNetworkEggId(currentId++);

            Rigidbody rb = egg.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = false;
                rb.useGravity = true;
                rb.WakeUp();
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            }

            spawnedEggs.Add(egg);
        }
        return currentId;
    }

    private int SpawnFromBoardSurface(int ownerPlayerId, Transform parent, int startId)
    {
        IReadOnlyList<Vector3> points = boardSurface.GetSpawnPoints(ownerPlayerId);
        EggController prefab = GetPrefabForPlayer(ownerPlayerId);
        int currentId = startId;
        for (int i = 0; i < eggsPerPlayer && i < points.Count; i++)
        {
            Vector3 spawnPos = points[i];

            EggController egg = Instantiate(prefab, spawnPos, Quaternion.identity, parent);
            egg.Initialize(ownerPlayerId);
            egg.SetNetworkEggId(currentId++);

            Rigidbody rb = egg.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = false;
                rb.useGravity = true;
                rb.WakeUp();
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            }

            spawnedEggs.Add(egg);
        }
        return currentId;
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
