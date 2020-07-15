using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ofr.grim {

  [System.Serializable]
  public struct AttackSetItem {
    public Attack attack;
    public float weight;
  }

  [CreateAssetMenu(fileName = "AttackSet", menuName = "Scriptables/AttackSet", order = 1)]
  public class AttackSet : ScriptableObject {

    public List<AttackSetItem> attacks;

    public void SortAttacks() {
      // sort them in order by weight
      attacks.Sort((x, y) => { return x.weight > y.weight ? 1 : -1; });
    }

    public Attack GetByRandomSeed(float seed) {
      foreach (AttackSetItem atk in attacks) {
        if (seed <= atk.weight) {
          return atk.attack;
        }
      }

      return attacks[attacks.Count - 1].attack;
    }
  }
}