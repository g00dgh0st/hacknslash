using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour {
  [SerializeField]
  [Range(0f, 1f)]
  private float timeScale = 1f;

  void Awake() {
    Time.timeScale = timeScale;
  }
}