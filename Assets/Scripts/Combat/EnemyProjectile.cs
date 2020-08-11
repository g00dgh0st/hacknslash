using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ofr.grim.combat {

  public class EnemyProjectile : Projectile {
    [HideInInspector]
    public Attack attack;

    // probably could use a global projectile lifetime
    private float lifeTime = 20f;
    private bool isDeflected = false;
    private float deflectDamage;

    void OnTriggerEnter(Collider other) {
      CombatTarget tgt = other.GetComponent<CombatTarget>();

      if (tgt == null) {
        Destroy(gameObject);
        return;
      }

      switch (tgt.tag) {
        case "Enemy":
          if (isDeflected || attack.canHitAllies) {
            tgt.GetHit(gameObject, isDeflected ? deflectDamage : attack.damage, attack.isPowerful, attack.hitEffect);
            Destroy(gameObject);
          }
          break;
        case "Player":
          if (!isDeflected) {
            tgt.GetHit(gameObject, attack.damage, attack.isPowerful, attack.hitEffect);
            Destroy(gameObject);
          }
          break;
        default:
          Destroy(gameObject);
          break;
      }
    }

    public void Fire(Vector3 direction, Attack atk) {
      attack = atk;
      direction.Normalize();
      direction.y = 0;
      transform.forward = direction;
      rBody.velocity = direction * attack.projectileSpeed;

      Destroy(gameObject, Time.time + lifeTime);
    }

    public void Deflect(EnemyProjectile original, float damageMultiplier) {
      GetComponent<Collider>().enabled = false;
      attack = original.attack;
      transform.forward = -original.transform.forward;
      rBody.velocity = -original.rBody.velocity;
      isDeflected = true;
      deflectDamage = damageMultiplier * attack.damage;
      GetComponent<Collider>().enabled = true;
    }
  }
}