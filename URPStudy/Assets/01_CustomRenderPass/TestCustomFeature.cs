using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace URPStudy.c01 {
    public class TestCustomFeature : ScriptableRendererFeature {
        [SerializeField]
        private Material _material;

        private TestCustomPass _pass;

        public override void Create() {
            _pass = new() {
                // AfterRenderingPostProcessing: 포스트 프로세싱이 적용된 후에 실행되는 이벤트
                // AfterRenderingOpaques: 불투명한 오브젝트가 렌더링된 후에 실행되는 이벤트
                // AfterRenderingSkybox: 스카이박스가 렌더링된 후에 실행되는 이벤트
                renderPassEvent = RenderPassEvent.BeforeRenderingTransparents 
            };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) {
            if (_pass != null && _material != null) {
                _pass.material = _material;

                renderer.EnqueuePass(_pass);
            }
        }

        public class TestCustomPass : ScriptableRenderPass {
            public Material material;

            private class PassData {
                public TextureHandle source;
                public Material material;
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData) {
                // source = 현재 렌더링 중인 카메라의 최종 컬러 텍스처
                var resourceData = frameData.Get<UniversalResourceData>();
                var source = resourceData.activeColorTexture;


                // descriptor = 카메라의 설정 정보 가져오기
                var cameraData = frameData.Get<UniversalCameraData>();
                RenderTextureDescriptor descriptor = cameraData.cameraTargetDescriptor;
                descriptor.depthBufferBits = 0; // 깊이 버퍼는 필요 없으므로 0으로 설정


                // 위 source, descriptor 기반 임시 텍스처 생성
                TextureHandle texture = UniversalRenderer.CreateRenderGraphTexture(
                    renderGraph,
                    descriptor, 
                    "TestCustomPassTexture", 
                    false);


                // builder = gpu 명령을 생성하는 객체
                // RasterRenderPass : 일반적인 래스터라이즈된 렌더 패스
                // ComputeRenderPass : 컴퓨트 셰이더를 사용하는 렌더 패스
                using (var builder = renderGraph.AddRasterRenderPass<PassData>("TestCustomPass", out var passData)) {
                    passData.source = texture;
                    passData.material = material;
        
                    builder.UseTexture(source, AccessFlags.Read); // source 텍스처를 읽어와서,
                    builder.SetRenderAttachment(texture, 0, AccessFlags.Write); // 임시 텍스처를 렌더 타겟으로 설정

                    builder.SetRenderFunc((PassData data, RasterGraphContext context) => {
                        // 렌더링 작업 수행

                        Blitter.BlitTexture(context.cmd, data.source, new Vector4(1, 1, 0, 0), data.material, 0);
                    });
                }
                
                using (var builder = renderGraph.AddRasterRenderPass<PassData>("TestDrawPass", out var passData)) {
                    passData.source = texture;

                    builder.UseTexture(texture, AccessFlags.Read); // Material이 적용된 임시 텍스처를 읽어와서,
                    builder.SetRenderAttachment(source, 0, AccessFlags.Write); // 최종 렌더 타겟으로 설정

                    builder.SetRenderFunc((PassData data, RasterGraphContext context) => {
                        Blitter.BlitTexture(context.cmd, data.source, new Vector4(1, 1, 0, 0), 0.0f, false);
                    });
                }
            }
        }
    }
}
