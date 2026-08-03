#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Persuasion.EditorTools
{
    /// <summary>
    /// WebGL 빌드 완료 후 자동 실행.
    /// 1) Netlify용 _headers 파일 생성 (Brotli Content-Encoding)
    /// 2) index.html에 GA4 추적 스크립트 삽입
    /// 3) privacy.html 복사
    /// </summary>
    public class PostBuildProcessor : IPostprocessBuildWithReport
    {
        public int callbackOrder => 0;

        // GA4 측정 ID — 실제 ID로 교체하세요 (예: G-ABCDEF1234)
        private const string GA4_MEASUREMENT_ID = "G-XXXXXXXXXX";

        // 개인정보처리방침 소스 경로 (프로젝트 폴더 상대)
        private const string PrivacySrcRelative = "../../WebGL-Build/privacy.html";

        public void OnPostprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.WebGL) return;

            string outputPath = report.summary.outputPath;
            Debug.Log($"[PostBuild] WebGL 빌드 후처리 시작: {outputPath}");

            WriteHeaders(outputPath);
            InjectGA4(outputPath);
            InjectKeyboardFix(outputPath);
            CopyPrivacyHtml(outputPath);

            Debug.Log("[PostBuild] 후처리 완료.");
        }

        // ── 1) Netlify _headers ──────────────────────────────────────
        private static void WriteHeaders(string buildPath)
        {
            string headersPath = Path.Combine(buildPath, "_headers");
            string content =
@"/Build/*.data.br
  Content-Type: application/octet-stream
  Content-Encoding: br
/Build/*.wasm.br
  Content-Type: application/wasm
  Content-Encoding: br
/Build/*.framework.js.br
  Content-Type: application/javascript
  Content-Encoding: br
/Build/*.loader.js
  Content-Type: application/javascript
";
            File.WriteAllText(headersPath, content);
            Debug.Log($"[PostBuild] _headers 생성: {headersPath}");
        }

        // ── 2) GA4 삽입 ─────────────────────────────────────────────
        private static void InjectGA4(string buildPath)
        {
            if (GA4_MEASUREMENT_ID == "G-XXXXXXXXXX")
            {
                Debug.LogWarning("[PostBuild] GA4 측정 ID가 설정되지 않았습니다. PostBuildProcessor.cs의 GA4_MEASUREMENT_ID를 교체하세요.");
                return;
            }

            string indexPath = Path.Combine(buildPath, "index.html");
            if (!File.Exists(indexPath))
            {
                Debug.LogWarning($"[PostBuild] index.html 없음: {indexPath}");
                return;
            }

            string html = File.ReadAllText(indexPath);
            const string marker = "<!-- GA4 -->";
            if (html.Contains(marker)) return; // 이미 삽입됨

            string ga4Script =
$@"<!-- GA4 -->
    <script async src=""https://www.googletagmanager.com/gtag/js?id={GA4_MEASUREMENT_ID}""></script>
    <script>
      window.dataLayer = window.dataLayer || [];
      function gtag(){{dataLayer.push(arguments);}}
      gtag('js', new Date());
      gtag('config', '{GA4_MEASUREMENT_ID}');
    </script>
    ";

            html = html.Replace("</head>", ga4Script + "</head>");
            File.WriteAllText(indexPath, html);
            Debug.Log($"[PostBuild] GA4 삽입 완료 (ID: {GA4_MEASUREMENT_ID})");
        }

        // ── 3) 키보드 픽스 (스페이스바·백스페이스 브라우저 가로채기 방지) ────
        private static void InjectKeyboardFix(string buildPath)
        {
            string indexPath = Path.Combine(buildPath, "index.html");
            if (!File.Exists(indexPath)) return;

            string html = File.ReadAllText(indexPath);
            const string marker = "<!-- KEYBOARD-FIX -->";
            if (html.Contains(marker)) return;

            const string fix = @"<!-- KEYBOARD-FIX -->
    <script>
      (function() {
        var BLOCK = ['Space','Backspace','ArrowUp','ArrowDown','ArrowLeft','ArrowRight','Tab'];
        function onKey(e) {
          var el = document.activeElement;
          if (el && (el.id === 'kor-ime' || el.tagName === 'TEXTAREA' || el.tagName === 'INPUT')) return;
          var wrap = document.getElementById('kor-ime-wrap');
          if (wrap && wrap.style.display !== 'none') return;
          if (BLOCK.indexOf(e.code) !== -1) e.preventDefault();
        }
        window.addEventListener('keydown', onKey, true);
        window.addEventListener('keyup',   onKey, true);
      })();

      // AUDIO-FIX
      (function() {
        function unlockAudio() {
          if (window.unityInstance && window.unityInstance.Module && window.unityInstance.Module.ctx) {
            window.unityInstance.Module.ctx.resume();
          }
        }
        ['click','keydown','touchstart'].forEach(function(evt) {
          document.addEventListener(evt, unlockAudio, { once: true, capture: true });
        });
      })();
    </script>
    ";
            html = html.Replace("</head>", fix + "</head>");
            File.WriteAllText(indexPath, html);
            Debug.Log("[PostBuild] 키보드 픽스 삽입 완료");
        }

        // ── 4) privacy.html 복사 ────────────────────────────────────
        private static void CopyPrivacyHtml(string buildPath)
        {
            // 소스: Assets/WebGL/privacy.html (빌드 삭제에 영향받지 않는 위치)
            string src  = Path.Combine(Application.dataPath, "WebGL", "privacy.html");
            string dest = Path.Combine(buildPath, "privacy.html");

            if (!File.Exists(src))
            {
                Debug.LogWarning($"[PostBuild] privacy.html 소스 없음: {src}  — Assets/WebGL/privacy.html 을 확인하세요.");
                return;
            }

            if (Path.GetFullPath(src) == Path.GetFullPath(dest)) return;

            File.Copy(src, dest, overwrite: true);
            Debug.Log($"[PostBuild] privacy.html 복사 완료: {dest}");
        }
    }
}
#endif
