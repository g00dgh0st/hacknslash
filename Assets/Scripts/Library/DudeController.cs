using System.Collections.Generic;
using UnityEngine;

namespace ofr.grim {
  public enum MovementState {
    Locomotion,
    Attack,
    Dodge,
    Hit
  }

  [RequireComponent(typeof(Animator))]
  public class DudeController : MonoBehaviour {
    protected Animator anim;

    [SerializeField]
    private float locomotionTransitionDampen = 0.2f;

    private bool _isGrounded;
    protected bool isGrounded {
      get {
        return this._isGrounded;
      }
      set {
        if (value == true && this._isGrounded == false) {
          // if grounded, reset to locomotion state
          this.movementState = MovementState.Locomotion;
        }
        this._isGrounded = value;
        anim.SetBool("grounded", this._isGrounded);
      }
    }
    protected MovementState movementState { get; set; }

    protected bool dodgeMovement = false;
    protected bool attackMovement = false;
    private bool canAttack = true;

    protected void Awake() {
      anim = GetComponent<Animator>();
    }

    protected void AnimateLocomotion(float speed) {
      anim.SetFloat("speed", Mathf.Lerp(anim.GetFloat("speed"), speed, locomotionTransitionDampen));
    }

    protected void AnimateDodge() {
      movementState = MovementState.Dodge;
      anim.SetTrigger("dodge");
    }

    protected bool AnimateAttack() {
      if (canAttack) {
        canAttack = false;
        anim.SetTrigger("attack");
        return true;
      }

      return false;
    }

    /// ANIMATION EVENTS
    void DodgeEvent(string message) {
      if (message == "start") {
        dodgeMovement = true;
      }

      if (message == "end") {
        dodgeMovement = false;
        anim.ResetTrigger("dodge");
        movementState = MovementState.Locomotion;
      }
    }

    void AttackEvent(string message) {
      if (message == "start") {;
        movementState = MovementState.Attack;
        attackMovement = true;
      }

      if (message == "combo") {
        canAttack = true;
      }

      if (message == "end") {
        print("end");
        movementState = MovementState.Locomotion;
        attackMovement = false;
        canAttack = true;
        anim.ResetTrigger("attack");
      }
    }
  }
}