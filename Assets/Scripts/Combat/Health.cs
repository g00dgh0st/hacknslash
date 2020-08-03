using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ofr.grim.combat {
  public class Health : MonoBehaviour {
    private float maxHealth = 100f;
    private float currentHealth;
    private bool isDead = false;
    void Awake() {
      currentHealth = maxHealth;
    }

    public void TakeDamage(float damage) {
      if (isDead) return;

      currentHealth = Mathf.Max(0, currentHealth - damage);

      if (currentHealth <= 0) {
        Die();
      }
    }

    public void Die() {
      isDead = true;
    }
  }
}