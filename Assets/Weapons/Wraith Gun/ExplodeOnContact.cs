using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExplodeOnContact : MonoBehaviour
{
    public GameObject explosionPrefab;
    public float radius = 5f;
    public float maxDamage = 10f;
    public float maxForce = 10f;
    public AnimationCurve FalloffCurve = AnimationCurve.Linear(0, 1, 1, 0);

    private float normalizedDist = 0f;


    public void OnCollisionEnter(Collision collision) {
        GameObject explosion = Instantiate(explosionPrefab, transform.position, Quaternion.identity);

        //find all objects within the explosion radius
        Collider[] colliders = Physics.OverlapSphere(transform.position, radius);

        foreach (Collider hit in colliders) {
            float distance = Vector3.Distance(transform.position, hit.transform.position);

            if (radius > 0) normalizedDist = Mathf.Clamp01(distance / radius);
            else normalizedDist = 0f;

            float falloffMultiplier = FalloffCurve.Evaluate(normalizedDist);

            Rigidbody rb = hit.GetComponent<Rigidbody>();
            if (rb != null) rb.AddExplosionForce(maxForce, transform.position, radius);

            float calculatedDmg = maxDamage * falloffMultiplier;

            if (hit.CompareTag("Enemy") || hit.CompareTag("Player")) {
                hit.SendMessage("TakeDamage", calculatedDmg, SendMessageOptions.DontRequireReceiver);
            }
        }

        this.GetComponent<Rigidbody>().Sleep();
        Destroy(gameObject, 0.2f);
    }
}
