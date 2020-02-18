using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ofr.grim {
  public class EnemyController : DudeController {
    public override void GetHit(Vector3 attackPosition) {
      Debug.Log("im hit");
      anim.SetTrigger("hit");
      transform.rotation = Quaternion.LookRotation(attackPosition - transform.position);
    }
  }
}