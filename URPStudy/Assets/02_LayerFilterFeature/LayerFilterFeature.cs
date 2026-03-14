using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace URPStudy.c02 {
    public class LayerFilterFeature : ScriptableRendererFeature {
        public LayerMask _layerMask;
        public Material _material;

        private LayerFilterPass _pass;

        public override void Create() {
            _pass = new LayerFilterPass {
                renderPassEvent = RenderPassEvent.AfterRenderingOpaques
            };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) {
            if (_material != null) {
                _pass.layerMask = _layerMask;
                _pass.material = _material;

                renderer.EnqueuePass(_pass);
            }
        }

        public class LayerFilterPass : ScriptableRenderPass {
            public LayerMask layerMask;
            public Material material;

            private class PassData {
                public RendererListHandle rendererList;
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData) {
                var resourceData = frameData.Get<UniversalResourceData>();
                var renderingData = frameData.Get<UniversalRenderingData>();
                var cameraData = frameData.Get<UniversalCameraData>();

                var source = resourceData.activeColorTexture;

                var sortingSettings = new SortingSettings(cameraData.camera) { 
                    criteria = SortingCriteria.CommonOpaque 
                };

                var drawingSettings = new DrawingSettings(new ShaderTagId("UniversalForward"), sortingSettings) {
                    overrideMaterial = material
                };

                var filteringSettings = new FilteringSettings(RenderQueueRange.opaque, layerMask);

                var listParams = new RendererListParams(renderingData.cullResults, drawingSettings, filteringSettings);
                var rendererListHandle = renderGraph.CreateRendererList(listParams);

                using var builder = renderGraph.AddRasterRenderPass<PassData>("Test Layer Filter", out var passData);
                
                passData.rendererList = rendererListHandle;

                builder.SetRenderAttachment(source, 0, AccessFlags.Write);
                builder.UseRendererList(rendererListHandle);
                builder.SetRenderFunc((PassData data, RasterGraphContext context) => {
                    context.cmd.DrawRendererList(data.rendererList);
                });
            }
        }
    }
}