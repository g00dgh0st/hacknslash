using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ofr.grim.combat {

  public class PlayerProjectile : Projectile {
    void OnTriggerEnter(Collider other) {
      CombatTarget tgt = other.GetComponent<CombatTarget>();

      if (tgt == null) {
        Destroy(gameObject);
        return;
      } else if (tgt.tag == "Enemey") {

        // tgt.GetHit(gameObject, isDeflected ? deflectDamage : attack.damage, attack.isPowerful, attack.hitEffect);
        Destroy(gameObject);
      }

    }

    public void Fire(Vector3 direction, float speed) {
      // attack = atk;
      direction.Normalize();
      direction.y = 0;
      transform.forward = direction;
      rBody.velocity = direction * speed;

      Destroy(gameObject, Time.time + lifeTime);
    }
  }
}