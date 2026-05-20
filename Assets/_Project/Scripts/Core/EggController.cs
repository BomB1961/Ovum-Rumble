using System;
using UnityEngine;

public class EggController : MonoBehaviour
{
    [SerializeField] private int ownerPlayerId;
    [SerializeField] private bool isAlive = true;
    [SerializeField] private Rigidbody cachedRigidbody;

    public event Action<EggController> Launched;
    public event Action<EggController, float> CollisionOccurred;
    public event Action<EggController> Fallen;

    public int OwnerPlayerId => ownerPlayerId;
    public bool IsAlive => isAlive;
    public Rigidbody Rigidbody => cachedRigidbody;
    public bool CanLaunch => isAlive && cachedRigidbody != null;

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

    public void Launch(Vector3 impulse)
    {
        if (!CanLaunch)
        {
            return;
        }

        cachedRigidbody.AddForce(impulse, ForceMode.Impulse);
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

        float impact = collision.relativeVelocity.magnitude;
        CollisionOccurred?.Invoke(this, impact);
    }
}
