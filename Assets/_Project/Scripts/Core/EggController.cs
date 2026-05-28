using System;
using UnityEngine;
using DinoAlkkagi.Data;

public class EggController : MonoBehaviour
{
    [SerializeField] private int ownerPlayerId;
    [SerializeField] private bool isAlive = true;
    [SerializeField] private Rigidbody cachedRigidbody;
    [SerializeField] private EggDefinition eggDefinition;

    [Header("Ability Egg")]
    [SerializeField] private float powerMultiplier = 1f; // Power 알: >1.0

    private const float MinRigidbodyMass = 0.0001f;

    private int networkEggId = -1;
    private bool hasCachedBasePhysics;
    private float baseMass;
    private float baseLinearDamping;
    private float baseAngularDamping;

    public event Action<EggController> Launched;
    public event Action<EggController, float> CollisionOccurred;
    public event Action<EggController> Fallen;

    public int OwnerPlayerId => ownerPlayerId;
    public bool IsAlive => isAlive;
    public Rigidbody Rigidbody => cachedRigidbody;
    public bool CanLaunch => isAlive && cachedRigidbody != null;
    public int NetworkEggId => networkEggId;
    public float PowerMultiplier => powerMultiplier;
    public EggDefinition Definition => eggDefinition;
    public EggType EggType => eggDefinition != null ? eggDefinition.eggType : EggType.Default;
    public float LaunchImpulseMultiplier => eggDefinition != null ? eggDefinition.launchImpulseMultiplier : 1f;

    public void SetNetworkEggId(int id) { networkEggId = id; }

    private void Awake()
    {
        if (cachedRigidbody == null)
        {
            cachedRigidbody = GetComponent<Rigidbody>();
        }

        if (cachedRigidbody == null)
        {
            Debug.LogError($"{nameof(EggController)} requires a Rigidbody.", this);
            return;
        }

        CacheBasePhysicsValues();
        ApplyDefinitionPhysics();
    }

    public void Initialize(int ownerId)
    {
        ownerPlayerId = ownerId;
        isAlive = true;
        ApplyDefinitionPhysics();
    }

    public void Initialize(int ownerId, EggDefinition definition)
    {
        ownerPlayerId = ownerId;
        isAlive = true;
        eggDefinition = definition;
        ApplyDefinitionPhysics();
    }

    public void SetDefinition(EggDefinition definition)
    {
        eggDefinition = definition;
        ApplyDefinitionPhysics();
    }

    public void Launch(Vector3 impulse)
    {
        if (!CanLaunch)
        {
            return;
        }

        // EggDefinition 배율 우선, 없으면 Prefab powerMultiplier 사용
        float multiplier = eggDefinition != null ? eggDefinition.launchImpulseMultiplier : powerMultiplier;
        cachedRigidbody.AddForce(impulse * multiplier, ForceMode.Impulse);
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

    private void CacheBasePhysicsValues()
    {
        if (cachedRigidbody == null || hasCachedBasePhysics)
        {
            return;
        }

        baseMass = cachedRigidbody.mass;
        baseLinearDamping = cachedRigidbody.linearDamping;
        baseAngularDamping = cachedRigidbody.angularDamping;
        hasCachedBasePhysics = true;
    }

    private void ApplyDefinitionPhysics()
    {
        if (cachedRigidbody == null)
        {
            return;
        }

        CacheBasePhysicsValues();

        if (!hasCachedBasePhysics)
        {
            return;
        }

        if (eggDefinition == null)
        {
            RestoreBasePhysicsValues();
            return;
        }

        cachedRigidbody.mass = Mathf.Max(MinRigidbodyMass, baseMass * Mathf.Max(0f, eggDefinition.massMultiplier));
        cachedRigidbody.linearDamping = Mathf.Max(0f, baseLinearDamping * Mathf.Max(0f, eggDefinition.linearDampingMultiplier));
        cachedRigidbody.angularDamping = Mathf.Max(0f, baseAngularDamping * Mathf.Max(0f, eggDefinition.angularDampingMultiplier));
    }

    private void RestoreBasePhysicsValues()
    {
        if (cachedRigidbody == null || !hasCachedBasePhysics)
        {
            return;
        }

        cachedRigidbody.mass = Mathf.Max(MinRigidbodyMass, baseMass);
        cachedRigidbody.linearDamping = Mathf.Max(0f, baseLinearDamping);
        cachedRigidbody.angularDamping = Mathf.Max(0f, baseAngularDamping);
    }
}
