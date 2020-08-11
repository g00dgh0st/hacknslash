using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ofr.grim.combat {

  public class PlayerProjectile : Projectile {
    private float damage;
    private bool isPowerful;
    private GameObject hitEffect;

    void OnTriggerEnter(Collider other) {
      CombatTarget tgt = other.GetComponent<CombatTarget>();

      if (tgt == null) {
        Destroy(gameObject);
        return;
      } else if (tgt.tag == "Enemy") {
        tgt.GetHit(gameObject, damage, isPowerful, hitEffect);
        Destroy(gameObject);
      }

    }

    public void Fire(Vector3 direction, float speed, float dmg, GameObject hitFX, bool powerful) {
      damage = dmg;
      hitEffect = hitFX;
      isPowerful = powerful;
      direction.Normalize();
      direction.y = 0;
      transform.forward = direction;
      rBody.velocity = direction * speed;

      Destroy(gameObject, Time.time + lifeTime);
    }
  }
}