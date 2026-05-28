using DinoAlkkagi.Data;
using UnityEngine;

namespace DinoAlkkagi.Environment
{
    public class DinosaurWalker : MonoBehaviour
    {
        [field: SerializeField]
        public DinosaurType Type { get; set; }

        [SerializeField]
        [Tooltip("모델이 뒤로 걸으면 180, 옆으로 걸으면 90 또는 -90")]
        private float forwardAngle = 0f;

        [SerializeField]
        private float walkSpeed = 3f;

        [SerializeField]
        private float animatorSpeed = 1f;

        public float ForwardAngle => forwardAngle;
        public float WalkSpeed => walkSpeed;
        public float AnimatorSpeed => animatorSpeed;

        private void OnCollisionEnter(Collision collision)
        {
            DinosaurWalker other = collision.gameObject.GetComponent<DinosaurWalker>();
            if (other == null) return;

            if (Type == DinosaurType.TRex && other.Type == DinosaurType.Velociraptor)
            {
                Destroy(other.gameObject);
            }
        }
    }
}
