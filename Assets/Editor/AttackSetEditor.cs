using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ofr.grim.editor {

  [CustomEditor(typeof(AttackSet))]
  public class AttackSetEditor : Editor {
    public override void OnInspectorGUI() {
      base.OnInspectorGUI();

      // serializedObject.Update();

      // EditorGUILayout.PropertyField(serializedObject.FindProperty("attacks"));

      // if (serializedObject.hasModifiedProperties) {
      //   Debug.Log("fsdfdsfdsf");
      //   List<AttackSetItem> attacks = (target as AttackSet).attacks;
      //   attacks.Sort((x, y) => { return x.weight > y.weight ? 1 : -1; });
      //   (target as AttackSet).attacks = attacks;

      //   serializedObject.ApplyModifiedProperties();
      // }

    }

    public void OnDisable() {
      (target as AttackSet).SortAttacks();
    }
  }
}