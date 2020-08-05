using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ofr.grim.player {

  public class WeaponManager : MonoBehaviour {
    private PlayerController controller;
    private Animator anim;

    public List<Weapon> weapons;

    private List<GameObject> equippedWeapons;

    [SerializeField] private Transform leftHand;
    [SerializeField] private Transform rightHand;

    void Awake() {
      equippedWeapons = new List<GameObject>();
      anim = GetComponent<Animator>();
      controller = GetComponent<PlayerController>();
    }

    public Weapon Equip(int idx) {
      Unequip();

      Weapon wp = weapons[idx - 1];

      if (wp.leftHandPrefab) {
        equippedWeapons.Add(Instantiate(wp.leftHandPrefab, leftHand));
      }

      if (wp.rightHandPrefab) {
        equippedWeapons.Add(Instantiate(wp.rightHandPrefab, rightHand));
      }

      anim.SetFloat("weaponId", wp.animId);

      return wp;
    }

    public void Unequip() {
      foreach (GameObject wep in equippedWeapons) {
        Destroy(wep);
      }

      anim.SetFloat("weaponId", 0);
    }
  }
}