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
            Debug.Log($"[BoardFallZone] TriggerEnter: {other.gameObject.name} | Layer={other.gameObject.layer} | HasEgg={egg != null}");

            if (egg != null && egg.IsAlive)
            {
                Debug.Log($"[BoardFallZone] -> MarkFallen: {other.gameObject.name} (P{egg.OwnerPlayerId})");
                egg.MarkFallen();

                // Person A EggController는 MarkFallen()에서 SetActive(false)를 안 함.
                // 여기서 직접 비활성화해서 계속 떨어지는 문제 해결.
                egg.Rigidbody.isKinematic = true;
                egg.gameObject.SetActive(false);
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            // FallZone Trigger보다 알이 빨라서 충돌로 감지되는 경우 대비
            EggController egg = collision.collider.GetComponent<EggController>();
            if (egg != null && egg.IsAlive)
            {
                Debug.Log($"[BoardFallZone] CollisionEnter fallback: {collision.collider.name}");
                egg.MarkFallen();
                egg.Rigidbody.isKinematic = true;
                egg.gameObject.SetActive(false);
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
