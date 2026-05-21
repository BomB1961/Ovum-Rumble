using UnityEngine;

namespace DinoAlkkagi.Environment
{
    public static class HeightfieldGenerator
    {
        public static float[,] Generate(int resolution, float noiseScale, float heightMultiplier, int octaves, int seed)
        {
            float[,] heightfield = new float[resolution, resolution];
            float halfRes = resolution * 0.5f;

            float frequency = 1f;
            float amplitude = 1f;
            float maxAmplitude = 0f;

            for (int o = 0; o < octaves; o++)
            {
                for (int z = 0; z < resolution; z++)
                {
                    for (int x = 0; x < resolution; x++)
                    {
                        float sampleX = (x - halfRes + seed * 17.3f) / resolution * noiseScale * frequency;
                        float sampleZ = (z - halfRes + seed * 31.7f) / resolution * noiseScale * frequency;

                        heightfield[x, z] += Mathf.PerlinNoise(sampleX, sampleZ) * amplitude;
                    }
                }

                maxAmplitude += amplitude;
                frequency *= 2f;
                amplitude *= 0.5f;
            }

            for (int z = 0; z < resolution; z++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    heightfield[x, z] = (heightfield[x, z] / maxAmplitude - 0.5f) * heightMultiplier;
                }
            }

            return heightfield;
        }
    }
}
