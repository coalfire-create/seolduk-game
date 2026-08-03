using UnityEngine;

namespace Persuasion.Core
{
    /// <summary>
    /// 씬에 카메라가 없어 "No cameras rendering"이 뜨는 문제를 런타임에서 근본적으로 방지한다.
    /// 씬 상태(에디터 stale/빌드 누락 등)와 무관하게, 플레이 시작 시 활성 카메라가 하나도 없으면
    /// 자동으로 Main Camera를 생성한다. 에디터 Play·빌드 양쪽 모두에서 동작한다.
    /// </summary>
    public static class RuntimeBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void EnsureRunInBackground()
        {
            // 에디터 포커스 상실 시 코루틴(UnityWebRequest 포함)이 멈추는 문제 방지
            Application.runInBackground = true;

#if UNITY_WEBGL && !UNITY_EDITOR
            // WebGL: Unity가 document 전역에서 모든 키(백스페이스·스페이스 포함)를 가로채면
            // HTML 입력 오버레이(한글 IME textarea)로 키가 전달되지 않는다.
            // 캔버스가 포커스일 때만 Unity가 키를 받도록 하여 textarea 입력(지우기/띄어쓰기/영어/한글)을 정상화한다.
            WebGLInput.captureAllKeyboardInput = false;
#endif
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureCamera()
        {
            // 이미 활성 카메라가 있으면 아무것도 하지 않는다.
            if (Camera.main != null) return;
            if (Object.FindFirstObjectByType<Camera>() != null) return;

            var go = new GameObject("Main Camera (auto)");
            go.tag = "MainCamera";
            var cam = go.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.05f, 0.05f, 0.08f, 1f);
            cam.depth = -1;

            if (Object.FindFirstObjectByType<AudioListener>() == null)
                go.AddComponent<AudioListener>();

            Debug.Log("[Persuasion] RuntimeBootstrap: 씬에 카메라가 없어 자동으로 Main Camera를 생성했습니다.");
        }
    }
}
