// Korean IME input bridge for Unity WebGL
// Positions a styled HTML textarea exactly over the Unity input field.
// Handles IME composition events (required for Windows Korean input).
mergeInto(LibraryManager.library, {

    KorIme_Init: function(goNamePtr, onTextPtr, onSubmitPtr) {
        var goName   = UTF8ToString(goNamePtr);
        var onText   = UTF8ToString(onTextPtr);
        var onSubmit = UTF8ToString(onSubmitPtr);

        if (document.getElementById('kor-ime-wrap')) return;

        // Placeholder style
        var style = document.createElement('style');
        style.textContent = '#kor-ime::placeholder{color:rgba(179,186,204,0.70);}#kor-ime{scrollbar-width:none;}#kor-ime::-webkit-scrollbar{display:none;}';
        document.head.appendChild(style);

        // Transparent wrapper (clicks pass through to Unity canvas)
        var wrap = document.createElement('div');
        wrap.id = 'kor-ime-wrap';
        wrap.style.cssText = 'position:fixed;top:0;left:0;width:100%;height:100%;pointer-events:none;z-index:9999;display:none;';
        document.body.appendChild(wrap);

        // The actual textarea
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

        // Recalculate position to match Unity input field layout (1920x1080 reference).
        // InputContainer: anchors (0,0)-(1,0), sizeDelta=(-120,64), pos=(0,20)
        // InputField:     offsetMin=(16,6), offsetMax=(-160,-6)  → text area only (excl. send btn)
        function updateLayout() {
            var canvas = getCanvas();
            if (!canvas) return;
            var r = canvas.getBoundingClientRect();
            var s = r.height / 1080;
            var left   = r.left  + 76  * s;          // 60(margin) + 16(field inset)
            var width  = r.width - 296 * s;           // excl. 76 left + 220 right (container margin + field offsetMax + gap)
            var height = 52 * s;                      // 64 - 6*2 inner padding
            var bottom = window.innerHeight - r.bottom + 26 * s; // 20(container) + 6(field inset)
            var fs     = Math.round(22 * s);
            ta.style.left      = left + 'px';
            ta.style.width     = width + 'px';
            ta.style.height    = height + 'px';
            ta.style.bottom    = bottom + 'px';
            ta.style.fontSize  = fs + 'px';
            ta.style.lineHeight = height + 'px';
            ta.style.padding   = '0 ' + Math.round(18 * s) + 'px';
            ta.style.fontFamily = "'Pretendard','맑은 고딕','Apple SD Gothic Neo',sans-serif";
        }

        var composing = false;

        ta.addEventListener('compositionstart', function() { composing = true; });
        ta.addEventListener('compositionend',   function() {
            composing = false;
            SendMessage(goName, onText, ta.value);
        });
        ta.addEventListener('input', function() {
            if (!composing) SendMessage(goName, onText, ta.value);
        });
        ta.addEventListener('keydown', function(e) {
            if (e.key === 'Enter' && !e.shiftKey) {
                e.preventDefault();
                var val = ta.value;
                ta.value = '';
                SendMessage(goName, onSubmit, val);
            }
        });

        // Auto-refocus when the overlay is clicked away (user clicked on chat area etc.)
        ta.addEventListener('blur', function() {
            if (!wrap || wrap.style.display === 'none') return;
            window._korRefocusTimer = setTimeout(function() {
                if (wrap && wrap.style.display !== 'none' && ta) ta.focus();
            }, 120);
        });

        window.addEventListener('resize', updateLayout);
        updateLayout();

        window._korIme      = ta;
        window._korImeWrap  = wrap;
        window._korImeLayout = updateLayout;
    },

    KorIme_Show: function() {
        if (!window._korImeWrap) return;
        if (window._korImeLayout) window._korImeLayout();
        window._korImeWrap.style.display = 'block';
        if (document.activeElement !== window._korIme) {
            setTimeout(function() {
                if (window._korIme) window._korIme.focus();
            }, 40);
        }
    },

    KorIme_Hide: function() {
        if (window._korRefocusTimer) { clearTimeout(window._korRefocusTimer); window._korRefocusTimer = null; }
        if (!window._korImeWrap) return;
        window._korImeWrap.style.display = 'none';
        if (window._korIme) { window._korIme.blur(); window._korIme.value = ''; }
    },

    KorIme_Clear: function() {
        if (window._korIme) window._korIme.value = '';
    }
});
