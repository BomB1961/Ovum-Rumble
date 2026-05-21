using UnityEngine;

namespace DinoAlkkagi.Environment
{
    public static class MapValidator
    {
        public static bool Validate(float[,] heightfield, float boardSize, float spawnRadius, float maxSlopeGradient)
        {
            int res = heightfield.GetLength(0);
            float step = boardSize / (res - 1);
            float halfSize = boardSize * 0.5f;

            if (!CheckSpawnFlatness(heightfield, res, step, halfSize, new Vector3(0f, 0f, -3f), spawnRadius))
            {
                Debug.Log("[MapValidator] P1 spawn zone too uneven.");
                return false;
            }

            if (!CheckSpawnFlatness(heightfield, res, step, halfSize, new Vector3(0f, 0f, 3f), spawnRadius))
            {
                Debug.Log("[MapValidator] P2 spawn zone too uneven.");
                return false;
            }

            return true;
        }

        private static bool CheckSpawnFlatness(float[,] heightfield, int res, float step, float halfSize, Vector3 center, float radius)
        {
            float minH = float.MaxValue;
            float maxH = float.MinValue;

            for (int z = 0; z < res; z++)
            {
                for (int x = 0; x < res; x++)
                {
                    float worldX = x * step - halfSize;
                    float worldZ = z * step - halfSize;
                    float dist = Mathf.Sqrt((worldX - center.x) * (worldX - center.x) + (worldZ - center.z) * (worldZ - center.z));

                    if (dist <= radius)
                    {
                        float h = heightfield[x, z];
                        if (h < minH) minH = h;
                        if (h > maxH) maxH = h;
                    }
                }
            }

            return (maxH - minH) <= 0.05f;
        }
    }
}
