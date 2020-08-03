using ofr.grim.combat;
using UnityEditor;
using UnityEngine;

namespace ofr.grim.editor {

  [CustomEditor(typeof(Attack))]
  [CanEditMultipleObjects]
  public class AttackEditor : Editor {

    void OnInit() {

    }

    public override void OnInspectorGUI() {
      serializedObject.Update();

      // base.OnInspectorGUI();
      Attack atk = target as Attack;

      // EditorGUILayout.BeginHorizontal();
      EditorGUILayout.PropertyField(serializedObject.FindProperty("animId"));
      EditorGUILayout.PropertyField(serializedObject.FindProperty("audio"));
      EditorGUILayout.PropertyField(serializedObject.FindProperty("type"));
      EditorGUILayout.PropertyField(serializedObject.FindProperty("damage"));
      EditorGUILayout.PropertyField(serializedObject.FindProperty("hitEffect"));
      EditorGUILayout.PropertyField(serializedObject.FindProperty("isPowerful"));
      EditorGUILayout.PropertyField(serializedObject.FindProperty("canHitAllies"));

      if (serializedObject.FindProperty("type").enumValueIndex == 0) {
        EditorGUILayout.Space();
        EditorGUILayout.PrefixLabel("Ranged Attack Params");
        EditorGUILayout.PropertyField(serializedObject.FindProperty("projectileSpeed"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("projectile"));
      }

      serializedObject.ApplyModifiedProperties();
    }

  }
}