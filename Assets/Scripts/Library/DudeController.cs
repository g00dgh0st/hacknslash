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

    protected bool dodgeMovement;
    protected MovementState movementState { get; set; }
    private Animator anim;

    [SerializeField]
    private float locomotionTransitionDampen = 0.2f;

    protected void Awake() {
      anim = GetComponent<Animator>();
    }

    protected void Locomotion(float speed) {
      anim.SetFloat("speed", Mathf.Lerp(anim.GetFloat("speed"), speed, locomotionTransitionDampen));
    }

    protected void Dodge() {
      anim.SetTrigger("dodge");
    }

    /// ANIMATION EVENTS
    void DodgeEvent(string message) {
      movementState = MovementState.Dodge;

      if (message == "start") {
        dodgeMovement = true;
      }

      if (message == "end") {
        dodgeMovement = false;
        anim.ResetTrigger("dodge");
        movementState = MovementState.Locomotion;
      }
    }
  }
}