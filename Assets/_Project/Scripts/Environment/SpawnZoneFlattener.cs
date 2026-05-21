using UnityEngine;

namespace DinoAlkkagi.Environment
{
    public static class SpawnZoneFlattener
    {
        private static readonly Vector3 Player1Center = new Vector3(0f, 0f, -3f);
        private static readonly Vector3 Player2Center = new Vector3(0f, 0f, 3f);

        public static void Flatten(float[,] heightfield, float boardSize, float radius)
        {
            int res = heightfield.GetLength(0);
            float step = boardSize / (res - 1);
            float halfSize = boardSize * 0.5f;

            FlattenZone(heightfield, res, step, halfSize, Player1Center, radius);
            FlattenZone(heightfield, res, step, halfSize, Player2Center, radius);
        }

        private static void FlattenZone(float[,] heightfield, int res, float step, float halfSize, Vector3 center, float radius)
        {
            float sum = 0f;
            int count = 0;

            for (int z = 0; z < res; z++)
            {
                for (int x = 0; x < res; x++)
                {
                    float worldX = x * step - halfSize;
                    float worldZ = z * step - halfSize;
                    float dist = Mathf.Sqrt((worldX - center.x) * (worldX - center.x) + (worldZ - center.z) * (worldZ - center.z));

                    if (dist <= radius)
                    {
                        sum += heightfield[x, z];
                        count++;
                    }
                }
            }

            if (count == 0) return;
            float avgHeight = sum / count;

            for (int z = 0; z < res; z++)
            {
                for (int x = 0; x < res; x++)
                {
                    float worldX = x * step - halfSize;
                    float worldZ = z * step - halfSize;
                    float dist = Mathf.Sqrt((worldX - center.x) * (worldX - center.x) + (worldZ - center.z) * (worldZ - center.z));

                    if (dist <= radius)
                    {
                        float t = Mathf.SmoothStep(1f, 0f, dist / radius);
                        heightfield[x, z] = Mathf.Lerp(heightfield[x, z], avgHeight, t);
                    }
                }
            }
        }
    }
}
