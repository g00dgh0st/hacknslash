using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ofr.grim.debug {

  public class PlayerDebug : MonoBehaviour {

    [SerializeField]
    private LineRenderer aimLineCenter;

    [SerializeField]
    private LineRenderer aimLineLeft;

    [SerializeField]
    private LineRenderer aimLineRight;

    [SerializeField]
    private LineRenderer lockLine;

    private float lifetime = 2f;
    private float expireTime;

    void Update() {
      if (Time.time >= expireTime) {
        aimLineCenter.enabled = false;
        aimLineLeft.enabled = false;
        aimLineCenter.enabled = false;
        aimLineRight.enabled = false;
        lockLine.enabled = false;
      }
    }

    public void UpdateMoveLines(Vector3 castPos, Vector3 castDir, Vector3 castDirRight, float castRad, float castDist) {

      aimLineCenter.enabled = true;
      aimLineLeft.enabled = true;
      aimLineRight.enabled = true;

      aimLineCenter.SetPositions(new Vector3[] {
        castPos,
        castPos + (castDir * castDist)
      });

      Vector3 leftPos = castPos - (castDirRight * castRad);
      aimLineLeft.SetPositions(new Vector3[] {
        leftPos,
        leftPos + (castDir * castDist)
      });

      Vector3 rightPos = castPos + (castDirRight * castRad);
      aimLineRight.SetPositions(new Vector3[] {
        rightPos,
        rightPos + (castDir * castDist)
      });

      lockLine.enabled = false;
      expireTime = Time.time + lifetime;
    }

    public void UpdateLockLine(Vector3 lockedDir, float castDist) {
      lockLine.enabled = true;
      lockLine.SetPositions(new Vector3[] {
        transform.position,
          transform.position + (lockedDir.normalized * castDist)
      });

      expireTime = Time.time + lifetime;
    }
  }
}