using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ofr.grim {

  public class Billboard : MonoBehaviour {
    private Transform camTrans;
    // Start is called before the first frame update
    void Awake() {
      camTrans = Camera.main.transform;
    }

    // Update is called once per frame
    void Update() {
      transform.LookAt(camTrans);
    }
  }
}