using System.Collections.Generic;
using UnityEngine;

namespace DinoAlkkagi.Environment
{
    public class StaticBoardSurface : IBoardSurface
    {
        private const float RayPadding = 10f;
        private const float SpawnHeightOffset = 1.75f;
        private const float SpawnSpacing = 1.6f;

        private readonly List<Collider> boardColliders = new List<Collider>();
        private readonly HashSet<Collider> boardColliderSet = new HashSet<Collider>();
        private readonly Bounds bounds;
        private readonly float fallbackBoardSize;

        public StaticBoardSurface(IEnumerable<Collider> colliders, float fallbackBoardSize)
        {
            this.fallbackBoardSize = Mathf.Max(1f, fallbackBoardSize);

            bool hasBounds = false;
            Bounds combinedBounds = new Bounds(Vector3.zero, Vector3.zero);

            foreach (Collider collider in colliders)
            {
                if (collider == null || collider.isTrigger)
                {
                    continue;
                }

                boardColliders.Add(collider);
                boardColliderSet.Add(collider);

                if (!hasBounds)
                {
                    combinedBounds = collider.bounds;
                    hasBounds = true;
                }
                else
                {
                    combinedBounds.Encapsulate(collider.bounds);
                }
            }

            bounds = hasBounds
                ? combinedBounds
                : new Bounds(Vector3.zero, new Vector3(this.fallbackBoardSize, 1f, this.fallbackBoardSize));
        }

        public float GetHeight(Vector3 xz)
        {
            return TrySampleSurface(xz, out RaycastHit hit) ? hit.point.y : bounds.max.y;
        }

        public Vector3 GetNormal(Vector3 xz)
        {
            return TrySampleSurface(xz, out RaycastHit hit) ? hit.normal : Vector3.up;
        }

        public bool IsInsidePlayableArea(Vector3 xz)
        {
            return TrySampleSurface(xz, out _);
        }

        public IReadOnlyList<Vector3> GetSpawnPoints(int playerId)
        {
            List<Vector3> points = new List<Vector3>();
            float zSign = playerId == 1 ? -1f : 1f;
            float centerZ = bounds.center.z + bounds.extents.z * 0.35f * zSign;
            float maxX = Mathf.Max(SpawnSpacing, bounds.extents.x * 0.35f);
            float rowOffset = SpawnSpacing * 0.5f * zSign;

            for (int i = 0; i < 6; i++)
            {
                int column = i % 3;
                int row = i / 3;
                float xOffset = Mathf.Clamp((column - 1) * SpawnSpacing, -maxX, maxX);
                float zOffset = (row - 0.5f) * rowOffset;
                Vector3 candidate = new Vector3(bounds.center.x + xOffset, 0f, centerZ + zOffset);

                if (!TryCreateSpawnPoint(candidate, out Vector3 spawnPoint))
                {
                    spawnPoint = new Vector3(candidate.x, bounds.max.y + SpawnHeightOffset, candidate.z);
                }

                points.Add(spawnPoint);
            }

            return points;
        }

        public Bounds GetCameraBounds()
        {
            return bounds;
        }

        private bool TryCreateSpawnPoint(Vector3 candidate, out Vector3 spawnPoint)
        {
            if (TrySampleSurface(candidate, out RaycastHit hit))
            {
                spawnPoint = hit.point + Vector3.up * SpawnHeightOffset;
                return true;
            }

            float searchStep = SpawnSpacing * 0.5f;
            for (int ring = 1; ring <= 4; ring++)
            {
                for (int x = -ring; x <= ring; x++)
                {
                    for (int z = -ring; z <= ring; z++)
                    {
                        if (Mathf.Abs(x) != ring && Mathf.Abs(z) != ring)
                        {
                            continue;
                        }

                        Vector3 nearby = candidate + new Vector3(x * searchStep, 0f, z * searchStep);
                        if (TrySampleSurface(nearby, out hit))
                        {
                            spawnPoint = hit.point + Vector3.up * SpawnHeightOffset;
                            return true;
                        }
                    }
                }
            }

            spawnPoint = default;
            return false;
        }

        private bool TrySampleSurface(Vector3 xz, out RaycastHit bestHit)
        {
            bestHit = default;
            if (boardColliders.Count == 0)
            {
                return false;
            }

            Vector3 origin = new Vector3(xz.x, bounds.max.y + RayPadding, xz.z);
            float distance = bounds.size.y + RayPadding * 2f;
            RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);

            bool found = false;
            float bestDistance = float.MaxValue;
            foreach (RaycastHit hit in hits)
            {
                if (hit.collider == null || !boardColliderSet.Contains(hit.collider))
                {
                    continue;
                }

                if (hit.distance < bestDistance)
                {
                    bestDistance = hit.distance;
                    bestHit = hit;
                    found = true;
                }
            }

            return found;
        }
    }
}
