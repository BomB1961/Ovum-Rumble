using UnityEngine;

namespace DinoAlkkagi.Environment
{
    public static class MeshFromHeightfield
    {
        public static Mesh Create(float[,] heightfield, float boardSize, float thickness = 1f)
        {
            int res = heightfield.GetLength(0);
            float halfSize = boardSize * 0.5f;
            float step = boardSize / (res - 1);

            float minH = float.MaxValue;
            for (int z = 0; z < res; z++)
                for (int x = 0; x < res; x++)
                    if (heightfield[x, z] < minH) minH = heightfield[x, z];

            float bottomY = minH - thickness;

            int topCount = res * res;
            int sideVertCount = res * 4;
            int bottomVertCount = 4;
            int totalVerts = topCount + sideVertCount + bottomVertCount;

            int topTriCount = (res - 1) * (res - 1) * 6;
            int sideTriCount = (res - 1) * 4 * 6;
            int bottomTriCount = 6;
            int totalTris = topTriCount + sideTriCount + bottomTriCount;

            Vector3[] vertices = new Vector3[totalVerts];
            Vector2[] uv = new Vector2[totalVerts];
            int[] triangles = new int[totalTris];

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

            int sideBase = topCount;
            int frontBase = sideBase;
            int backBase = sideBase + res;
            int leftBase = sideBase + res * 2;
            int rightBase = sideBase + res * 3;

            for (int i = 0; i < res; i++)
            {
                float x = i * step - halfSize;
                float z = i * step - halfSize;

                vertices[frontBase + i] = new Vector3(x, bottomY, -halfSize);
                uv[frontBase + i] = new Vector2((float)i / (res - 1), 0f);

                vertices[backBase + i] = new Vector3(x, bottomY, halfSize);
                uv[backBase + i] = new Vector2((float)i / (res - 1), 0f);

                vertices[leftBase + i] = new Vector3(-halfSize, bottomY, z);
                uv[leftBase + i] = new Vector2((float)i / (res - 1), 0f);

                vertices[rightBase + i] = new Vector3(halfSize, bottomY, z);
                uv[rightBase + i] = new Vector2((float)i / (res - 1), 0f);
            }

            triIdx = AddEdgeTriangles(triangles, triIdx, res, frontBase, 0, true, false);
            triIdx = AddEdgeTriangles(triangles, triIdx, res, backBase, (res - 1) * res, false, false);
            triIdx = AddEdgeTriangles(triangles, triIdx, res, leftBase, 0, true, true);
            triIdx = AddEdgeTriangles(triangles, triIdx, res, rightBase, res - 1, false, true);

            int b0 = totalVerts - 4;
            int b1 = b0 + 1;
            int b2 = b0 + 2;
            int b3 = b0 + 3;

            vertices[b0] = new Vector3(-halfSize, bottomY, -halfSize);
            vertices[b1] = new Vector3(halfSize, bottomY, -halfSize);
            vertices[b2] = new Vector3(-halfSize, bottomY, halfSize);
            vertices[b3] = new Vector3(halfSize, bottomY, halfSize);

            uv[b0] = new Vector2(0f, 0f);
            uv[b1] = new Vector2(1f, 0f);
            uv[b2] = new Vector2(0f, 1f);
            uv[b3] = new Vector2(1f, 1f);

            triangles[triIdx++] = b0;
            triangles[triIdx++] = b2;
            triangles[triIdx++] = b1;

            triangles[triIdx++] = b1;
            triangles[triIdx++] = b2;
            triangles[triIdx++] = b3;

            Mesh mesh = new Mesh();
            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mesh.name = "ProceduralBoard";

            return mesh;
        }

        private static int AddEdgeTriangles(int[] triangles, int triIdx, int res, int bottomStart, int topStart, bool forwardX, bool isVerticalEdge)
        {
            for (int i = 0; i < res - 1; i++)
            {
                int topA, topB, botA, botB;

                if (isVerticalEdge)
                {
                    int row = i;
                    int nextRow = i + 1;
                    topA = topStart + row * res;
                    topB = topStart + nextRow * res;
                }
                else
                {
                    topA = topStart + i;
                    topB = topStart + i + 1;
                }

                botA = bottomStart + i;
                botB = bottomStart + i + 1;

                if (forwardX)
                {
                    triangles[triIdx++] = topA;
                    triangles[triIdx++] = botA;
                    triangles[triIdx++] = topB;

                    triangles[triIdx++] = topB;
                    triangles[triIdx++] = botA;
                    triangles[triIdx++] = botB;
                }
                else
                {
                    triangles[triIdx++] = topA;
                    triangles[triIdx++] = topB;
                    triangles[triIdx++] = botA;

                    triangles[triIdx++] = topB;
                    triangles[triIdx++] = botB;
                    triangles[triIdx++] = botA;
                }
            }

            return triIdx;
        }
    }
}
