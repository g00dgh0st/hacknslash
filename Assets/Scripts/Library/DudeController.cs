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
  public class DudeController : MonoBehaviour, CombatTarget {
    protected Animator anim;

    // [SerializeField]
    // private Collider attackCollider;

    [SerializeField]
    private WeaponCollision weaponCollision;

    [SerializeField]
    protected GameObject hitFX;

    [SerializeField]
    private float locomotionTransitionDampen = 0.2f;

    [SerializeField] protected LayerMask groundCheckLayer;
    [SerializeField] protected LayerMask enemyLayerMask;

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
    protected MovementState movementState { get; set; }
    protected AttackState attackState { get; set; }

    protected bool dodgeMovement = false;
    protected bool attackMovement = false;
    // protected bool attackCollisionOn = false;

    protected void Awake() {
      anim = GetComponent<Animator>();
      attackState = AttackState.End;
    }

    // public void AttackCollide(Collider target) {
    //   // TODO: tags could be a protected array of tags?
    //   if (target.tag == "Enemy") {
    //     target.GetComponent<DudeController>().GetHit(transform.position);
    //     Destroy(Instantiate(hitFX, target.ClosestPoint(transform.position + Vector3.up), target.transform.rotation), 2f);
    //   } else if (target.tag == "Hittable") {
    //     // stuff
    //   }
    // }
    private void AttackCollision(WeaponCollider collider) {
      Collider[] hits = Physics.OverlapSphere(collider.transform.position, collider.radius, enemyLayerMask);

      foreach (Collider hit in hits) {
        CombatTarget tgt = hit.GetComponent<CombatTarget>();

        if (tgt != null) {
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

      // if (message == "collideOn") {
      //   // attackCollider.enabled = true;
      //   attackCollisionOn = true;
      // }

      // if (message == "collideOff") {
      //   // attackCollider.enabled = false;
      //   attackCollisionOn = false;
      // }

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