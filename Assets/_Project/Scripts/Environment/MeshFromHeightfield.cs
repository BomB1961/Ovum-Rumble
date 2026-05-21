using UnityEngine;

namespace DinoAlkkagi.Environment
{
    public static class MeshFromHeightfield
    {
        public static Mesh Create(float[,] heightfield, float boardSize)
        {
            int res = heightfield.GetLength(0);
            int vertexCount = res * res;
            int triangleCount = (res - 1) * (res - 1) * 6;

            Vector3[] vertices = new Vector3[vertexCount];
            Vector2[] uv = new Vector2[vertexCount];
            int[] triangles = new int[triangleCount];

            float halfSize = boardSize * 0.5f;
            float step = boardSize / (res - 1);

            for (int z = 0; z < res; z++)
            {
                for (int x = 0; x < res; x++)
                {
                    int idx = z * res + x;
                    vertices[idx] = new Vector3(
                        x * step - halfSize,
                        heightfield[x, z],
                        z * step - halfSize
                    );
                    uv[idx] = new Vector2((float)x / (res - 1), (float)z / (res - 1));
                }
            }

            int triIdx = 0;
            for (int z = 0; z < res - 1; z++)
            {
                for (int x = 0; x < res - 1; x++)
                {
                    int bl = z * res + x;
                    int br = bl + 1;
                    int tl = bl + res;
                    int tr = tl + 1;

                    triangles[triIdx++] = bl;
                    triangles[triIdx++] = tl;
                    triangles[triIdx++] = br;

                    triangles[triIdx++] = br;
                    triangles[triIdx++] = tl;
                    triangles[triIdx++] = tr;
                }
            }

            Mesh mesh = new Mesh();
            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mesh.name = "ProceduralBoard";

            return mesh;
        }
    }
}
