using System.Collections.Generic;
using UnityEngine;

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

  [RequireComponent(typeof(Animator))]
  [RequireComponent(typeof(AudioSource))]
  public class DudeController : MonoBehaviour, CombatTarget {
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

    protected void Awake() {
      anim = GetComponent<Animator>();
      audio = GetComponent<AudioSource>();
      attackState = AttackState.End;
      attackHits = new List<CombatTarget>();
    }

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
        movementState = MovementState.Hit;
      }

      if (message == "end") {
        movementState = MovementState.Locomotion;
      }
    }
  }
}