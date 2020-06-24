using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace ofr.grim {
  public enum AIState {
    Idle,
    Patrol,
    Combat,
    Reset
  }

  public class EnemyController : DudeController {
    private NavMeshAgent navAgent;
    private Rigidbody rBody;
    private AIState state = AIState.Idle;

    new void Awake() {
      base.Awake();
      navAgent = GetComponent<NavMeshAgent>();
      rBody = GetComponent<Rigidbody>();
    }

    void Update() {
      switch (movementState) {
        case MovementState.Locomotion:
          HandleGroundedControl();
          break;
        case MovementState.Dodge:
          HandleDodgeControl();
          break;
        case MovementState.Attack:
          HandleAttackControl();
          break;
        case MovementState.Block:
          HandleBlockControl();
          break;
        default:
          // should not happen
          break;
      }
    }

    private void HandleGroundedControl() {
      Vector3 ppos = GameManager.player.gameObject.transform.position;
      navAgent.SetDestination(ppos);
      AnimateLocomotion(GetAnimatorSpeed());
    }

    private void HandleDodgeControl() {
      throw new NotImplementedException();
    }

    private void HandleAttackControl() {
      throw new NotImplementedException();
    }

    private void HandleBlockControl() {
      throw new NotImplementedException();
    }

    private void HandleAirborneControl() {
      throw new NotImplementedException();
    }

    public override void GetHit(Vector3 attackPosition) {
      anim.SetTrigger("hit");
      transform.rotation = Quaternion.LookRotation(attackPosition - transform.position);
      navAgent.Move(transform.forward * -0.05f);
    }

    private float GetAnimatorSpeed() {
      return navAgent.velocity.magnitude / navAgent.speed;
    }
  }
}