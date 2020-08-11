using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ofr.grim.combat {
  public enum AttackType {
    Ranged,
    Melee,
    Special
  }

  [CreateAssetMenu(fileName = "Attack", menuName = "Scriptables/Attack", order = 0)]
  public class Attack : ScriptableObject {
    public int animId;
    public AudioClip audio;
    public AttackType type;
    public bool canHitAllies;
    public bool isPowerful;
    public float damage;
    public GameObject hitEffect;

    public float projectileSpeed;
    public EnemyProjectile projectile;
  }

}