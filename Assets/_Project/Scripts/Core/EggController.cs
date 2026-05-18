using UnityEngine;

namespace DinoAlkkagi.Core
{
    /// <summary>
    /// 개별 알의 상태와 물리 동작을 관리한다.
    /// Person A가 확장할 기본 클래스.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class EggController : MonoBehaviour
    {
        [SerializeField] private int ownerPlayerId = 1;

        private Rigidbody rb;
        private bool isFallen = false;
        private Vector3 startPosition;
        private Quaternion startRotation;

        public int OwnerPlayerId => ownerPlayerId;
        public bool IsFallen => isFallen;
        public Rigidbody Rigidbody => rb;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                Debug.LogError($"[EggController] Rigidbody missing on {gameObject.name}");
            }
        }

        public void Initialize(int playerId, Vector3 position)
        {
            ownerPlayerId = playerId;
            startPosition = position;
            startRotation = Quaternion.identity;
            ResetEgg();
        }

        /// <summary>
        /// 알이 보드 밖으로 떨어졌을 때 호출된다.
        /// </summary>
        public void MarkFallen()
        {
            if (isFallen) return;
            isFallen = true;
            rb.isKinematic = true;
            gameObject.SetActive(false);
            GameEvents.TriggerEggFell(this);
        }

        /// <summary>
        /// 알을 초기 위치와 상태로 되돌린다.
        /// </summary>
        public void ResetEgg()
        {
            isFallen = false;
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            transform.position = startPosition;
            transform.rotation = startRotation;
            gameObject.SetActive(true);
        }

        /// <summary>
        /// 발사 전용: 알을 지정된 힘과 방향으로 발사한다.
        /// </summary>
        public void Launch(Vector3 force, ForceMode mode = ForceMode.Impulse)
        {
            if (isFallen) return;
            rb.AddForce(force, mode);
            GameEvents.TriggerEggLaunched(this);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (isFallen) return;
            float impact = collision.relativeVelocity.magnitude;
            GameEvents.TriggerEggCollision(impact);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (rb != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawRay(transform.position, rb.linearVelocity);
            }
        }
#endif
    }
}
