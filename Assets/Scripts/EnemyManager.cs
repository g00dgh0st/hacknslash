using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ofr.grim {
  public struct EnemyQueue {
    public Queue<EnemyController> enemies;
    public EnemyType type;
    public float nextAttackTime;
    public EnemyController lastAttackedEnemy;

    public EnemyQueue(EnemyType eType) {
      this.type = eType;
      this.enemies = new Queue<EnemyController>();
      this.nextAttackTime = 0f;
      this.lastAttackedEnemy = null;
    }
  }

  public class EnemyManager : MonoBehaviour {
    private float attackCooldown = 1f;

    private EnemyQueue meleeQueue;
    private EnemyQueue rangedQueue;

    void Awake() {
      meleeQueue = new EnemyQueue(EnemyType.Melee);
      rangedQueue = new EnemyQueue(EnemyType.Ranged);
    }

    public bool EnterAttackQueue(EnemyController enemy) {
      return AddToQueue(enemy, enemy.GetType());
    }

    void Update() {
      CheckForAttack(ref meleeQueue);
      CheckForAttack(ref rangedQueue);
    }

    void OrderAttack(ref EnemyQueue queue) {
      // TODO: check if dead, then skip over?
      EnemyController enemy = queue.enemies.Dequeue();
      enemy.AttemptAttack();
      queue.nextAttackTime = Time.time + attackCooldown;
      queue.lastAttackedEnemy = enemy;
    }

    private void CheckForAttack(ref EnemyQueue queue) {
      if (queue.enemies.Count > 0 && queue.nextAttackTime < Time.time) {

        OrderAttack(ref queue);
      }
    }

    private bool AddToQueue(EnemyController enemy, EnemyType queueType) {
      Queue<EnemyController> enemies = GetQueueByType(queueType).enemies;

      if (enemies.Contains(enemy)) return false;
      enemies.Enqueue(enemy);
      return true;
    }

    private EnemyQueue GetQueueByType(EnemyType queueType) {
      return queueType == EnemyType.Ranged ? rangedQueue : meleeQueue;

    }
  }
}