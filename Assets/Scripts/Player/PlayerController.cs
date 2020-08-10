using System;
using System.Collections;
using System.Collections.Generic;
using ofr.grim.character;
using ofr.grim.combat;
using ofr.grim.debug;
using UnityEngine;
using UnityEngine.UI;

namespace ofr.grim.player {
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

  [RequireComponent(typeof(WeaponManager))]
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
    private WeaponManager weaponManager;

    // player control config
    [SerializeField] private LayerMask enemyLayerMask;
    [SerializeField] private LayerMask groundCheckLayer;
    private float locomotionTransitionDampen = 0.2f;
    private float turnDamped = 10f;
    private float attackTurnTime = 0.1f;
    private float moveSpeed = 5f;
    private float blockMaxMoveInput = 0.4f;
    private float rollSpeed = 12f;
    private float lockOnCastRadius = 1f;
    private float lockOnCastDistance = 3.5f;
    private float parryTime = 0.2f;
    private float deflectedProjectileDamageMultiplier = 2f;
    float gapCloseMaxReach = 2.05f;
    float gapCloseMinReach = 1.3f;
    float gapCloseSpeed = 0.15f;
    float chargeSpeedDamper = 0.5f;
    [SerializeField] private GameObject blockFX;
    [SerializeField] private GameObject parryFX;
    [SerializeField] private WeaponCollision weaponCollision;

    // This should be part of a weapon object
    // float attackDamage = 20f;
    // [SerializeField] private AudioClip swingAudio;
    // [SerializeField] private GameObject hitFX;
    // [SerializeField] private GameObject equippedWeaponPrefab;
    // end weapon config

    // state vars
    private Vector3 moveVector;
    private ControlState controlState { get; set; }
    private AttackState attackState { get; set; }
    private bool dodgeMovement = false;
    private bool attackMovement = false;
    private bool hitMovement = false;
    private bool chargingAttack = false;
    private float chargeTime = 0f;
    private bool attackIsPowerful = false;
    private float lastBlockTime;
    private int attackComboMouseBtn;
    private List<CombatTarget> attackHits;

    private Coroutine moveRoutine = null;
    private Coroutine turnRoutine = null;

    void Awake() {
      anim = GetComponent<Animator>();
      audio = GetComponent<AudioSource>();
      attackState = AttackState.End;
      attackHits = new List<CombatTarget>();
      controller = GetComponent<CharacterController>();
      weaponManager = GetComponent<WeaponManager>();
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

      if (Input.GetKeyDown(KeyCode.E)) {
        GetHit(gameObject, 0, false, blockFX);
      }

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
        CancelCharging();
        Dodge(moveInput);
        return;
      }

      if (HandleAttackButtons(0, moveInput)) {
        return;
      }

      if (HandleAttackButtons(1, moveInput)) {
        return;
      }

      if (Input.GetKey(KeyCode.LeftShift)) {
        // if (Input.GetMouseButton(1)) {
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
      if (Input.GetButtonDown("Jump") && attackState != AttackState.Swing) {
        // trigger the end event just to make sure attack state is cleared
        AttackEvent("end");
        Dodge(GetInputDirectionByCamera());
        return;
      }

      if (
        weaponManager.weapon.fireType == FireType.Repeat && Input.GetMouseButton(attackComboMouseBtn)
        || Input.GetMouseButtonDown(attackComboMouseBtn)) {
        print("combo");
        if (attackState == AttackState.Continue) {
          Attack(GetInputDirectionByMouse());
        } else if (attackState == AttackState.Swing) {
          /// Queue attack for next tick
        }
        return;
      }

      if (Input.GetKey(KeyCode.LeftShift)) {
        // if (Input.GetMouseButton(1)) {
        if (attackState != AttackState.Swing) ToggleBlock(true);
      }
    }

    private void HandleBlockControl() {
      Vector3 moveInput = GetInputDirectionByCamera();
      HandleTurning(moveInput);
      HandleMoving(Vector3.ClampMagnitude(moveInput, blockMaxMoveInput));

      if (!Input.GetKey(KeyCode.LeftShift)) {
        // if (!Input.GetMouseButton(1)) {
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
      if (chargingAttack) {
        moveVector *= chargeSpeedDamper;
        inputMagnitude *= chargeSpeedDamper;
      }
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

    private bool HandleAttackButtons(int mouseBtn, Vector3 moveInput) {

      if (chargingAttack && attackComboMouseBtn == mouseBtn) {
        if (Input.GetMouseButton(mouseBtn)) {
          HandleCharging(moveInput);
        } else {
          Attack(GetInputDirectionByMouse());
          return true;
        }
        return false;
      }

      if (Input.GetMouseButton(mouseBtn)) {
        EquipWeapon(mouseBtn == 0 ? 1 : 2);
        attackComboMouseBtn = mouseBtn;
        if (weaponManager.weapon.fireType == FireType.Charge) {
          StartCharging();
        } else {
          Attack(GetInputDirectionByMouse());
          return true;
        }
      }

      return false;
    }

    private void StartCharging() {
      print("charging attack");
      chargingAttack = true;
      chargeTime = 0f;
      anim.SetBool("chargeAttack", true);
    }

    private void CancelCharging() {
      chargingAttack = false;
      chargeTime = 0f;
      anim.SetBool("chargeAttack", false);
    }

    private void HandleCharging(Vector3 moveInput) {
      chargeTime += Time.deltaTime;
      if (moveInput.magnitude == 0)
        HandleTurning(GetInputDirectionByMouse());
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

        if (
          weaponManager.weapon.type == AttackType.Melee
          && lockDir.magnitude > gapCloseMaxReach
        ) {
          moveRoutine = StartCoroutine(HandleMovingAsync(transform.position + Vector3.ClampMagnitude(lockDir, (lockDir.magnitude - gapCloseMinReach)), gapCloseSpeed));
        }
      } else {
        turnRoutine = StartCoroutine(HandleTurningAsync((transform.position + castDirection) - transform.position, attackTurnTime));
      }

      if (weaponManager.weapon.fireType == FireType.Charge && weaponManager.weapon.chargeTime < chargeTime) {
        print("Power attack");
        attackIsPowerful = true;
      } else {
        attackIsPowerful = false;
      }

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

    private Vector3 GetInputDirectionByMouse() {
      Ray mouseRay = mainCam.ScreenPointToRay(Input.mousePosition);
      Plane clickPlane = new Plane(Vector3.up, transform.position);

      clickPlane.Raycast(mouseRay, out float hitDist);

      return mouseRay.GetPoint(hitDist) - transform.position;
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

    private void FireMeleeAttack(WeaponCollider collider) {
      Collider[] hits = Physics.OverlapSphere(collider.transform.position, collider.radius, enemyLayerMask);

      foreach (Collider hit in hits) {
        CombatTarget tgt = hit.GetComponent<CombatTarget>();

        if (tgt != null && !attackHits.Exists((t) => GameObject.ReferenceEquals(t, tgt))) {
          attackHits.Add(tgt);
          tgt.GetHit(gameObject, weaponManager.weapon.attackDamage, attackIsPowerful, weaponManager.weapon.hitFX);

        }
      }
    }

    private void FireRangedAttack() {

    }

    public override void GetHit(GameObject hitter, float damage, bool isPowerful, GameObject fx) {
      if (isDead || controlState == ControlState.Dodge) return;
      Vector3 hitterPosition = hitter.transform.position;
      Vector3 hitDir = (hitterPosition - transform.position).normalized;
      hitDir.y = 0f;

      if (controlState == ControlState.Block && !isPowerful) {
        turnRoutine = StartCoroutine(HandleTurningAsync(hitDir, attackTurnTime));
        if (lastBlockTime + parryTime > Time.time) {
          // parry
          EnemyController enemy = hitter.GetComponent<EnemyController>();
          EnemyProjectile projectile = hitter.GetComponent<EnemyProjectile>();

          if (enemy != null) {
            enemy.GetParried(gameObject);
            Destroy(Instantiate(parryFX, transform.position + (Vector3.up * 1.5f), transform.rotation), 2f);
          } else if (projectile != null) {
            EnemyProjectile deflected = Instantiate(projectile) as EnemyProjectile;
            deflected.Deflect(projectile, deflectedProjectileDamageMultiplier);
          } else {
            // block hit
            anim.SetTrigger("hit");
            Destroy(Instantiate(blockFX, transform.position + (Vector3.up * 1.5f), transform.rotation), 2f);
          }
        } else {
          // block hit
          anim.SetTrigger("hit");
          Destroy(Instantiate(blockFX, transform.position + (Vector3.up * 1.5f), transform.rotation), 2f);
        }
      } else {
        Interrupt();
        ToggleBlock(false);
        TakeDamage(damage);
        weaponManager.Unequip();
        controlState = ControlState.Hit;
        hitMovement = true;
        Vector3 hitAnimDir = transform.InverseTransformDirection(hitDir);
        if (isPowerful) hitAnimDir *= 2;

        anim.SetTrigger("hit");
        anim.SetFloat("hitX", hitAnimDir.x);
        anim.SetFloat("hitY", hitAnimDir.z);
        Destroy(Instantiate(fx, transform.position + (Vector3.up * 1.5f), transform.rotation), 2f);
        // if (isPowerful)
        //   turnRoutine = StartCoroutine(HandleTurningAsync(hitDir, attackTurnTime));
        // controller.Move(hitDir * -0.1f);
        // hitMovement = isPowerful;
        // anim.SetBool("bigHit", isPowerful);
        // temp

      }
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
      if (weaponManager.weapon == null) {
        EquipWeapon(1);
      }
      anim.SetBool("block", blockOn);
      if (blockOn) {
        controlState = ControlState.Block;
        lastBlockTime = Time.time;

        // if (weapon == null) weapon = weaponManager.Equip(1);
      } else controlState = ControlState.Locomotion;
    }

    protected void EquipWeapon(int wepIdx) {
      weaponManager.Equip(wepIdx);
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
      chargingAttack = false;
      anim.SetBool("chargeAttack", false);
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
        audio.PlayOneShot(weaponManager.weapon.swingAudio);
      }

      if (message.Contains("projectile")) {
        FireRangedAttack();
      }

      if (message.Contains("collide")) {
        string[] split = message.Split('.');

        if (split.Length > 1) {
          switch (split[1]) {
            case "left":
              FireMeleeAttack(weaponCollision.left);
              break;
            case "right":
              FireMeleeAttack(weaponCollision.right);
              break;
            default:
              FireMeleeAttack(weaponCollision.front);
              break;
          }
        } else {
          FireMeleeAttack(weaponCollision.front);
        }
      }
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