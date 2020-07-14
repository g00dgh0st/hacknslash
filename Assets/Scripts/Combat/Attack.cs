using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ofr.grim {
  public enum AttackType {
    Ranged,
    Melee,
    Special
  }

  [CreateAssetMenu(fileName = "Attack", menuName = "Scriptables/Attack", order = 0)]
  public class Attack : ScriptableObject {
    public int animId;
    public AttackType type;
    public AudioClip audio;
    public bool isPowerul;
    public bool canHitAllies;

    public GameObject hitEffect;
    public float damage;

    public EnemyProjectile projectile;
  }

}