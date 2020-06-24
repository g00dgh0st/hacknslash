using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ofr.grim {
  public interface CombatTarget {
    void GetHit(Vector3 hitPosition);
  }
}