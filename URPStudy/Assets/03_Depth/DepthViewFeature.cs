using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace URPStudy.c03 {
    public class DepthViewFeature : ScriptableRendererFeature {
        public Material material;
        private DepthViewPass _pass;

        public override void Create() {
            _pass = new() {
                renderPassEvent = RenderPassEvent.AfterRenderingOpaques
            };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) {
            if (material != null) {
                _pass.material = material;
                _pass.ConfigureInput(ScriptableRenderPassInput.Depth);
                
                renderer.EnqueuePass(_pass);
            }
        }

        public class DepthViewPass : ScriptableRenderPass {
            public Material material;

            private class PassData {
                public TextureHandle source;
                public Material material;
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData) {
                var resourceData = frameData.Get<UniversalResourceData>();
                var cameraData = frameData.Get<UniversalCameraData>();

                TextureHandle sourceColor = resourceData.activeColorTexture;
                TextureHandle sourceDepth = resourceData.cameraDepthTexture;

                RenderTextureDescriptor desc = cameraData.cameraTargetDescriptor;
                desc.depthBufferBits = 0;
                TextureHandle tempTexture = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "TempDepthTexture", false);

                using (var builder = renderGraph.AddRasterRenderPass<PassData>("Draw Depth Pass", out var passData)) {
                    passData.source = sourceColor;
                    passData.material = material;

                    builder.UseTexture(sourceDepth, AccessFlags.Read);
                    
                    builder.UseTexture(sourceColor, AccessFlags.Read);
                    builder.SetRenderAttachment(tempTexture, 0, AccessFlags.Write);

                    builder.SetRenderFunc((PassData data, RasterGraphContext context) => {
                        Blitter.BlitTexture(context.cmd, data.source, new Vector4(1, 1, 0, 0), data.material, 0);
                    });
                }

                using (var builder = renderGraph.AddRasterRenderPass<PassData>("Copy Back Pass", out var passData)) {
                    passData.source = tempTexture;

                    builder.UseTexture(tempTexture, AccessFlags.Read);
                    builder.SetRenderAttachment(sourceColor, 0, AccessFlags.Write);

                    builder.SetRenderFunc((PassData data, RasterGraphContext context) => {
                        Blitter.BlitTexture(context.cmd, data.source, new Vector4(1, 1, 0, 0), 0.0f, false);
                    });
                }
            }
        }
    }
}