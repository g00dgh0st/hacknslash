using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ofr.grim {
  public class EnemyIcons : MonoBehaviour {
    [SerializeField]
    private GameObject attackIcon;
    [SerializeField]
    private GameObject powerAttackIcon;

    public void PowerAttack(bool onOff) {
      powerAttackIcon.SetActive(onOff);
    }

    public void NormalAttack(bool onOff) {
      attackIcon.SetActive(onOff);
    }

    public void DisableIcons() {
      attackIcon.SetActive(false);
      powerAttackIcon.SetActive(false);
    }
  }
}