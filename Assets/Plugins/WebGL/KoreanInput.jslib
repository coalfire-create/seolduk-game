mergeInto(LibraryManager.library, {

    KorIme_Init: function(goNamePtr, onTextPtr, onSubmitPtr) {
        var goName   = UTF8ToString(goNamePtr);
        var onText   = UTF8ToString(onTextPtr);
        var onSubmit = UTF8ToString(onSubmitPtr);

        if (document.getElementById('kor-ime-wrap')) return;

        var style = document.createElement('style');
        style.textContent = [
            '#kor-ime::placeholder{color:rgba(179,186,204,0.70);}',
            '#kor-ime{scrollbar-width:none;}',
            '#kor-ime::-webkit-scrollbar{display:none;}',
        ].join('');
        document.head.appendChild(style);

        // Transparent wrapper — clicks pass through to Unity canvas
        var wrap = document.createElement('div');
        wrap.id = 'kor-ime-wrap';
        wrap.style.cssText = 'position:fixed;top:0;left:0;width:100%;height:100%;pointer-events:none;z-index:9999;display:none;';
        document.body.appendChild(wrap);

        var ta = document.createElement('textarea');
        ta.id = 'kor-ime';
        ta.setAttribute('lang', 'ko');
        ta.setAttribute('autocomplete', 'off');
        ta.setAttribute('autocorrect', 'off');
        ta.setAttribute('autocapitalize', 'none');
        ta.setAttribute('spellcheck', 'false');
        ta.setAttribute('placeholder', '질문이나 대화를 자유롭게 입력하세요...');
        ta.rows = 1;
        ta.style.cssText = [
            'position:absolute',
            'pointer-events:auto',
            'background:rgba(5,6,14,0.92)',
            'color:#ffffff',
            'border:none',
            'outline:none',
            'resize:none',
            'overflow:hidden',
            'box-sizing:border-box',
            'caret-color:rgba(255,210,80,0.95)',
            'vertical-align:middle',
        ].join(';');
        wrap.appendChild(ta);

        function getCanvas() {
            return document.querySelector('#unity-canvas') || document.querySelector('canvas');
        }

        // Match Unity input field layout (1920×1080 reference).
        function updateLayout() {
            var canvas = getCanvas();
            if (!canvas) return;
            var r = canvas.getBoundingClientRect();
            var s = r.height / 1080;
            ta.style.left       = (r.left  + 76  * s) + 'px';
            ta.style.width      = (r.width - 296 * s) + 'px';
            ta.style.height     = (52 * s) + 'px';
            ta.style.bottom     = (window.innerHeight - r.bottom + 26 * s) + 'px';
            ta.style.fontSize   = Math.round(22 * s) + 'px';
            ta.style.lineHeight = (52 * s) + 'px';
            ta.style.padding    = '0 ' + Math.round(18 * s) + 'px';
            ta.style.fontFamily = "'Pretendard','맑은 고딕','Apple SD Gothic Neo',sans-serif";
        }

        var composing = false;
        var lastVal   = '';

        // Deduplicated send: only sends when value actually changed.
        function trySend() {
            var v = ta.value;
            if (v !== lastVal) {
                lastVal = v;
                SendMessage(goName, onText, v);
            }
        }

        // ── Composition events (Korean IME) ──────────────────────────────
        ta.addEventListener('compositionstart', function(e) {
            composing = true;
            window._korComposing = true;
            e.stopPropagation();
        });
        ta.addEventListener('compositionupdate', function(e) {
            e.stopPropagation();
        });
        ta.addEventListener('compositionend', function(e) {
            composing = false;
            window._korComposing = false;
            e.stopPropagation();
            // Chrome fires 'input' AFTER compositionend with the final value.
            // Schedule trySend via setTimeout so the 'input' handler fires first;
            // if 'input' already sent the final value, lastVal dedup suppresses the duplicate.
            setTimeout(function() { trySend(); }, 0);
        });

        // ── Input event (English typing + post-composition sync) ─────────
        ta.addEventListener('input', function(e) {
            e.stopPropagation();
            if (!composing) trySend();
        });

        // ── Keyboard events: stop propagation to prevent Unity interference ──
        ta.addEventListener('keydown', function(e) {
            e.stopPropagation();
            if (e.key === 'Enter' && !e.shiftKey && !composing) {
                e.preventDefault();
                var val = ta.value;
                ta.value = '';
                lastVal  = '';
                SendMessage(goName, onSubmit, val);
            }
        });
        ta.addEventListener('keyup',   function(e) { e.stopPropagation(); });
        ta.addEventListener('keypress', function(e) { e.stopPropagation(); });

        // ── Blur guard: immediately refocus when overlay is visible ───────
        ta.addEventListener('blur', function() {
            if (!wrap || wrap.style.display === 'none') return;
            requestAnimationFrame(function() {
                if (wrap && wrap.style.display !== 'none') ta.focus();
            });
        });

        window.addEventListener('resize', updateLayout);
        window.addEventListener('orientationchange', updateLayout);
        updateLayout();

        window._korIme       = ta;
        window._korImeWrap   = wrap;
        window._korImeLayout = updateLayout;
        window._korComposing = false;
        window._korOverlayActive = false;
    },

    KorIme_Show: function() {
        if (!window._korImeWrap) return;
        window._korOverlayActive = true;
        if (window._korImeLayout) window._korImeLayout();
        window._korImeWrap.style.display = 'block';
        var ta = window._korIme;
        if (!ta) return;
        // Blur canvas immediately so it cannot intercept keyboard events.
        var canvas = document.querySelector('#unity-canvas') || document.querySelector('canvas');
        if (canvas) canvas.blur();
        // Focus textarea without delay (40 ms delay was the root cause on Windows).
        ta.focus();
    },

    KorIme_Hide: function() {
        window._korOverlayActive = false;
        window._korComposing     = false;
        if (!window._korImeWrap) return;
        window._korImeWrap.style.display = 'none';
        var ta = window._korIme;
        if (ta) { ta.blur(); ta.value = ''; }
    },

    KorIme_Clear: function() {
        if (window._korIme) window._korIme.value = '';
    }
});
