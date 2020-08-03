using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ofr.grim.player {

  [CreateAssetMenu(fileName = "Weapon", menuName = "Scriptables/Weapon", order = 0)]
  public class Weapon : ScriptableObject {
    public int animId = 0;
    public float attackDamage = 20f;
    public AudioClip swingAudio;
    public GameObject hitFX;
    public GameObject rightHandPrefab;
    public GameObject leftHandPrefab;

  }
}