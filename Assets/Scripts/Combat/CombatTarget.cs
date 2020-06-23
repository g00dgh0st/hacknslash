using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ofr.grim {
  interface CombatTarget {
    void GetHit(Vector3 hitPosition);
  }
}