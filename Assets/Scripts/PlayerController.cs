﻿using System;
using System.Collections;
using System.Collections.Generic;
using Animancer;
using UnityEngine;

namespace ofr.grim {

  public class PlayerController : DudeController {
    private AnimancerComponent anim;
    private Camera mainCam;
    private CharacterController controller;

    private float locomotionDampen = 0.2f;
    private float turnDamped = 0.5f;
    private float airTurnDampMultiplier = 0.05f;
    private float moveSpeed = 5f;
    private float rollSpeed = 10f;
    private float jumpSpeed = 18f;
    private float fallMultiplier = 1.5f;
    private float groundCheckDistance = 0.4f;
    [SerializeField] private LayerMask groundCheckLayer;

    private Vector3 moveVector;

    new void Awake() {
      base.Awake();
      anim = GetComponent<AnimancerComponent>();
      controller = GetComponent<CharacterController>();
      mainCam = Camera.main;
    }

    void Start() {
      movementState = MovementState.Locomotion;
      anim.Play(animLibrary.animationLookup["idle"]);
    }

    void Update() {
      ApplyGravity();

      if (isGrounded) {
        // grounded control
        HandleGroundedControl();
        // switch (movementState) {
        //   case MovementState.Locomotion:
        //     break;
        //   case MovementState.Dodge:
        //     HandleRolling();
        //     break;
        //   case MovementState.Attack:
        //     HandleAttacking();
        //     break;
        //   default:
        //     // should not happen
        //     break;
        // }
      } else {
        // aerial control
        HandleAirborneControl();
      }

      MakeMove();
    }

    private void HandleGroundedControl() {
      Vector3 moveInput = GetInputDirectionByCamera();
      HandleTurning(moveInput);

      // if (Input.GetButtonDown("Jump")) {
      //   StartRoll(moveInput);
      //   return;
      // }

      // if (Input.GetButtonDown("Jump")) {
      //   HandleMoving(moveInput);
      //   StartJump(moveInput);
      //   return;
      // }

      // if (Input.GetButtonDown("Fire")) {
      //   StartAttack(moveInput);
      //   return;
      // }

      HandleMoving(moveInput);
    }

    private void HandleAirborneControl() {
      HandleTurning(GetInputDirectionByCamera(), airTurnDampMultiplier);
    }

    private void HandleMoving(Vector3 moveInput) {
      float inputMagnitude = moveInput.normalized.magnitude;

      moveVector += transform.forward * inputMagnitude * moveSpeed;
      // SetAnimSpeed(inputMagnitude);
    }

    private void HandleTurning(Vector3 moveInput, float multiplier = 1f) {
      if (moveInput.magnitude == 0f) return;

      Quaternion targetDirection = Quaternion.LookRotation(moveInput);
      transform.rotation = Quaternion.Lerp(transform.rotation, targetDirection, turnDamped * multiplier);
    }

    private bool ApplyGravity() {
      if (controller.isGrounded) {
        // anim.SetBool("grounded", true);
        isGrounded = true;
        moveVector = Physics.gravity * Time.deltaTime;
        return true;
      } else if (moveVector.y <= 0 && Physics.Raycast(transform.position, Vector3.down, groundCheckDistance, groundCheckLayer)) {
        // anim.SetBool("grounded", true);
        isGrounded = true;
        moveVector = Vector3.down * groundCheckDistance / Time.deltaTime;
        return true;
      }
      // anim.SetBool("grounded", false);
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

    // OLD STUFF

    // void OnAnimatorMove() {
    //   if (movementState == MovementState.Attack) {
    //     // attack animations only atm
    //     controller.Move(anim.deltaPosition);
    //   }
    // }

    // private void HandleRolling() {
    //   moveVector += transform.forward * rollSpeed;
    //   anim.ResetTrigger("roll");
    // }

    // private void HandleAttacking() {
    //   AnimatorStateInfo info = anim.GetCurrentAnimatorStateInfo(0);
    //   AnimatorTransitionInfo transInfo = anim.GetAnimatorTransitionInfo(0);
    //   if (Time.time - startAttackTime > 0.3f && !info.IsTag("Attacking") && !transInfo.IsName("Attacking")) {
    //     movementState = MovementState.Locomotion;
    //     anim.ResetTrigger("attack");
    //     return;
    //   }

    //   Vector3 moveInput = GetInputDirectionByCamera();

    //   if (Input.GetMouseButtonDown(0)) {
    //     ContinueAttack(moveInput);
    //     return;
    //   }

    //   SetAnimSpeed(moveInput.normalized.magnitude);
    // }

    // // NOTE: hacky fix
    // private float startAttackTime;

    // public bool IsGrounded {
    //   get =>
    //     throw new NotImplementedException();
    //   set =>
    //     throw new NotImplementedException();
    // }
    // public MovementState MovementState {
    //   get =>
    //     throw new NotImplementedException();
    //   set =>
    //     throw new NotImplementedException();
    // }

    // private void StartAttack(Vector3 moveInput) {
    //   movementState = MovementState.Attack;
    //   startAttackTime = Time.time;
    //   anim.SetTrigger("attack");
    //   anim.SetFloat("speed", 0f);
    // }

    // private void ContinueAttack(Vector3 moveInput) {
    //   anim.SetTrigger("attack");
    // }

    // void StartRoll(Vector3 rollDirection) {
    //   anim.SetTrigger("roll");
    // }

    // // Animation Event
    // public void Rolling(int rolling) {
    //   movementState = (rolling == 1 ? MovementState.Dodge : MovementState.Locomotion);
    // }

    // void StartJump(Vector3 moveInput) {
    //   // TODO: use move vector to affect jump
    //   anim.SetTrigger("jump");
    //   moveVector += (Vector3.up * jumpSpeed);
    // }

    // private void SetAnimSpeed(float inputMagnitude) {
    //   anim.SetFloat("speed", Mathf.Lerp(anim.GetFloat("speed"), inputMagnitude, locomotionDampen));
    // }
  }
}