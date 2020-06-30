using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ofr.grim {
  public class CombatTarget : MonoBehaviour {

    [SerializeField] protected float maxHealth = 100f;
    protected float currentHealth;
    protected bool isDead;

    protected void Start() { currentHealth = maxHealth; }

    public virtual void GetHit(Vector3 hitPosition, float damage = 20f) {}

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