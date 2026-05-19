using UnityEngine;
using DinoAlkkagi.Core;

namespace DinoAlkkagi.Rules
{
    /// <summary>
    /// Person B 전용 — 보드 아래/외곽의 Trigger 영역.
    /// 알이 떨어지면 EggController.MarkFallen()을 호출한다.
    /// 기획서 6-4 낙하 판정 알고리즘을 구현한다.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class BoardFallZone : MonoBehaviour
    {
        private Collider fallCollider;

        private void Awake()
        {
            fallCollider = GetComponent<Collider>();
            fallCollider.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            EggController egg = other.GetComponent<EggController>();
            if (egg != null && egg.IsAlive)
            {
                Debug.Log($"[BoardFallZone] Egg fell: {other.gameObject.name} (Player {egg.OwnerPlayerId})");
                egg.MarkFallen();
            }
        }

        private void OnDrawGizmos()
        {
            if (fallCollider != null)
            {
                Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
                Gizmos.DrawCube(transform.position, transform.lossyScale);
            }
        }
    }
}
