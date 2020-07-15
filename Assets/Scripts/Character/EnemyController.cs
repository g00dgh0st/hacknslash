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
    [SerializeField] private EnemyIcons icons;

    public float aggroRadius = 8f;
    public float chaseRadius = 12f;
    public float combatRadius = 4f;
    public float attackRadius = 2f;
    private float attackTurnTime = 0.2f;
    private float attackingTurnSpeed = 70f;
    private float combatIdleTurnSpeed = 100f;
    [SerializeField] private Transform startPosition;
    [SerializeField] private WeaponCollision weaponCollision;

    private AIState state = AIState.Idle;
    private Vector3 resetPosition;

    // Attack config
    [SerializeField] private AttackSet attacks;
    public float attackCooldown = 2f;
    public bool bigBoy = false;

    private Attack currentAttack;
    protected List<CombatTarget> attackHits;
    private bool isAttacking = false;
    private bool isStaggering = false;
    private float nextAttackTime = 0f;
    private float attackGiveUpTime = 0f;

    void Awake() {
      anim = GetComponent<Animator>();
      audio = GetComponent<AudioSource>();
      attackHits = new List<CombatTarget>();
      navAgent = GetComponent<NavMeshAgent>();
      rBody = GetComponent<Rigidbody>();
    }

    new void Start() {
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
      if (isAttacking || isStaggering) {
        // attack animations only atm
        transform.position += anim.deltaPosition;
      }

      if (isAttacking)
        HandleTurningToPlayer(attackingTurnSpeed);
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
      if (isAttacking || isStaggering) {
        return;
      }

      if (!CheckForPlayerDistance(chaseRadius)) {
        ResetAggro();
        return;
      } else if (CheckForPlayerDistance(combatRadius) && CheckForPlayerLOS(chaseRadius)) {
        if (nextAttackTime == 0f)
          nextAttackTime = Time.time + attackCooldown;
        // In combat position
        if (nextAttackTime < Time.time) {
          if (CheckForPlayerDistance(attackRadius))
            AttackPlayer();
          else
            MoveTo(GameManager.player.gameObject.transform.position);
        } else {
          if (navAgent.velocity.sqrMagnitude > 0f)
            StartCoroutine(HandleStoppingAsync(attackTurnTime));
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
      if (isAttacking) return;
      StartAttack();

      Vector3 lookDir = GameManager.player.transform.position - transform.position;
      lookDir.y = 0f;
      if (navAgent.velocity.sqrMagnitude > 0f)
        StartCoroutine(HandleStoppingAsync(attackTurnTime));
      StartCoroutine(HandleTurningAsync(lookDir, attackTurnTime));

      //// TEMP: need way to configure all attacks instead of random
      currentAttack = attacks.GetByRandomSeed(Random.Range(0f, 1f));
      anim.SetInteger("attackType", currentAttack.animId);
      anim.SetTrigger("attack");
      if (currentAttack.isPowerul) {
        icons.PowerAttack(true);
      } else {
        icons.NormalAttack(true);
      }
    }

    private void MoveTo(Vector3 targetPos) {
      navAgent.SetDestination(targetPos);
    }

    public override bool GetHit(GameObject hitter, float damage, bool powerful, GameObject fx) {
      if (isDead) return false;
      Vector3 hitterPosition = hitter.transform.position;

      TakeDamage(damage);
      Destroy(Instantiate(fx, transform.position + (Vector3.up * 1.5f), transform.rotation), 2f);

      if ((!(isAttacking && currentAttack.isPowerul) && !bigBoy) || isDead) {
        Stagger(hitterPosition, false);
      }

      return true;
    }

    // NOTE: this is called from Player's GetHit, so attack is already executed
    public void GetParried(GameObject hitter) {
      Stagger(hitter.transform.position, true);
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
      StopAllCoroutines();
    }

    private void Stagger(Vector3 hitterPosition, bool bigHit) {
      Interrupt();
      anim.SetTrigger("hit");
      anim.SetBool("bigHit", bigBoy ? false : bigHit);
      transform.rotation = Quaternion.LookRotation(hitterPosition - transform.position);
      // StartCoroutine(HandleMovingAsync(transform.position - (transform.forward * 0.5f), 0.1f));
    }

    private bool CheckForPlayerDistance(float radius) {
      if (Vector3.Distance(GameManager.player.transform.position, transform.position) <= radius)
        return true;
      else
        return false;
    }

    private bool CheckForPlayerLOS(float distance) {
      bool hitSomething = Physics.Raycast(transform.position + Vector3.up, (GameManager.player.transform.position + Vector3.up) - (transform.position + Vector3.up), out RaycastHit hit, distance, LayerMask.NameToLayer("Enemy"));

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

      navAgent.ResetPath();
      AnimateLocomotion(0);
    }

    // TODO: duped with player controller
    private void FireMeleeAttack(WeaponCollider collider) {
      Collider[] hits = Physics.OverlapSphere(collider.transform.position, collider.radius);

      foreach (Collider hit in hits) {
        CombatTarget tgt = hit.GetComponent<CombatTarget>();

        // handle if they get interrupted during this call
        if (!isAttacking)
          return;

        if (tgt == null || (!currentAttack.canHitAllies && tgt.tag != "Player")) continue;

        if (tgt != this && !attackHits.Exists((t) => GameObject.ReferenceEquals(t, tgt))) {
          attackHits.Add(tgt);
          tgt.GetHit(gameObject, currentAttack.damage, currentAttack.isPowerul, currentAttack.hitEffect);
        }
      }
    }

    private void FireRangedAttack() {
      EnemyProjectile proj = Instantiate<EnemyProjectile>(currentAttack.projectile, transform.position + transform.forward + (Vector3.up * 1.2f), transform.rotation);
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
        // StartAttack();
      }

      if (message == "swing") {
        // TEMP: just trying out audio
        audio.PlayOneShot(currentAttack.audio);

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

    protected void HitEvent(string message) {
      if (message == "start") {
        StartStagger();
      }

      if (message == "end") {
        EndStagger();
      }
    }

    private void StartAttack() {
      isAttacking = true;
    }

    private void EndAttack() {
      isAttacking = false;
      currentAttack = null;
      nextAttackTime = Time.time + attackCooldown;
      attackHits.Clear();
      anim.ResetTrigger("attack");
      icons.DisableIcons();
    }

    private void StartStagger() {
      isStaggering = true;
    }

    private void EndStagger() {
      isStaggering = false;
    }

    /// DEbug stuff
    private void OnDrawGizmosSelected() {
      Gizmos.color = Color.white;
      Gizmos.DrawWireSphere(transform.position, aggroRadius);
      Gizmos.color = Color.green;
      Gizmos.DrawWireSphere(transform.position, chaseRadius);
      Gizmos.color = Color.yellow;
      Gizmos.DrawWireSphere(transform.position, combatRadius);
      Gizmos.color = Color.red;
      Gizmos.DrawWireSphere(transform.position, attackRadius);
    }
  }
}