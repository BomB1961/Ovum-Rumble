using System;
using UnityEngine;

public class EggController : MonoBehaviour
{
    [SerializeField] private int ownerPlayerId;
    [SerializeField] private bool isAlive = true;
    [SerializeField] private Rigidbody cachedRigidbody;

    public event Action<EggController> Launched;
    public event Action<EggController, float> CollisionOccurred;
    public event Action<EggController, float, Vector3, Vector3> CollisionDetailedOccurred;
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

        if (collision.collider.GetComponentInParent<EggController>() == null)
        {
            return;
        }

        float impact = collision.relativeVelocity.magnitude;
        Vector3 contactPoint = transform.position;
        Vector3 contactNormal = (collision.transform.position - transform.position).normalized;

        if (collision.contactCount > 0)
        {
            ContactPoint contact = collision.GetContact(0);
            contactPoint = contact.point;
            contactNormal = (contact.point - transform.position).normalized;
        }

        if (contactNormal.sqrMagnitude <= 0.0001f)
        {
            contactNormal = transform.forward;
        }

        CollisionOccurred?.Invoke(this, impact);
        CollisionDetailedOccurred?.Invoke(this, impact, contactPoint, contactNormal.normalized);
    }
}
