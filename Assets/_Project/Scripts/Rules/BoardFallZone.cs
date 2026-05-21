using UnityEngine;
using DinoAlkkagi.Core;
using DinoAlkkagi.Environment;

namespace DinoAlkkagi.Rules
{
    [RequireComponent(typeof(Collider))]
    public class BoardFallZone : MonoBehaviour
    {
        private Collider fallCollider;
        private IBoardSurface boardSurface;

        public void SetBoardSurface(IBoardSurface surface)
        {
            boardSurface = surface;
        }

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
                if (boardSurface != null && boardSurface.IsInsidePlayableArea(egg.transform.position))
                {
                    return;
                }

                Debug.Log($"[BoardFallZone] -> MarkFallen: {other.gameObject.name} (P{egg.OwnerPlayerId})");
                egg.MarkFallen();

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
