using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace URPStudy.c01 {
    public class TestCustomFeature : ScriptableRendererFeature {
        private TestCustomPass _pass;

        public override void Create() {
            _pass = new() {
                // AfterRenderingPostProcessing: 포스트 프로세싱이 적용된 후에 실행되는 이벤트
                renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing 
            };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) {
            if (_pass != null) {
                renderer.EnqueuePass(_pass);
            }
        }

        public class TestCustomPass : ScriptableRenderPass {
            private class PassData {
                
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData) {
                using var builder = renderGraph.AddRenderPass<PassData>("TestCustomPass", out var passData);
                // 권한 설정

                builder.SetRenderFunc((PassData data, RenderGraphContext context) => {
                    // 렌더링 작업 수행
                });
            }
        }
    }
}
