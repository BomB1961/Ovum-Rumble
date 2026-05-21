using UnityEngine;

namespace DinoAlkkagi.Environment
{
    public static class SlopeClamper
    {
        public static void Clamp(float[,] heightfield, float boardSize, float maxGradient)
        {
            int res = heightfield.GetLength(0);
            float step = boardSize / (res - 1);
            float maxHeightDiff = maxGradient * step;

            for (int iteration = 0; iteration < 3; iteration++)
            {
                for (int z = 0; z < res; z++)
                {
                    for (int x = 0; x < res; x++)
                    {
                        float h = heightfield[x, z];

                        if (x > 0)
                            h = ClampNeighbor(heightfield, x, z, x - 1, z, h, maxHeightDiff);
                        if (x < res - 1)
                            h = ClampNeighbor(heightfield, x, z, x + 1, z, h, maxHeightDiff);
                        if (z > 0)
                            h = ClampNeighbor(heightfield, x, z, x, z - 1, h, maxHeightDiff);
                        if (z < res - 1)
                            h = ClampNeighbor(heightfield, x, z, x, z + 1, h, maxHeightDiff);

                        heightfield[x, z] = h;
                    }
                }
            }
        }

        private static float ClampNeighbor(float[,] hf, int cx, int cz, int nx, int nz, float current, float maxDiff)
        {
            float diff = hf[nx, nz] - current;
            if (diff > maxDiff) return hf[nx, nz] - maxDiff;
            if (diff < -maxDiff) return hf[nx, nz] + maxDiff;
            return current;
        }
    }
}
