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
            InjectWindowUnityInstance(outputPath);
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

        // ── 3) 키보드 픽스 + 한글 IME 보호 ────────────────────────────────
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
        // 1. Block Space/Backspace/Arrow/Tab from reaching Unity when overlay is hidden
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

        // 2. Prevent Unity from calling e.preventDefault() during Korean IME composition.
        //    Unity's window-level capture handler may call preventDefault() on keydown events,
        //    which cancels IME composition on Windows Chrome/Edge.
        var _origPD = Event.prototype.preventDefault;
        Event.prototype.preventDefault = function() {
          if (window._korComposing &&
              (this.type === 'keydown' || this.type === 'keyup' || this.type === 'keypress') &&
              this.target && this.target.id === 'kor-ime') {
            return;
          }
          return _origPD.call(this);
        };

        // 3. Redirect canvas focus to textarea when Korean overlay is active.
        //    Registered before Unity loads (capture phase) so it fires first.
        document.addEventListener('focus', function(e) {
          if (!window._korOverlayActive) return;
          var el = e.target;
          if (!el) return;
          if (el.tagName === 'CANVAS' || el.id === 'unity-canvas') {
            e.stopPropagation();
            el.blur();
            var ta = document.getElementById('kor-ime');
            if (ta) ta.focus();
          }
        }, true);

        // AUDIO-FIX: resume WebAudio context on first user interaction
        (function() {
          function resumeCtx(ctx) { try { if (ctx && ctx.state === 'suspended') ctx.resume(); } catch (e) {} }
          function unlockAudio() {
            var ui = window.unityInstance, m = ui && ui.Module;
            if (m) { resumeCtx(m.ctx); if (m.WEBAudio) resumeCtx(m.WEBAudio.audioContext); }
          }
          ['pointerdown','click','keydown','touchstart'].forEach(function(evt) {
            document.addEventListener(evt, unlockAudio, true);
          });
        })();
      })();
    </script>
    ";
            html = html.Replace("</head>", fix + "</head>");
            File.WriteAllText(indexPath, html);
            Debug.Log("[PostBuild] 키보드 픽스 + 한글 IME 보호 삽입 완료");
        }

        // ── 4) window.unityInstance 글로벌 노출 (오디오 언락 스크립트가 참조) ──
        private static void InjectWindowUnityInstance(string buildPath)
        {
            string indexPath = Path.Combine(buildPath, "index.html");
            if (!File.Exists(indexPath)) return;

            string html = File.ReadAllText(indexPath);
            const string marker  = "window.unityInstance = unityInstance;";
            if (html.Contains(marker)) return;

            // createUnityInstance().then((unityInstance) => { 다음 줄에 삽입
            const string anchor  = "}).then((unityInstance) => {";
            const string inject  = "}).then((unityInstance) => {\n                window.unityInstance = unityInstance;";
            if (!html.Contains(anchor)) { Debug.LogWarning("[PostBuild] then 블록을 찾지 못해 window.unityInstance 삽입 생략"); return; }
            html = html.Replace(anchor, inject);
            File.WriteAllText(indexPath, html);
            Debug.Log("[PostBuild] window.unityInstance 삽입 완료");
        }

        // ── 5) privacy.html 복사 ────────────────────────────────────
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
