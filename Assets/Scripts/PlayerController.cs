using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour {
  private Animator anim;
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

  private bool isRolling = false;
  private bool isGrounded = true;
  private bool isAttacking = false;
  private Vector3 moveVector;

  void Awake() {
    controller = GetComponent<CharacterController>();
    anim = GetComponent<Animator>();
    mainCam = Camera.main;
  }

  void Update() {
    float h = Input.GetAxis("Horizontal");
    float v = Input.GetAxis("Vertical");

    ApplyGravity();

    // if airborne

    if (isGrounded) {
      // grounded locomotion
      if (isRolling) {
        HandleRolling();
      } else {
        // TODO: state management
        CheckAttacking();
        if (Input.GetMouseButtonDown(1)) {
          StartRoll(GetMoveDirectionByCamera(h, v));
        } else if (Input.GetMouseButtonDown(0)) {
          isAttacking = true;
          anim.SetTrigger("attack");
        } else if (!isAttacking) {
          if (Input.GetButtonDown("Jump")) {
            StartJump();
          } else {
            HandleMoving(GetMoveDirectionByCamera(h, v));
          }
        }

      }
    } else {
      // air control
      HandleTurning(GetMoveDirectionByCamera(h, v), airTurnDampMultiplier);
    }

    MakeMove();
  }

  void OnAnimatorMove() {
    if (isAttacking) {
      // attack animations atm
      controller.Move(anim.deltaPosition);
    }
  }

  private bool ApplyGravity() {
    if (controller.isGrounded) {
      anim.SetBool("grounded", true);
      isGrounded = true;
      moveVector = Physics.gravity * Time.deltaTime;
      return true;
    } else if (moveVector.y <= 0 && Physics.Raycast(transform.position, Vector3.down, groundCheckDistance, groundCheckLayer)) {
      anim.SetBool("grounded", true);
      isGrounded = true;
      moveVector = Vector3.down * groundCheckDistance / Time.deltaTime;
      return true;
    }
    anim.SetBool("grounded", false);
    isGrounded = false;
    moveVector += Physics.gravity * Time.deltaTime * (moveVector.y <= 0 ? fallMultiplier : 1f);
    return false;
  }

  // Apply all movement at once, so there is only one Move call
  void MakeMove() {
    controller.Move(moveVector * Time.deltaTime);
  }

  private void HandleRolling() {
    moveVector += transform.forward * rollSpeed;
    anim.ResetTrigger("roll");
  }

  private void HandleMoving(Vector3 moveInput) {
    float inputMagnitude = moveInput.normalized.magnitude;

    HandleTurning(moveInput);

    moveVector += transform.forward * inputMagnitude * moveSpeed;
    anim.SetFloat("speed", Mathf.Lerp(anim.GetFloat("speed"), inputMagnitude, locomotionDampen));
  }

  private void HandleTurning(Vector3 moveInput, float multiplier = 1f) {
    if (moveInput.magnitude == 0f) return;

    Quaternion targetDirection = Quaternion.LookRotation(moveInput);
    transform.rotation = Quaternion.Lerp(transform.rotation, targetDirection, turnDamped * multiplier);
  }

  private void CheckAttacking() {
    AnimatorStateInfo info = anim.GetCurrentAnimatorStateInfo(0);
    isAttacking = info.IsTag("Combat");
    if (!isAttacking) anim.ResetTrigger("attack");
  }

  void StartJump() {
    anim.SetTrigger("jump");
    moveVector += Vector3.up * jumpSpeed;
  }

  void StartRoll(Vector3 rollDirection) {
    anim.SetTrigger("roll");
    HandleTurning(rollDirection, 100f);
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