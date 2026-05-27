using System;
using UnityEngine;
using DinoAlkkagi.Data;

public class EggController : MonoBehaviour
{
    [SerializeField] private int ownerPlayerId;
    [SerializeField] private bool isAlive = true;
    [SerializeField] private Rigidbody cachedRigidbody;
    [SerializeField] private EggDefinition eggDefinition;

    public event Action<EggController> Launched;
    public event Action<EggController, float> CollisionOccurred;
    public event Action<EggController> Fallen;

    public int OwnerPlayerId => ownerPlayerId;
    public bool IsAlive => isAlive;
    public Rigidbody Rigidbody => cachedRigidbody;
    public bool CanLaunch => isAlive && cachedRigidbody != null;
    public EggDefinition Definition => eggDefinition;
    public EggType EggType => eggDefinition != null ? eggDefinition.eggType : EggType.Default;
    public float LaunchImpulseMultiplier => eggDefinition != null ? eggDefinition.launchImpulseMultiplier : 1f;

    private void Awake()
    {
        if (cachedRigidbody == null)
        {
            cachedRigidbody = GetComponent<Rigidbody>();
        }

        if (cachedRigidbody == null)
        {
            Debug.LogError($"{nameof(EggController)} requires a Rigidbody.", this);
        }
    }

    public void Initialize(int ownerId)
    {
        ownerPlayerId = ownerId;
        isAlive = true;
    }

    public void Initialize(int ownerId, EggDefinition definition)
    {
        Initialize(ownerId);
        eggDefinition = definition;
    }

    public void SetDefinition(EggDefinition definition)
    {
        eggDefinition = definition;
    }

    public void Launch(Vector3 impulse)
    {
        if (!CanLaunch)
        {
            return;
        }

        cachedRigidbody.AddForce(impulse * LaunchImpulseMultiplier, ForceMode.Impulse);
        Launched?.Invoke(this);
    }

    public void MarkFallen()
    {
        if (!isAlive)
        {
            return;
        }

        isAlive = false;
        StopImmediately();
        Fallen?.Invoke(this);
    }

    public void StopImmediately()
    {
        if (cachedRigidbody == null)
        {
            return;
        }

        cachedRigidbody.linearVelocity = Vector3.zero;
        cachedRigidbody.angularVelocity = Vector3.zero;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!isAlive)
        {
            return;
        }

        if (collision.collider.GetComponentInParent<EggController>() == null)
        {
            return;
        }

        float impact = collision.relativeVelocity.magnitude;
        CollisionOccurred?.Invoke(this, impact);
    }
}
