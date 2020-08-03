using System.Collections;
using System.Collections.Generic;
using ofr.grim.player;
using UnityEngine;

namespace ofr.grim.core {
  [RequireComponent(typeof(EnemyManager))]
  public class GameManager : MonoBehaviour {
    [SerializeField]
    [Range(0f, 1f)]
    private float timeScale = 1f;

    public static EnemyManager enemyManager;
    public static PlayerController player;

    void Awake() {
      Time.timeScale = timeScale;
      enemyManager = GetComponent<EnemyManager>();
      player = GameObject.Find("Player").GetComponent<PlayerController>();
    }

    void Start() {}
  }
}