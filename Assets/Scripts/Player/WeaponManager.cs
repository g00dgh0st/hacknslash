using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ofr.grim.player {

  public class WeaponManager : MonoBehaviour {
    private PlayerController controller;
    private Animator anim;

    public List<Weapon> weapons;

    [SerializeField] private Transform leftHand;
    [SerializeField] private Transform rightHand;

    void Awake() {
      anim = GetComponent<Animator>();
      controller = GetComponent<PlayerController>();
    }

    public Weapon Equip(int idx) {
      Weapon wp = weapons[idx];

      if (wp.leftHandPrefab) {
        Instantiate(wp.leftHandPrefab, leftHand);
      }
      if (wp.rightHandPrefab) {
        Instantiate(wp.rightHandPrefab, rightHand);
      }

      anim.SetInteger("weaponId", wp.animId);

      return wp;
    }

  }
}