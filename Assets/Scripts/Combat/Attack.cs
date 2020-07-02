using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ofr.grim {
  [System.Serializable]
  public class Attack {
    public AudioClip swingAudio;
    public GameObject hitEffect;
    public float attackDamage;
    public bool isPowerul;
    public int id;
  }
}