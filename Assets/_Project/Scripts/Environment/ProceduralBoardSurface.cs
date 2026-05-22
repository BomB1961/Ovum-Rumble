using System.Collections.Generic;
using UnityEngine;

namespace DinoAlkkagi.Environment
{
    public class ProceduralBoardSurface : IBoardSurface
    {
        private readonly float[,] heightfield;
        private readonly float boardSize;
        private readonly int resolution;
        private readonly float cellSize;
        private readonly float halfSize;

        public ProceduralBoardSurface(float[,] heightfield, float boardSize)
        {
            this.heightfield = heightfield;
            this.boardSize = boardSize;
            this.resolution = heightfield.GetLength(0);
            this.cellSize = boardSize / (resolution - 1);
            this.halfSize = boardSize * 0.5f;
        }

        public float GetHeight(Vector3 xz)
        {
            float fx = (xz.x + halfSize) / cellSize;
            float fz = (xz.z + halfSize) / cellSize;

            int x0 = Mathf.Clamp(Mathf.FloorToInt(fx), 0, resolution - 2);
            int z0 = Mathf.Clamp(Mathf.FloorToInt(fz), 0, resolution - 2);
            int x1 = x0 + 1;
            int z1 = z0 + 1;

            float tx = Mathf.Clamp01(fx - x0);
            float tz = Mathf.Clamp01(fz - z0);

            float h00 = heightfield[x0, z0];
            float h10 = heightfield[x1, z0];
            float h01 = heightfield[x0, z1];
            float h11 = heightfield[x1, z1];

            float h0 = Mathf.Lerp(h00, h10, tx);
            float h1 = Mathf.Lerp(h01, h11, tx);
            return Mathf.Lerp(h0, h1, tz);
        }

        public Vector3 GetNormal(Vector3 xz)
        {
            float h = GetHeight(xz);
            float hx = GetHeight(xz + Vector3.right * cellSize) - h;
            float hz = GetHeight(xz + Vector3.forward * cellSize) - h;
            Vector3 normal = new Vector3(-hx, cellSize, -hz);
            return normal.normalized;
        }

        public bool IsInsidePlayableArea(Vector3 xz)
        {
            float dx = xz.x / (boardSize * 0.5f);
            float dz = xz.z / (boardSize * 0.5f);
            return (dx * dx + dz * dz) <= 1f;
        }

        public IReadOnlyList<Vector3> GetSpawnPoints(int playerId)
        {
            Vector3 center = playerId == 1
                ? new Vector3(0f, 0f, -3f)
                : new Vector3(0f, 0f, 3f);

            List<Vector3> points = new List<Vector3>();
            const float spacing = 1.6f;
            const float spawnHeightOffset = 0.75f;

            for (int i = 0; i < 6; i++)
            {
                int col = i % 3;
                int row = i / 3;
                float xOffset = (col - 1) * spacing;
                float zOffset = (row - 0.5f) * spacing;
                Vector3 pos = center + new Vector3(xOffset, 0f, zOffset);
                pos.y = GetHeight(pos) + spawnHeightOffset;
                points.Add(pos);
            }

            return points;
        }

        public Bounds GetCameraBounds()
        {
            float maxH = float.MinValue;
            float minH = float.MaxValue;

            for (int z = 0; z < resolution; z++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    float h = heightfield[x, z];
                    if (h > maxH) maxH = h;
                    if (h < minH) minH = h;
                }
            }

            Vector3 center = new Vector3(0f, (maxH + minH) * 0.5f, 0f);
            Vector3 size = new Vector3(boardSize, maxH - minH, boardSize);
            return new Bounds(center, size);
        }
    }
}
