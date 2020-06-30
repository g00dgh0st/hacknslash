using System;
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

  public class EnemyController : CombatTarget {
    private NavMeshAgent navAgent;
    private Rigidbody rBody;
    private Animator anim;
    private AudioSource audio;

    public float aggroRadius = 8f;
    public float chaseRadius = 10f;
    public float attackRadius = 3f;
    public float attackTurnTime = 0.2f;
    private float attackCooldown = 2f;
    [SerializeField] private AudioClip swingAudio;
    [SerializeField] private WeaponCollision weaponCollision;
    [SerializeField] protected GameObject hitFX;
    [SerializeField] private bool canHitAllies = false;

    [SerializeField] private Transform startPosition;

    private AIState state = AIState.Idle;
    private Vector3 resetPosition;
    protected List<CombatTarget> attackHits;
    private bool isAttacking = false;
    private float nextAttackTime = 0f;

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
      if (isAttacking) {
        // attack animations only atm
        transform.position += anim.deltaPosition;
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
      } else if (CheckForPlayerDistance(attackRadius)) {
        AttackPlayer();
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

      if (nextAttackTime < Time.time) {
        anim.SetTrigger("attack");
        isAttacking = true;
        nextAttackTime = Time.time + attackCooldown;
      }
    }

    private void MoveTo(Vector3 targetPos) {
      navAgent.SetDestination(targetPos);
    }

    public override bool GetHit(Vector3 attackPosition, float damage = 20f) {
      if (isDead) return false;
      Interrupt();
      TakeDamage(damage);
      anim.SetTrigger("hit");
      transform.rotation = Quaternion.LookRotation(attackPosition - transform.position);
      navAgent.Move(transform.forward * -0.05f);
      return true;
    }

    protected override void Die() {
      isDead = true;
      anim.SetBool("die", true);
    }

    private void Interrupt() {
      isAttacking = false;
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
      AnimateLocomotion(0);
    }

    // TODO: duped with player controller
    private void AttackCollision(WeaponCollider collider) {
      Collider[] hits = Physics.OverlapSphere(collider.transform.position, collider.radius);

      foreach (Collider hit in hits) {
        CombatTarget tgt = hit.GetComponent<CombatTarget>();

        if (!canHitAllies && tgt.tag != "Player") continue;

        if (tgt != null && tgt != this && !attackHits.Exists((t) => GameObject.ReferenceEquals(t, tgt))) {
          attackHits.Add(tgt);
          bool didHit = tgt.GetHit(transform.position);

          if (didHit)
            Destroy(Instantiate(hitFX, hit.ClosestPoint(transform.position + Vector3.up), transform.rotation), 2f);
        }
      }
    }

    protected void AnimateLocomotion(float speed) {
      anim.SetFloat("speed", speed);
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
        isAttacking = true;
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
        isAttacking = false;
        attackHits.Clear();
        anim.ResetTrigger("attack");
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
  }
}