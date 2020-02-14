using System.Collections.Generic;
using UnityEngine;

namespace ofr.grim {

  [System.Serializable]
  public class AnimationClipMapping {
    public string name;
    public AnimationClip animation;
  }

  [CreateAssetMenu(fileName = "AnimationLibrary", menuName = "HacknSlash/AnimationLibrary", order = 0)]
  public class AnimationLibrary : ScriptableObject {
    [SerializeField]
    public AnimationClipMapping[] animationClips;
    public Dictionary<string, AnimationClip> animationLookup;

    public void BuildLookup() {
      this.animationLookup = new Dictionary<string, AnimationClip>(animationClips.Length);
      foreach (AnimationClipMapping map in animationClips) {
        this.animationLookup.Add(map.name, map.animation);
      }
    }
  }
}