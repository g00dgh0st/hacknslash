using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ofr.grim {
  public class GameManager : MonoBehaviour {
    [SerializeField]
    [Range(0f, 1f)]
    private float timeScale = 1f;

    public static PlayerController player;

    void Awake() {
      Time.timeScale = timeScale;
    }

    void Start() {
      GameManager.player = GameObject.Find("Player").GetComponent<PlayerController>();
    }
  }
}