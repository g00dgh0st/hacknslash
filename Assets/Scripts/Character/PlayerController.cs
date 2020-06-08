﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ofr.grim {

  public class PlayerController : DudeController {
    // DEBUG STUFF
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
    }

    void OnAnimatorMove() {
      if (attackMovement) {
        // attack animations only atm
        controller.Move(anim.deltaPosition);
      }
    }

    void Update() {
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

      if (Input.GetMouseButtonDown(0)) {
        Attack(moveInput);
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
      Vector3 castDirection = moveInput.magnitude > 0.1 ? moveInput : transform.forward;

      if (Physics.SphereCast(transform.position, 2f, castDirection, out RaycastHit hit, 3f, enemyLayerMask)) {
        HandleTurning(hit.transform.position - transform.position);
        Debug.DrawRay(transform.position, castDirection * 10f, Color.red, 1f);
        Debug.DrawRay(transform.position, (hit.transform.position - transform.position) * 10f, Color.green, 1f);
      }

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