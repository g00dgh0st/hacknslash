using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum MovementState {
  Grounded
}

public class PlayerController : MonoBehaviour {
  private Animator anim;
  private Camera mainCam;
  private CharacterController controller;

  private float locomotionDampen = 0.2f;
  private float turnDamped = 0.5f;
  private float moveSpeed = 5f;
  private float rollSpeed = 10f;

  private bool isRolling = false;

  void Awake() {
    controller = GetComponent<CharacterController>();
    anim = GetComponent<Animator>();
    mainCam = Camera.main;
  }

  void Update() {

    if (isRolling) {
      HandleRolling();
      return;
    }

    if (Input.GetMouseButtonDown(1)) {
      anim.SetTrigger("dodge");
    }

    if (Input.GetButtonDown("Jump")) {
      anim.SetTrigger("jump");
    }

    float h = Input.GetAxis("Horizontal");
    float v = Input.GetAxis("Vertical");

    HandleMovement(GetMoveDirectionByCamera(h, v));
  }

  private void HandleRolling() {
    controller.Move(transform.forward * rollSpeed * Time.deltaTime);
  }

  private void HandleMovement(Vector3 moveInput) {
    float inputMagnitude = moveInput.normalized.magnitude;

    if (moveInput.magnitude > 0f) {
      // handle rotation
      Quaternion targetDirection = Quaternion.LookRotation(moveInput);
      transform.rotation = Quaternion.Lerp(transform.rotation, targetDirection, turnDamped);
      controller.Move(transform.forward * inputMagnitude * moveSpeed * Time.deltaTime);
    }

    anim.SetFloat("speed", Mathf.Lerp(anim.GetFloat("speed"), moveInput.normalized.magnitude, locomotionDampen));
  }

  private Vector3 GetMoveDirectionByCamera(float horizontalAxis, float verticalAxis) {
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

  // Animation Event
  public void Rolling(int rolling) {
    isRolling = (rolling == 1 ? true : false);
  }
}