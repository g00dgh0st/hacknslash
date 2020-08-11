using System;
using System.Collections;
using System.Collections.Generic;
using ofr.grim.player;
using UnityEngine;

namespace ofr.grim.combat {

  public class WeaponManager : MonoBehaviour {
    private PlayerController controller;
    private Animator anim;
    [SerializeField] private Arsenal arsenal;

    // [SerializeField] private List<Weapon> weapons;
    [SerializeField] private Transform leftHand;
    [SerializeField] private Transform rightHand;

    [SerializeField] private int primaryWeaponIdx;
    [SerializeField] private int secondaryWeaponIdx;

    public Weapon weapon { get; private set; }

    public Weapon primaryWeapon { get; private set; }
    public Weapon secondaryWeapon { get; private set; }

    private List<GameObject> equippedWeaponObjects;

    void Awake() {
      equippedWeaponObjects = new List<GameObject>();
      anim = GetComponent<Animator>();
      controller = GetComponent<PlayerController>();
    }

    void Start() {
      // TEMP
      primaryWeaponIdx = 0;
      secondaryWeaponIdx = 2;

      primaryWeapon = arsenal.GetWeapons() [primaryWeaponIdx];
      secondaryWeapon = arsenal.GetWeapons() [secondaryWeaponIdx];
    }

    public void Equip(int weaponId) {
      Unequip();

      if (weaponId == 1)
        weapon = primaryWeapon;
      else
        weapon = secondaryWeapon;

      if (weapon.leftHandPrefab) {
        equippedWeaponObjects.Add(Instantiate(weapon.leftHandPrefab, leftHand));
      }

      if (weapon.rightHandPrefab) {
        equippedWeaponObjects.Add(Instantiate(weapon.rightHandPrefab, rightHand));
      }

      anim.SetFloat("weaponId", weapon.animId);
    }

    public void Unequip() {
      foreach (GameObject wep in equippedWeaponObjects) {
        Destroy(wep);
      }
      weapon = null;

      anim.SetFloat("weaponId", 0);
    }
  }
}