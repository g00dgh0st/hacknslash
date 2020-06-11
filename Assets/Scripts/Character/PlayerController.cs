using System;
using System.Collections;
using System.Collections.Generic;
using ofr.grim.debug;
using UnityEngine;
using UnityEngine.UI;

namespace ofr.grim {

  public class PlayerController : DudeController {
    // DEBUG STUFF
    public bool debugMode = false;
    [SerializeField]
    private GameObject debug;
    public Text debugText;
    // END DEBUGT STUFF

    private Camera mainCam;
    private CharacterController controller;

    private float turnDamped = 0.5f;
    private float airTurnDampMultiplier = 0.05f;
    private float moveSpeed = 5f;
    private float rollSpeed = 10f;
    private float jumpSpeed = 18f;
    private float fallMultiplier = 1.5f;
    private float groundCheckDistance = 0.4f;
    [SerializeField] private LayerMask groundCheckLayer;
    [SerializeField] private LayerMask enemyLayerMask;

    private Vector3 moveVector;

    new void Awake() {
      base.Awake();
      controller = GetComponent<CharacterController>();
      mainCam = Camera.main;
    }

    void Start() {
      movementState = MovementState.Locomotion;

      if (debugMode)
        debug.SetActive(true);
    }

    void OnAnimatorMove() {
      if (attackMovement) {
        // attack animations only atm
        controller.Move(anim.deltaPosition);
      }
    }

    void Update() {
      if (debugMode)
        debugText.text = isGrounded ? movementState.ToString("g") : "airborne";

      ApplyGravity();

      if (isGrounded) {
        // grounded control
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
      } else {
        // aerial control
        HandleAirborneControl();
      }

      MakeMove();
    }

    private void HandleGroundedControl() {
      Vector3 moveInput = GetInputDirectionByCamera();
      HandleTurning(moveInput);

      if (Input.GetButtonDown("Jump")) {
        AnimateDodge();
        return;
      }

      if (Input.GetMouseButtonDown(0)) {
        Attack(moveInput);
        return;
      }

      if (Input.GetMouseButton(1)) {
        Block(true);
        return;
      }

      HandleMoving(moveInput.normalized);
    }

    private void HandleAirborneControl() {
      HandleTurning(GetInputDirectionByCamera(), airTurnDampMultiplier);
    }

    private void HandleDodgeControl() {
      if (dodgeMovement) {
        moveVector += transform.forward * rollSpeed;
      }
    }

    private void HandleAttackControl() {
      Vector3 moveInput = GetInputDirectionByCamera();

      // if (Input.GetButtonDown("Jump")) {
      //   AnimateDodge();
      //   return;
      // }

      if (Input.GetMouseButtonDown(0)) {
        if (attackState == AttackState.Continue) {
          Attack(moveInput);
        } else if (attackState == AttackState.Swing) {
          /// Queue attack for next tick
        }

        return;
      }
    }

    private void HandleBlockControl() {
      Vector3 moveInput = GetInputDirectionByCamera();
      HandleTurning(moveInput);
      HandleMoving(Vector3.ClampMagnitude(moveInput, 0.4f));

      if (!Input.GetMouseButton(1)) {
        ToggleBlock(false);
        return;
      }
    }

    private void HandleMoving(Vector3 moveInput) {
      float inputMagnitude = moveInput.magnitude;

      moveVector += transform.forward * inputMagnitude * moveSpeed;
      AnimateLocomotion(inputMagnitude);
    }

    private void HandleTurning(Vector3 moveInput, float multiplier = 1f) {
      if (moveInput.magnitude == 0f) return;

      Quaternion targetDirection = Quaternion.LookRotation(moveInput);
      transform.rotation = Quaternion.Lerp(transform.rotation, targetDirection, turnDamped * multiplier);
    }

    private void Attack(Vector3 moveInput) {
      Vector3 castDirection = moveInput.magnitude > 0.1 ? moveInput.normalized : transform.forward;
      Vector3 castPosition = transform.position + Vector3.up;
      float castRadius = 1f;
      float castDistance = 4f;

      Vector3 castDirectionRight = Vector3.Cross(Vector3.up, castDirection).normalized;

      bool centerCast = Physics.Raycast(castPosition, castDirection, out RaycastHit centerHit, castDistance, enemyLayerMask);
      bool leftCast = Physics.Raycast(castPosition - (castDirectionRight * castRadius), castDirection, out RaycastHit leftHit, castDistance, enemyLayerMask);
      bool rightCast = Physics.Raycast(castPosition + (castDirectionRight * castRadius), castDirection, out RaycastHit rightHit, castDistance, enemyLayerMask);

      if (debugMode) {
        PlayerDebug pd = debug.GetComponent<PlayerDebug>();
        pd.UpdateMoveLines(castPosition, castDirection, castDirectionRight, castRadius, castDistance);
      }

      Vector3 lockDir = Vector3.zero;

      if (centerCast) {
        lockDir = centerHit.transform.position - transform.position;
      } else if (rightCast) {
        lockDir = rightHit.transform.position - transform.position;
      } else if (leftCast) {
        lockDir = leftHit.transform.position - transform.position;
      }

      if (lockDir != Vector3.zero) {
        HandleTurning(lockDir, 100f);
        if (debugMode) debug.GetComponent<PlayerDebug>().UpdateLockLine(lockDir, castDistance);
      } else {
        HandleTurning((transform.position + castDirection) - transform.position, 100f);
      }

      // TODO: track lock target?

      AnimateAttack();
    }

    private void Block(bool blockOn) {
      ToggleBlock(blockOn);
    }

    private bool ApplyGravity() {
      if (controller.isGrounded) {
        isGrounded = true;
        moveVector = Physics.gravity * Time.deltaTime;
        return true;
      } else if (moveVector.y <= 0 && Physics.Raycast(transform.position, Vector3.down, groundCheckDistance, groundCheckLayer)) {
        isGrounded = true;
        moveVector = Vector3.down * groundCheckDistance / Time.deltaTime;
        return true;
      }
      isGrounded = false;
      moveVector += Physics.gravity * Time.deltaTime * (moveVector.y <= 0 ? fallMultiplier : 1f);
      return false;
    }

    // Apply all movement at once, so there is only one Move call
    private void MakeMove() {
      controller.Move(moveVector * Time.deltaTime);
    }

    private Vector3 GetInputDirectionByCamera() {
      float horizontalAxis = Input.GetAxis("Horizontal");
      float verticalAxis = Input.GetAxis("Vertical");

      //camera forward and right vectors:
      var forward = mainCam.transform.forward;
      var right = mainCam.transform.right;

      //project forward and right vectors on the horizontal plane (y = 0)
      forward.y = 0f;
      right.y = 0f;
      forward.Normalize();
      right.Normalize();

      //this is the direction in the world space we want to move:
      return forward * verticalAxis + right * horizontalAxis;
    }
  }
}