using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ofr.grim {

  [RequireComponent(typeof(Rigidbody))]
  [RequireComponent(typeof(Collider))]
  public class EnemyProjectile : MonoBehaviour {
    private Rigidbody rBody;

    private Attack attack;

    // probably could use a global projectile lifetime
    private float lifeTime = 20f;

    void Awake() {
      rBody = GetComponent<Rigidbody>();
    }

    void OnTriggerEnter(Collider other) {
      CombatTarget tgt = other.GetComponent<CombatTarget>();

      if (tgt == null) {
        Destroy(gameObject);
        return;
      }

      if (attack.canHitAllies && tgt.tag == "Enemy") {
        tgt.GetHit(gameObject, attack.damage, attack.isPowerful, attack.hitEffect);
      } else if (tgt.tag == "Player") {
        tgt.GetHit(gameObject, attack.damage, attack.isPowerful, attack.hitEffect);
      }

      Destroy(gameObject);
    }

    public void Fire(Vector3 direction, Attack atk) {
      attack = atk;
      direction.Normalize();
      direction.y = 0;
      transform.forward = direction;
      rBody.velocity = direction * attack.projectileSpeed;

      Destroy(gameObject, Time.time + lifeTime);
    }
  }
}