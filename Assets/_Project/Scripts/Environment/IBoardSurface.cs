using System.Collections.Generic;
using UnityEngine;

namespace DinoAlkkagi.Environment
{
    public interface IBoardSurface
    {
        float GetHeight(Vector3 xz);
        Vector3 GetNormal(Vector3 xz);
        bool IsInsidePlayableArea(Vector3 xz);
        IReadOnlyList<Vector3> GetSpawnPoints(int playerId);
        Bounds GetCameraBounds();
        Bounds GetPlayableBounds();
    }
}
