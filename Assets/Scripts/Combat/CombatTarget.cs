using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ofr.grim {
  [RequireComponent(typeof(Collider))]
  public class CombatTarget : MonoBehaviour {

    [SerializeField] protected float maxHealth = 100f;
    protected float currentHealth;
    protected bool isDead;

    protected void Start() { currentHealth = maxHealth; }

    public virtual void GetHit(GameObject hitter, float damage, bool powerful, GameObject hitFX) {
      return;
    }

    protected virtual void Die() {
      isDead = true;
    }

    protected void TakeDamage(float damage) {
      currentHealth -= damage;
      if (currentHealth <= 0f) {
        Die();
      }
    }
  }
}