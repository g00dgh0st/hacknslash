using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ofr.grim.combat {

  [CreateAssetMenu(fileName = "Arsenal", menuName = "Scriptables/Arsenal", order = 0)]
  public class Arsenal : ScriptableObject {
    [SerializeField] private List<Weapon> weapons;

    public List<Weapon> GetWeapons() {
      return weapons;
    }
  }
}