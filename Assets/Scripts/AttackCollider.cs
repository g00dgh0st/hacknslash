using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ofr.grim {
  public class AttackCollider : MonoBehaviour {
    private DudeController parentController;

    void Awake() {
      parentController = GetComponentInParent<DudeController>();
    }

    void OnTriggerEnter(Collider target) {
      parentController.AttackCollide(target);
    }
  }
}