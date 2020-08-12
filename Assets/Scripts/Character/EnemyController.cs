﻿using System.Collections;
using System.Collections.Generic;
using ofr.grim.combat;
using ofr.grim.core;
using ofr.grim.utils;
using UnityEngine;
using UnityEngine.AI;

namespace ofr.grim.character {
  public enum AIState {
    Idle,
    Patrol,
    Combat,
    Reset
  }

  public enum EnemyType {
    Melee,
    Ranged
  }

  // require enemy ui
  // require enemyAttackController

  public class EnemyController : CombatTarget {
    private NavMeshAgent navAgent;
    private Rigidbody rBody;
    private Animator anim;
    private AudioSource audio;
    [SerializeField] private EnemyIcons icons;

    [SerializeField] private EnemyType type;
    public float attemptAttackTime = 4f;
    public float repeatAttackCooldown = 2f;
    public float aggroRadius = 8f;
    public float chaseRadius = 12f;
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
    public bool bigBoy = false;

    private float attackGiveUpTime;
    private float repeatAttackTime;
    private Attack currentAttack;
    protected List<CombatTarget> attackHits;
    private bool isAttacking = false;
    private bool isStaggering = false;
    private bool attemptingAttack = false;
    private bool inAttackQueue = false;

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
      return;
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
        navAgent.Move(anim.deltaPosition);
      }

      if (isAttacking)
        HandleTurningToPlayer(attackingTurnSpeed);
    }

    public void AttemptAttack() {
      attemptingAttack = true;
      attackGiveUpTime = Time.time + attemptAttackTime;
    }

    public void GiveUpAttack() {
      attemptingAttack = true;
    }

    public EnemyType GetType() {
      return type;
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
      // TODO: this whole logic tree is a mess
      if (isAttacking || isStaggering) {
        return;
      }

      if (!CheckForPlayerDistance(chaseRadius)) {
        ResetAggro();
        return;
      } else if (CheckForPlayerDistance(aggroRadius) && CheckForPlayerLOS(chaseRadius)) {
        // In combat position
        if (!inAttackQueue && repeatAttackTime < Time.time) {
          inAttackQueue = GameManager.enemyManager.EnterAttackQueue(this);
        }

        if (attemptingAttack && attackGiveUpTime > Time.time) {
          // move to attack range and attack
          if (CheckForPlayerDistance(attackRadius))
            AttackPlayer();
          else
            MoveTo(GameManager.player.gameObject.transform.position);
        } else {
          if (attemptingAttack) EndAttack();

          // waiting for turn to attack
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
      StartCoroutine(HandleStoppingAsync(attackTurnTime));
    }

    private void AttackPlayer() {
      if (isAttacking) return;
      StartAttack();

      Vector3 lookDir = GameManager.player.transform.position - transform.position;
      lookDir.y = 0f;
      if (navAgent.velocity.sqrMagnitude > 0f)
        StartCoroutine(HandleStoppingAsync(attackTurnTime));
      StartCoroutine(HandleTurningAsync(lookDir, attackTurnTime));

      currentAttack = attacks.GetByRandomSeed(Random.Range(0f, 1f));
      anim.SetFloat("attackType", currentAttack.animId);
      anim.SetTrigger("attack");
      if (currentAttack.isPowerful) {
        icons.PowerAttack(true);
      } else {
        icons.NormalAttack(true);
      }
    }

    private void MoveTo(Vector3 targetPos) {
      navAgent.SetDestination(targetPos);
    }

    public override void GetHit(GameObject hitter, float damage, bool powerful, GameObject fx) {
      if (isDead) return;
      Vector3 hitterPosition = hitter.transform.position;

      if (powerful) {
        print("BIG HIT");
      }

      TakeDamage(damage);
      Destroy(Instantiate(fx, transform.position + (Vector3.up * 1.5f), transform.rotation), 2f);

      if ((!(isAttacking && currentAttack.isPowerful) && !bigBoy) || isDead) {
        Stagger(hitterPosition, false);
      }

    }

    // NOTE: this is called from Player's GetHit, so attack is already executed
    public void GetParried(GameObject hitter) {
      // set repeatAttacktime 
      repeatAttackTime = Time.time + repeatAttackCooldown;

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
    }

    private bool CheckForPlayerDistance(float radius) {
      if (Vector3.Distance(GameManager.player.transform.position, transform.position) <= radius)
        return true;
      else
        return false;
    }

    private bool CheckForPlayerLOS(float distance) {
      bool hitSomething = Physics.Raycast(
        transform.position + Vector3.up,
        (GameManager.player.transform.position + Vector3.up) - (transform.position + Vector3.up),
        out RaycastHit hit,
        distance,
        type == EnemyType.Ranged ? LayerMask.NameToLayer("Enemy") : Physics.DefaultRaycastLayers
      );

      if (hitSomething && hit.collider.tag == Tags.Player) {
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
      AnimateLocomotion(0, false);
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
          tgt.GetHit(gameObject, currentAttack.damage, currentAttack.isPowerful, currentAttack.hitEffect);
        }
      }
    }

    private void FireRangedAttack() {
      EnemyProjectile proj = Instantiate<EnemyProjectile>(currentAttack.projectile, transform.position + transform.forward + (Vector3.up * 1.2f), transform.rotation);
      proj.Fire(GameManager.player.transform.position - proj.transform.position, currentAttack);
    }

    protected void AnimateLocomotion(float speed, bool lerpVal = true) {
      if (lerpVal)
        anim.SetFloat("speed", speed, 0.2f, Time.deltaTime);
      else
        anim.SetFloat("speed", speed);
    }

    // /// ANIMATION EVENTS
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

    /// ANIMATION EVENTS
    public void AttackMachineCallback(bool startStop) {
      if (startStop) {
        // controlState = ControlState.Attack;
        // attackMovement = true;
      } else {
        EndAttack();

        // successful attack set repeatAttacktime
        repeatAttackTime = Time.time + repeatAttackCooldown;

        // controlState = ControlState.Locomotion;
        // attackMovement = false;
        // anim.ResetTrigger("attack");
      }
    }

    protected void AttackEvent(string message) {
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
      attemptingAttack = false;
      inAttackQueue = false;
      isAttacking = false;
      currentAttack = null;
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
      Gizmos.color = Color.green;
      Gizmos.DrawWireSphere(transform.position, aggroRadius);
      Gizmos.color = Color.white;
      Gizmos.DrawWireSphere(transform.position, chaseRadius);
      Gizmos.color = Color.red;
      Gizmos.DrawWireSphere(transform.position, attackRadius);
    }
  }
}