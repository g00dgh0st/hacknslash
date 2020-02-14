using System.Collections.Generic;
using UnityEngine;

namespace ofr.grim {
  public enum MovementState {
    Locomotion,
    Attack,
    Dodge,
    Hit
  }

  public class DudeController : MonoBehaviour {

    private bool _isGrounded;
    protected bool isGrounded {
      get {
        return this._isGrounded;
      }
      set {
        this._isGrounded = value;
        if (value == true) {
          // if grounded, reset to locomotion state
          this.movementState = MovementState.Locomotion;
        }
      }
    }

    protected MovementState movementState { get; set; }

    [SerializeField]
    protected AnimationLibrary animLibrary;

    protected void Awake() {
      animLibrary.BuildLookup();
    }

  }
}