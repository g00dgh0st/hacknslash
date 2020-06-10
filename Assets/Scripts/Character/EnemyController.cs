using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace ofr.grim {
  public class EnemyController : DudeController {
    private NavMeshAgent navAgent;
    private Rigidbody rBody;

    new void Awake() {
      base.Awake();
      navAgent = GetComponent<NavMeshAgent>();
      rBody = GetComponent<Rigidbody>();
    }

    public override void GetHit(Vector3 attackPosition) {
      anim.SetTrigger("hit");
      transform.rotation = Quaternion.LookRotation(attackPosition - transform.position);
      // navAgent.Move(transform.forward * -0.05f);
    }
  }
}