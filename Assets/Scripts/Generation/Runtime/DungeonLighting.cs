using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Generation.Runtime
{
    /// <summary>Spec stage 6: dims the scene's global light and gives the player a torch that wall shadows cut.</summary>
    public static class DungeonLighting
    {
        const string TorchName = "Torch";

        public static void Setup(GenerationConfig config, Transform player, bool shadows)
        {
            Shader.SetGlobalFloat("_ActorAmbient", config.ambientLight); // read by the PixelLit shader
            Shader.SetGlobalFloat("_LightPixelsPerUnit", config.lightPixelsPerUnit);
            Shader.SetGlobalFloat("_LightSteps", config.lightSteps);
            if (player != null) PixelLit.World(player.gameObject);
            foreach (var light in Object.FindObjectsByType<Light2D>(FindObjectsSortMode.None))
                if (light.lightType == Light2D.LightType.Global) light.intensity = config.ambientLight;

            if (player == null || player.Find(TorchName) != null) return;

            var go = new GameObject(TorchName);
            go.transform.SetParent(player, false);
            var torch = go.AddComponent<Light2D>();
            torch.lightType = Light2D.LightType.Point;
            torch.color = config.torchColor;
            torch.intensity = config.torchIntensity;
            torch.pointLightOuterRadius = config.torchRadius;
            torch.pointLightInnerRadius = config.torchRadius * 0.25f;
            torch.shadowsEnabled = shadows;
            torch.shadowIntensity = 1f; // fully black behind walls, so PixelLit hides what is in shadow
        }
    }
}
