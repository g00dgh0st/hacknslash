using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ofr.grim.combat {

  [System.Serializable]
  public struct WeaponCollision {
    public WeaponCollider left;
    public WeaponCollider right;
    public WeaponCollider front;
  }

  public class WeaponCollider : MonoBehaviour {
    public float radius;
  }
}