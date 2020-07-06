using System;
using System.Collections;
using System.Collections.Generic;
using ofr.grim.debug;
using UnityEngine;
using UnityEngine.UI;

namespace ofr.grim {
  public enum ControlState {
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

  public class PlayerController : CombatTarget {
    // DEBUG STUFF
    public bool debugMode = false;
    [SerializeField] private GameObject debug;
    public Text debugText;
    // END DEBUGT STUFF

    // dependencies
    private Camera mainCam;
    private CharacterController controller;
    private Animator anim;
    private AudioSource audio;

    // player control config
    private float locomotionTransitionDampen = 0.2f;
    private float turnDamped = 10f;
    private float attackTurnTime = 0.1f;
    private float moveSpeed = 5f;
    private float blockMaxMoveInput = 0.4f;
    private float rollSpeed = 12f;
    private float lockOnCastRadius = 1f;
    private float lockOnCastDistance = 3.5f;
    private float parryTime = 0.2f;
    [SerializeField] private LayerMask enemyLayerMask;
    [SerializeField] private LayerMask groundCheckLayer;

    // This should be part of a weapon object
    float gapCloseMaxReach = 2.05f;
    float gapCloseMinReach = 1.3f;
    float gapCloseSpeed = 0.15f;
    float attackDamage = 20f;
    [SerializeField] private AudioClip swingAudio;
    [SerializeField] private WeaponCollision weaponCollision;
    [SerializeField] private GameObject hitFX;
    [SerializeField] private GameObject blockFX;
    [SerializeField] private GameObject parryFX;
    // end weapon config

    // state vars
    private Vector3 moveVector;
    private ControlState controlState { get; set; }
    private AttackState attackState { get; set; }
    private bool dodgeMovement = false;
    private bool attackMovement = false;
    private bool hitMovement = false;
    private float lastBlockTime;
    private List<CombatTarget> attackHits;

    private Coroutine moveRoutine = null;
    private Coroutine turnRoutine = null;

    void Awake() {
      anim = GetComponent<Animator>();
      audio = GetComponent<AudioSource>();
      attackState = AttackState.End;
      attackHits = new List<CombatTarget>();
      controller = GetComponent<CharacterController>();
      mainCam = Camera.main;
    }

    new void Start() {
      base.Start();

      controlState = ControlState.Locomotion;

      if (debugMode)
        debug.SetActive(true);
    }

    void OnAnimatorMove() {
      if (attackMovement || hitMovement) {
        // attack animations only atm
        controller.Move(anim.deltaPosition);
      }
    }

    float timer = 0f;
    void Update() {
      if (isDead) return;

      if (debugMode)
        debugText.text = controlState.ToString("g");

      // if (Input.GetKeyDown(KeyCode.E)) {
      //   GetHit(transform.position + Vector3.up - transform.forward, 0, false, hitFX);
      // }

      ApplyGravity();

      switch (controlState) {
        case ControlState.Locomotion:
          HandleGroundedControl();
          break;
        case ControlState.Dodge:
          HandleDodgeControl();
          break;
        case ControlState.Attack:
          HandleAttackControl();
          break;
        case ControlState.Block:
          HandleBlockControl();
          break;
        default:
          // should not happen
          break;
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
        ToggleBlock(true);
        return;
      }

      HandleMoving(moveInput.normalized);
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

      if (Input.GetMouseButton(1)) {
        if (attackState != AttackState.Swing) ToggleBlock(true);
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

      if (Input.GetButtonDown("Jump")) {
        ToggleBlock(false);
        Dodge(moveInput);
        return;
      }

    }

    private void HandleMoving(Vector3 moveInput) {
      float inputMagnitude = moveInput.magnitude;

      moveVector += transform.forward * inputMagnitude * moveSpeed;
      AnimateLocomotion(inputMagnitude);
    }

    private IEnumerator HandleMovingAsync(Vector3 targetPosition, float timeToReach) {
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

        turnRoutine = StartCoroutine(HandleTurningAsync(lockDir, attackTurnTime));

        if (lockDir.magnitude > gapCloseMaxReach) {
          moveRoutine = StartCoroutine(HandleMovingAsync(transform.position + Vector3.ClampMagnitude(lockDir, (lockDir.magnitude - gapCloseMinReach)), gapCloseSpeed));
        }
      } else {
        turnRoutine = StartCoroutine(HandleTurningAsync((transform.position + castDirection) - transform.position, attackTurnTime));
      }

      // TODO: track lock target?

      AnimateAttack();
    }

    private void Dodge(Vector3 moveDir) {
      attackMovement = false;
      HandleTurnInstant(moveDir);
      AnimateDodge();
    }

    private bool ApplyGravity() {
      if (controller.isGrounded) {
        moveVector = Physics.gravity * Time.deltaTime;
        return true;
      } else if (Physics.Raycast((transform.position + Vector3.up), Vector3.down, out RaycastHit hit, 100f, groundCheckLayer)) {
        moveVector = (hit.point - transform.position) / Time.deltaTime;
        return true;
      }

      Debug.LogError("PlayerController: Can't find ground");
      moveVector = new Vector3(0f, moveVector.y + (Physics.gravity.y * Time.deltaTime), 0f);
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

    private void AttackCollision(WeaponCollider collider) {
      Collider[] hits = Physics.OverlapSphere(collider.transform.position, collider.radius, enemyLayerMask);

      foreach (Collider hit in hits) {
        CombatTarget tgt = hit.GetComponent<CombatTarget>();

        if (tgt != null && !attackHits.Exists((t) => GameObject.ReferenceEquals(t, tgt))) {
          attackHits.Add(tgt);
          tgt.GetHit(gameObject, attackDamage, false, hitFX);

        }
      }
    }

    public override bool GetHit(GameObject hitter, float damage, bool isPowerful, GameObject fx) {
      if (isDead || controlState == ControlState.Dodge) return false;
      Vector3 hitterPosition = hitter.transform.position;
      Vector3 hitDir = (hitterPosition - transform.position).normalized;
      hitDir.y = 0f;

      if (controlState == ControlState.Block && !isPowerful) {
        turnRoutine = StartCoroutine(HandleTurningAsync(hitDir, attackTurnTime));
        if (lastBlockTime + parryTime > Time.time) {
          // parry
          EnemyController enemy = hitter.GetComponent<EnemyController>();

          if (enemy != null) {
            enemy.GetParried(gameObject, parryFX);
          }
        } else {
          // block hit
          Destroy(Instantiate(blockFX, transform.position + (Vector3.up * 1.5f), transform.rotation), 2f);
        }
      } else {
        Interrupt();
        ToggleBlock(false);
        TakeDamage(damage);
        controlState = ControlState.Hit;
        controller.Move(hitDir * -0.1f);
        Destroy(Instantiate(fx, transform.position + (Vector3.up * 1.5f), transform.rotation), 2f);
        hitMovement = isPowerful;
        anim.SetBool("bigHit", isPowerful);
        anim.SetTrigger("hit");
      }

      return true;
    }

    protected override void Die() {
      isDead = true;
      anim.SetBool("die", true);
    }

    private void Interrupt() {
      attackMovement = false;
      anim.SetFloat("speed", 0);

      if (moveRoutine != null)
        StopCoroutine(moveRoutine);
      if (turnRoutine != null)
        StopCoroutine(turnRoutine);
    }

    protected void ToggleBlock(bool blockOn) {
      if (blockOn) lastBlockTime = Time.time;
      anim.SetBool("block", blockOn);
      if (blockOn) controlState = ControlState.Block;
      else controlState = ControlState.Locomotion;
    }

    protected void AnimateLocomotion(float speed) {
      // TODO: this should only lerp down to 0 not always
      anim.SetFloat("speed", Mathf.Lerp(anim.GetFloat("speed"), speed, locomotionTransitionDampen));
    }

    protected void AnimateDodge() {
      controlState = ControlState.Dodge;
      anim.SetTrigger("dodge");
    }

    protected void AnimateAttack() {
      anim.SetTrigger("attack");
    }

    /// ANIMATION EVENTS
    public void AttackMachineCallback(AttackState state) {
      attackState = state;

      if (attackState != AttackState.Swing) {
        attackHits.Clear();
      }
    }

    public void AttackMachineCallback(bool startStop) {
      if (startStop) {
        controlState = ControlState.Attack;
        attackMovement = true;
      } else {
        controlState = ControlState.Locomotion;
        attackMovement = false;
        anim.ResetTrigger("attack");
      }
    }

    protected void DodgeEvent(string message) {
      if (message == "start") {
        controlState = ControlState.Dodge;
        dodgeMovement = true;
      }

      if (message == "end") {
        dodgeMovement = false;
        anim.ResetTrigger("dodge");
        controlState = ControlState.Locomotion;
      }
    }

    protected void AttackEvent(string message) {
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

      // if (message == "end") {
      //   // when transitioning to dodge or hit, since it bypasses the statemachineexit on the attackmachine
      //   AttackMachineCallback(false);
      // }
    }

    protected void HitEvent(string message) {
      if (message == "start") {
        controlState = ControlState.Hit;
      }

      if (message == "end" && controlState == ControlState.Hit) {
        hitMovement = false;
        controlState = ControlState.Locomotion;
      }
    }

  }
}