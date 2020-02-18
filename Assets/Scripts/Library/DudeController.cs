using System.Collections.Generic;
using UnityEngine;

namespace ofr.grim {
  public enum MovementState {
    Locomotion,
    Attack,
    Dodge,
    Hit,
    Block
  }

  [RequireComponent(typeof(Animator))]
  public class DudeController : MonoBehaviour {
    protected Animator anim;

    [SerializeField]
    private Collider attackCollider;

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

    protected void Awake() {
      anim = GetComponent<Animator>();
    }

    public void AttackCollide(Collider target) {
      if (target.tag == "Enemy") {
        target.GetComponent<DudeController>().GetHit(transform.position);
      } else if (target.tag == "Hittable") {
        // stuff
      }
    }

    public virtual void GetHit(Vector3 attackPosition) {
      anim.SetTrigger("hit");
    }

    public virtual void Die() {
      print("i dead");
    }

    protected void AnimateLocomotion(float speed) {
      anim.SetFloat("speed", Mathf.Lerp(anim.GetFloat("speed"), speed, locomotionTransitionDampen));
    }

    protected void AnimateDodge() {
      movementState = MovementState.Dodge;
      anim.SetTrigger("dodge");
    }

    protected bool AnimateAttack() {
      anim.SetTrigger("attack");
      return true;
    }

    protected void ToggleBlock(bool blockOn) {
      anim.SetBool("block", blockOn);
      if (blockOn) movementState = MovementState.Block;
      else movementState = MovementState.Locomotion;
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

      if (message == "collideOn") {
        attackCollider.enabled = true;
      }

      if (message == "collideOff") {
        attackCollider.enabled = false;
      }

      if (message == "end") {
        movementState = MovementState.Locomotion;
        attackMovement = false;
        // canAttack = true;
        anim.ResetTrigger("attack");
      }
    }

    void HitEvent(string message) {
      if (message == "start") {
        movementState = MovementState.Hit;
      }

      if (message == "end") {
        movementState = MovementState.Locomotion;
      }
    }
  }
}