using System;
using Unity.VisualScripting;
using UnityEngine;

public class Grenade : MonoBehaviour
{
    public float throwForce = 10f;
    public float explosionRadius = 5f;
    public float explosionForce = 700f;
    public float delay = 3f;

    private Rigidbody _rb;
    private bool _hasExploded = false;
    private Vector3 _throwDirection;

    void Start()
    {
        _rb = GetComponent<Rigidbody>();
        ThrowGrenade(_throwDirection);
    }
    
    public void SetThrowDirection(Vector3 direction)
    {
        _throwDirection = direction;
    }

    private void ThrowGrenade(Vector3 direction)
    {
        _rb.AddForce(direction * throwForce, ForceMode.Impulse);
        Invoke("Explode", delay);
    }

    private void Update()
    {
        //Draw a wire sphere to visualize the explosion radius
        
        //ADD CODE HERE
        DebugExtension.DebugWireSphere(transform.position, explosionRadius);
    }

    void Explode()
    {
        if (_hasExploded)
            return;

        _hasExploded = true;
        Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius);

        foreach (Collider collider in colliders)
        {
            Rigidbody targetRb = collider.GetComponent<Rigidbody>();
            if (targetRb != null)
            {
                targetRb.AddExplosionForce(explosionForce, transform.position, explosionRadius);
            }
        }
        
        Destroy(gameObject);
    }


}