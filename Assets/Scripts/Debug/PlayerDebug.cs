using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ofr.grim.debug {

  public class PlayerDebug : MonoBehaviour {
    [SerializeField]
    private float lineLength = 3f;

    [SerializeField]
    private LineRenderer moveDir;
    [SerializeField]
    private LineRenderer lockDir;

    private float lifetime = 2f;
    private float expireTime;

    void Update() {
      if (Time.time >= expireTime) {
        moveDir.enabled = false;
        lockDir.enabled = false;
      }
    }

    public void UpdateMoveDir(Vector3 dir) {
      moveDir.enabled = true;
      moveDir.SetPositions(new Vector3[] {
        transform.position,
          transform.position + (dir.normalized * lineLength)
      });
      lockDir.enabled = false;
      expireTime = Time.time + lifetime;
    }

    public void UpdateMoveDir(Vector3 dir, Vector3 locked) {
      moveDir.enabled = true;
      moveDir.SetPositions(new Vector3[] {
        transform.position,
          transform.position + (dir.normalized * lineLength)
      });
      lockDir.enabled = true;
      lockDir.SetPositions(new Vector3[] {
        transform.position,
          transform.position + (locked.normalized * lineLength)
      });

      expireTime = Time.time + lifetime;
    }
  }
}