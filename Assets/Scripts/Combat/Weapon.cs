using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ofr.grim.combat {

  public enum FireType {
    Normal,
    Charge,
    Repeat
  }

  [CreateAssetMenu(fileName = "Weapon", menuName = "Scriptables/Weapon", order = 0)]
  public class Weapon : ScriptableObject {
    public int animId = 0;
    public float attackDamage = 20f;
    public float chargeTime = 1f;
    public AudioClip swingAudio;
    public GameObject hitFX;
    public FireType fireType;
    public AttackType type;

    public GameObject rightHandPrefab;
    public GameObject leftHandPrefab;
  }
}