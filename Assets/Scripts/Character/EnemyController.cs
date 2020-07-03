using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace ofr.grim {
  public enum AIState {
    Idle,
    Patrol,
    Combat,
    Reset
  }

  // require enemy ui
  // require enemyAttackController

  public class EnemyController : CombatTarget {
    private NavMeshAgent navAgent;
    private Rigidbody rBody;
    private Animator anim;
    private AudioSource audio;

    public float aggroRadius = 8f;
    public float chaseRadius = 10f;
    public float attackRadius = 3f;
    public float attackTurnTime = 0.2f;
    public float attackingTurnSpeed = 45f;
    public float combatIdleTurnSpeed = 100f;
    [SerializeField] private Transform startPosition;
    [SerializeField] private WeaponCollision weaponCollision;

    private AIState state = AIState.Idle;
    private Vector3 resetPosition;

    // Attack config
    public float attackCooldown = 2f;
    [SerializeField] private Attack[] attacks;

    public float attackDamage = 10f;
    [SerializeField] private AudioClip swingAudio;
    [SerializeField] protected GameObject hitFX;

    public bool canHitAllies = false;
    public bool bigBoy = false;
    protected List<CombatTarget> attackHits;

    private bool isAttacking = false;
    private bool isPowerfulAttacking = false;
    private float nextAttackTime = 0f;

    //TEMP:::
    public GameObject powerAttackIcon;
    public EnemyProjectile projectile;

    void Awake() {
      anim = GetComponent<Animator>();
      audio = GetComponent<AudioSource>();
      attackHits = new List<CombatTarget>();
      navAgent = GetComponent<NavMeshAgent>();
      rBody = GetComponent<Rigidbody>();
    }

    new void Start() {
      powerAttackIcon.SetActive(false);

      base.Start();

      if (startPosition)
        resetPosition = startPosition.position;
      else
        resetPosition = transform.position;
    }

    void Update() {
      if (isDead) return;

      switch (state) {
        case AIState.Combat:
          HandleCombatControl();
          break;
        case AIState.Idle:
          HandleIdleControl();
          break;
        case AIState.Patrol:
          break;
        case AIState.Reset:
          HandleResetControl();
          break;
      }
    }

    void OnAnimatorMove() {
      if (isAttacking) {
        // attack animations only atm
        transform.position += anim.deltaPosition;
        HandleTurningToPlayer(attackingTurnSpeed);
      }
    }

    private void HandleResetControl() {
      if (CheckForNavAgentReachedDestination()) {
        ResetIdle();
      }

      AnimateLocomotion(GetAnimatorSpeed());
    }

    private void HandleIdleControl() {
      if (CheckForPlayerDistance(aggroRadius)) {
        StartCombat();
      }
    }

    private void HandleCombatControl() {
      if (isAttacking) {
        return;
      }

      if (!CheckForPlayerDistance(chaseRadius)) {
        ResetAggro();
        return;
      } else if (CheckForPlayerDistance(attackRadius) && CheckForPlayerLOS(attackRadius)) {
        if (nextAttackTime < Time.time) {
          AttackPlayer();
        } else {
          HandleTurningToPlayer(combatIdleTurnSpeed);
        }
      } else {
        MoveTo(GameManager.player.gameObject.transform.position);
      }

      AnimateLocomotion(GetAnimatorSpeed());
    }

    private void StartCombat() {
      state = AIState.Combat;
    }

    private void ResetAggro() {
      // TODO: RESET ALL ANIMS
      state = AIState.Reset;
      Interrupt();
      navAgent.SetDestination(resetPosition);
    }

    private void ResetIdle() {
      state = AIState.Idle;
      Interrupt();
      AnimateLocomotion(0);
    }

    private void AttackPlayer() {
      Vector3 lookDir = GameManager.player.transform.position - transform.position;
      lookDir.y = 0f;
      if (navAgent.velocity.sqrMagnitude > 0f)
        StartCoroutine(HandleStoppingAsync(attackTurnTime));
      StartCoroutine(HandleTurningAsync(lookDir, attackTurnTime));

      //// TEMP: need way to configure all attacks instead of random
      int atIdx = Random.Range(0f, 1f) > 0.8f ? 1 : 0;
      Attack atk = attacks[atIdx];
      anim.SetInteger("attackType", atk.id);
      anim.SetTrigger("attack");
      isPowerfulAttacking = atk.isPowerul;
      powerAttackIcon.SetActive(atk.isPowerul);
      nextAttackTime = Time.time + attackCooldown;
    }

    private void MoveTo(Vector3 targetPos) {
      navAgent.isStopped = false;
      navAgent.SetDestination(targetPos);
    }

    public override bool GetHit(Vector3 hitterPosition, float damage, bool powerful, GameObject fx) {
      if (isDead) return false;

      if (!isPowerfulAttacking && !bigBoy) {
        Interrupt();
        anim.SetTrigger("hit");
        transform.rotation = Quaternion.LookRotation(hitterPosition - transform.position);
        StartCoroutine(HandleMovingAsync(transform.position - (transform.forward * 0.5f), 0.1f));
      }

      TakeDamage(damage);
      Destroy(Instantiate(fx, transform.position + (Vector3.up * 1.5f), transform.rotation), 2f);

      return true;
    }

    protected override void Die() {
      isDead = true;
      anim.SetBool("die", true);
      navAgent.enabled = false;
      GetComponent<Collider>().enabled = false;
    }

    private void Interrupt() {
      EndAttack();
      navAgent.velocity = Vector3.zero;
      nextAttackTime = Time.time + attackCooldown;
      StopAllCoroutines();
    }

    private bool CheckForPlayerDistance(float radius) {
      if (Vector3.Distance(GameManager.player.transform.position, transform.position) <= radius)
        return true;
      else
        return false;
    }

    private bool CheckForPlayerLOS(float distance) {
      bool hitSomething = Physics.Raycast(transform.position + Vector3.up, (GameManager.player.transform.position + Vector3.up) - (transform.position + Vector3.up), out RaycastHit hit, distance);

      if (hitSomething && hit.collider.tag == "Player") {
        return true;
      }

      return false;
    }

    private bool CheckForNavAgentReachedDestination() {
      if (!navAgent.pathPending) {
        if (navAgent.remainingDistance <= navAgent.stoppingDistance) {
          if (!navAgent.hasPath || navAgent.velocity.sqrMagnitude == 0f) {
            return true;
          }
        }
      }
      return false;
    }

    private float GetAnimatorSpeed() {
      return navAgent.velocity.magnitude / navAgent.speed;
    }

    private void HandleTurningToPlayer(float turnSpeed) {
      Quaternion targetRotation = Quaternion.LookRotation(GameManager.player.transform.position - transform.position);
      transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
    }

    // NOTE: duped with player
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

    private IEnumerator HandleMovingAsync(Vector3 targetPosition, float timeToReach) {
      Vector3 startPos = transform.position;
      float timeTaken = 0f;
      float turnT = 0f;

      while (turnT <= 1f) {
        timeTaken += Time.deltaTime;
        turnT = timeTaken / timeToReach;
        transform.position = Vector3.Lerp(startPos, targetPosition, turnT);
        yield return true;
      }
    }

    private IEnumerator HandleStoppingAsync(float stopTime) {
      Vector3 startVelocity = navAgent.velocity;
      float timeTaken = 0f;
      float stopT = 0f;

      while (stopT <= 1f) {
        timeTaken += Time.deltaTime;
        stopT = timeTaken / stopTime;
        navAgent.velocity = Vector3.Lerp(startVelocity, Vector3.zero, stopT);
        AnimateLocomotion(GetAnimatorSpeed());
        yield return true;
      }

      navAgent.isStopped = true;
      AnimateLocomotion(0);
    }

    // TODO: duped with player controller
    private void FireMeleeAttack(WeaponCollider collider) {
      Collider[] hits = Physics.OverlapSphere(collider.transform.position, collider.radius);

      foreach (Collider hit in hits) {
        CombatTarget tgt = hit.GetComponent<CombatTarget>();

        if (tgt == null || (!canHitAllies && tgt.tag != "Player")) continue;

        if (tgt != this && !attackHits.Exists((t) => GameObject.ReferenceEquals(t, tgt))) {
          attackHits.Add(tgt);
          tgt.GetHit(transform.position, attackDamage, isPowerfulAttacking, hitFX);
        }
      }
    }

    private void FireRangedAttack() {
      EnemyProjectile proj = Instantiate<EnemyProjectile>(projectile, transform.position + transform.forward + (Vector3.up * 1.2f), transform.rotation);
      proj.Fire(GameManager.player.transform.position - proj.transform.position);
    }

    protected void AnimateLocomotion(float speed) {
      anim.SetFloat("speed", speed, 0.2f, Time.deltaTime);
    }

    // protected void AnimateDodge() {
    //   movementState = MovementState.Dodge;
    //   anim.SetTrigger("dodge");
    // }

    // protected void AnimateAttack() {
    //   anim.SetTrigger("attack");
    // }

    // protected void ToggleBlock(bool blockOn) {
    //   anim.SetBool("block", blockOn);
    //   if (blockOn) movementState = MovementState.Block;
    //   else movementState = MovementState.Locomotion;
    // }

    // /// ANIMATION EVENTS
    // public void AttackMachineCallback(AttackState state) {
    //   attackState = state;

    //   if (attackState != AttackState.Swing) {
    //     attackHits.Clear();
    //   }
    // }

    // protected void DodgeEvent(string message) {
    //   if (message == "start") {
    //     dodgeMovement = true;
    //   }

    //   if (message == "end") {
    //     dodgeMovement = false;
    //     anim.ResetTrigger("dodge");
    //     movementState = MovementState.Locomotion;
    //   }
    // }

    protected void AttackEvent(string message) {
      if (message == "start") {
        StartAttack();
      }

      if (message == "swing") {
        // TEMP: just trying out audio
        audio.PlayOneShot(swingAudio);

        // TEMP: probably need a different event to reset
        attackHits.Clear();
      }

      if (message == "projectile") {
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

      if (message == "end") {
        EndAttack();
      }
    }

    // protected void HitEvent(string message) {
    //   if (message == "start") {
    //     print("HOTT");
    //     movementState = MovementState.Hit;
    //   }

    //   if (message == "end") {
    //     movementState = MovementState.Locomotion;
    //   }
    // }

    private void StartAttack() {
      isAttacking = true;
      // navAgent.enabled = false;
    }

    private void EndAttack() {
      isAttacking = false;
      isPowerfulAttacking = false;
      // navAgent.enabled = true;
      attackHits.Clear();
      anim.ResetTrigger("attack");
      powerAttackIcon.SetActive(false);
    }
  }
}