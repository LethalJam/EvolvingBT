using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletBehaviour : MonoBehaviour {

    // Private variables
    private float m_lifetimeTimer = 0.0f;

    // Public variables
    public int damage = 10;
    private float lifeTime = 2.0f; 

    // Used by colliding bodies to retrieve damage.
	public int GetDamage()
    {
        return damage;
    }

    // Destroy the bullet if it collides with terrain.
    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "terrain")
            Destroy(this.gameObject);
    }

    // Update state of timer and destroy when appropriate.
    void Update ()
    {
        m_lifetimeTimer += Time.deltaTime;

        if (m_lifetimeTimer >= lifeTime)
            Destroy(this.gameObject);
	}
}
