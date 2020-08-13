using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class OutlineFeature : ScriptableRendererFeature {

  [System.Serializable]
  public class OutlineSettings {
    public Material outlineMaterial = null;
  }

  public OutlineSettings settings = new OutlineSettings();
  OutlinePass outlinePass;
  RenderTargetHandle outlineTexture;

  public override void Create() {
    outlinePass = new OutlinePass(settings.outlineMaterial);
    outlinePass.renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
    outlineTexture.Init("_OutlineTexture");
  }

  // Here you can inject one or multiple render passes in the renderer.
  // This method is called when setting up the renderer once per-camera.
  public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) {
    if (settings.outlineMaterial == null) {
      Debug.LogWarningFormat("Missing Outline Material");
      return;
    }
    outlinePass.Setup(renderer.cameraColorTarget, RenderTargetHandle.CameraTarget);
    renderer.EnqueuePass(outlinePass);
  }
}