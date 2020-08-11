using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ofr.grim.combat {

  [RequireComponent(typeof(Rigidbody))]
  [RequireComponent(typeof(Collider))]
  public class Projectile : MonoBehaviour {
    [HideInInspector]
    public Rigidbody rBody;

    // probably could use a global projectile lifetime
    protected float lifeTime = 20f;

    void Awake() {
      rBody = GetComponent<Rigidbody>();
    }
  }
}