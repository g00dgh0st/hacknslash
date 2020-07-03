using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ofr.grim {

  [RequireComponent(typeof(Rigidbody))]
  [RequireComponent(typeof(Collider))]
  public class EnemyProjectile : MonoBehaviour {
    private Rigidbody rBody;

    private float lifeTime = 20f;
    public float speed = 4f;
    public float damage = 10f;
    public bool isPowerful = false;
    public GameObject hitFX;

    private float deathTime;
    private bool canHitAllies;
    private Vector3 direction;

    void Awake() {
      rBody = GetComponent<Rigidbody>();
    }

    void FixedUpdate() {
      if (Time.time > deathTime) {
        Destroy(gameObject);
      }
    }

    void OnTriggerEnter(Collider other) {
      CombatTarget tgt = other.GetComponent<CombatTarget>();

      if (tgt == null) {
        Destroy(gameObject);
        return;
      }

      if (canHitAllies && tgt.tag == "Enemy") {
        tgt.GetHit(transform.position, damage, isPowerful, hitFX);
      } else if (tgt.tag == "Player") {
        tgt.GetHit(transform.position, damage, isPowerful, hitFX);
      }

      Destroy(gameObject);
    }

    public void Fire(Vector3 direction) {
      direction.y = 0;
      transform.forward = direction;
      rBody.velocity = direction * speed;
      deathTime = Time.time + lifeTime;
    }
  }
}