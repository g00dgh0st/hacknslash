using System;
using System.Collections;
using System.Collections.Generic;
using ofr.grim.debug;
using UnityEngine;
using UnityEngine.UI;

namespace ofr.grim {
  public enum MovementState {
    Locomotion,
    Attack,
    Dodge,
    Hit,
    Block
  }

  public enum AttackState {
    Swing,
    Continue,
    End
  }

  public class PlayerController : MonoBehaviour, CombatTarget {
    // DEBUG STUFF
    public bool debugMode = false;
    [SerializeField]
    private GameObject debug;
    public Text debugText;
    // END DEBUGT STUFF

    private Camera mainCam;
    private CharacterController controller;

    private float turnDamped = 10f;
    private float airTurnDampMultiplier = 0.5f;
    private float attackTurnTime = 0.1f;
    private float moveSpeed = 5f;
    private float blockMaxMoveInput = 0.4f;
    private float rollSpeed = 12f;
    private float fallMultiplier = 1.5f;
    private float groundCheckDistance = 0.4f;
    float lockOnCastRadius = 0.8f;
    float lockOnCastDistance = 3.5f;

    // This should be part of a weapon object
    float gapCloseMaxReach = 2.05f;
    float gapCloseMinReach = 1.3f;
    float gapCloseSpeed = 0.15f;

    private Vector3 moveVector;

    // TODO: remove this
    [SerializeField] protected LayerMask groundCheckLayer;
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

    void Awake() {
      anim = GetComponent<Animator>();
      audio = GetComponent<AudioSource>();
      attackState = AttackState.End;
      attackHits = new List<CombatTarget>();
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
        Dodge(moveInput);
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
        moveVector += transform.forward.normalized * rollSpeed;
      }
    }

    private void HandleAttackControl() {
      Vector3 moveInput = GetInputDirectionByCamera();

      if (Input.GetButtonDown("Jump") && attackState != AttackState.Swing) {
        // trigger the end event just to make sure attack state is cleared
        AttackEvent("end");
        Dodge(moveInput);
        return;
      }

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
      HandleMoving(Vector3.ClampMagnitude(moveInput, blockMaxMoveInput));

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

      Quaternion targetRotation = Quaternion.LookRotation(moveInput);

      transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, turnDamped * multiplier * Time.deltaTime);
    }

    private IEnumerator HandleTurningAsync(Vector3 turnDir, float turnTime) {
      // TODO: if this takes in a zero magnitude turn dir, it'll try to turn anyway
      Quaternion targetRotation = Quaternion.LookRotation(turnDir);
      Quaternion startRotation = transform.rotation;
      float timeTaken = 0f;
      float turnT = 0f;

      while (turnT <= 1f) {
        timeTaken += Time.deltaTime;
        turnT = timeTaken / turnTime;
        transform.rotation = Quaternion.Lerp(startRotation, targetRotation, turnT);
        yield return true;
      }
    }

    private void HandleTurnInstant(Vector3 moveInput) {
      if (moveInput.magnitude == 0f) return;
      transform.rotation = Quaternion.LookRotation(moveInput);
    }

    private void Attack(Vector3 moveInput) {
      Vector3 castDirection = moveInput.magnitude > 0.1 ? moveInput.normalized : transform.forward;
      Vector3 castPosition = transform.position + Vector3.up;

      Vector3 castDirectionRight = Vector3.Cross(Vector3.up, castDirection).normalized;

      bool centerCast = Physics.Raycast(castPosition, castDirection, out RaycastHit centerHit, lockOnCastDistance, enemyLayerMask);
      bool leftCast = Physics.Raycast(castPosition - (castDirectionRight * lockOnCastRadius), castDirection, out RaycastHit leftHit, lockOnCastDistance, enemyLayerMask);
      bool rightCast = Physics.Raycast(castPosition + (castDirectionRight * lockOnCastRadius), castDirection, out RaycastHit rightHit, lockOnCastDistance, enemyLayerMask);

      if (debugMode) {
        PlayerDebug pd = debug.GetComponent<PlayerDebug>();
        pd.UpdateMoveLines(castPosition, castDirection, castDirectionRight, lockOnCastRadius, lockOnCastDistance);
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
        // locked on
        if (debugMode) debug.GetComponent<PlayerDebug>().UpdateLockLine(lockDir, lockOnCastDistance);

        StartCoroutine(HandleTurningAsync(lockDir, attackTurnTime));

        if (lockDir.magnitude > gapCloseMaxReach) {
          StartCoroutine(AttackMove(transform.position + Vector3.ClampMagnitude(lockDir, (lockDir.magnitude - gapCloseMinReach)), gapCloseSpeed));
        }
      } else {
        StartCoroutine(HandleTurningAsync((transform.position + castDirection) - transform.position, attackTurnTime));
      }

      // TODO: track lock target?

      AnimateAttack();
    }

    private IEnumerator AttackMove(Vector3 targetPosition, float timeToReach) {
      attackMovement = false;

      Vector3 startPos = transform.position;
      float timeTaken = 0f;
      float turnT = 0f;

      while (turnT <= 1f) {
        timeTaken += Time.deltaTime;
        turnT = timeTaken / timeToReach;
        controller.Move(Vector3.Lerp(startPos, targetPosition, turnT) - transform.position);
        yield return true;
      }
    }

    private void Dodge(Vector3 moveDir) {
      HandleTurnInstant(moveDir);
      AnimateDodge();
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
      float horizontalAxis = Input.GetAxisRaw("Horizontal");
      float verticalAxis = Input.GetAxisRaw("Vertical");

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

    ///// DUDE CONTROOLLER STUFF
    protected Animator anim;
    protected AudioSource audio;

    [SerializeField]
    private AudioClip swingAudio;

    [SerializeField]
    private WeaponCollision weaponCollision;

    [SerializeField]
    protected GameObject hitFX;

    [SerializeField]
    private float locomotionTransitionDampen = 0.2f;

    [SerializeField] protected LayerMask enemyLayerMask;

    protected MovementState movementState { get; set; }
    protected AttackState attackState { get; set; }

    protected bool dodgeMovement = false;
    protected bool attackMovement = false;

    protected List<CombatTarget> attackHits;

    private void AttackCollision(WeaponCollider collider) {
      Collider[] hits = Physics.OverlapSphere(collider.transform.position, collider.radius, enemyLayerMask);

      foreach (Collider hit in hits) {
        CombatTarget tgt = hit.GetComponent<CombatTarget>();

        if (tgt != null && !attackHits.Exists((t) => GameObject.ReferenceEquals(t, tgt))) {
          attackHits.Add(tgt);
          tgt.GetHit(transform.position);
          Destroy(Instantiate(hitFX, hit.ClosestPoint(transform.position + Vector3.up), transform.rotation), 2f);
        }
      }
    }

    public virtual void GetHit(Vector3 hitPosition) {
      // TODO: need hit state logic 
      // movementState = MovementState.Hit;
      anim.SetTrigger("hit");
    }

    public virtual void Die() {
      print("i dead");
    }

    protected void AnimateLocomotion(float speed) {
      // TODO: this should only lerp down to 0 not always
      anim.SetFloat("speed", Mathf.Lerp(anim.GetFloat("speed"), speed, locomotionTransitionDampen));
    }

    protected void AnimateDodge() {
      movementState = MovementState.Dodge;
      anim.SetTrigger("dodge");
    }

    protected void AnimateAttack() {
      anim.SetTrigger("attack");
    }

    protected void ToggleBlock(bool blockOn) {
      anim.SetBool("block", blockOn);
      if (blockOn) movementState = MovementState.Block;
      else movementState = MovementState.Locomotion;
    }

    /// ANIMATION EVENTS
    public void AttackMachineCallback(AttackState state) {
      attackState = state;

      if (attackState != AttackState.Swing) {
        attackHits.Clear();
      }
    }

    protected void DodgeEvent(string message) {
      if (message == "start") {
        dodgeMovement = true;
      }

      if (message == "end") {
        dodgeMovement = false;
        anim.ResetTrigger("dodge");
        movementState = MovementState.Locomotion;
      }
    }

    protected void AttackEvent(string message) {
      if (message == "start") {
        movementState = MovementState.Attack;
        attackMovement = true;
      }

      if (message == "swing") {
        // TEMP: just trying out audio
        audio.PlayOneShot(swingAudio);
      }

      if (message.Contains("collide")) {
        string[] split = message.Split('.');

        if (split.Length > 1) {
          switch (split[1]) {
            case "left":
              AttackCollision(weaponCollision.left);
              break;
            case "right":
              AttackCollision(weaponCollision.right);
              break;
            default:
              AttackCollision(weaponCollision.front);
              break;
          }
        } else {
          AttackCollision(weaponCollision.front);
        }
      }

      if (message == "end") {
        movementState = MovementState.Locomotion;
        attackMovement = false;
        // canAttack = true;
        anim.ResetTrigger("attack");
      }
    }

    protected void HitEvent(string message) {
      if (message == "start") {
        print("HOTT");
        movementState = MovementState.Hit;
      }

      if (message == "end") {
        movementState = MovementState.Locomotion;
      }
    }

  }
}