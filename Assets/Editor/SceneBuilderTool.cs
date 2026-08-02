#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using TMPro;
using Persuasion;
using Persuasion.Core;
using Persuasion.AI;
using Persuasion.UI;
using Persuasion.Character;
using Persuasion.Audio;

namespace Persuasion.EditorTools
{
    public static class SceneBuilderTool
    {
        private const string ScenePath             = "Assets/Scenes/PlayScene.unity";
        private const string FontAssetPath         = "Assets/Art/Fonts/Pretendard-Bold SDF.asset";
        private const string GameDataPath          = "Assets/Data/GameData.asset";
        private const string LibraryPath           = "Assets/Data/StagePortraitLibrary.asset";
        private const string TransitionLibraryPath = "Assets/Data/TransitionArtLibrary.asset";

        private static TMP_FontAsset _font;

        // ── Design Tokens ─── 기밀문서 / 심문 테마 ─────────────────────
        // Backgrounds: Near-black ink paper
        static readonly Color BG_DEEP    = new Color(0.055f, 0.050f, 0.060f, 1.00f);
        static readonly Color BG_PANEL   = new Color(0.065f, 0.060f, 0.075f, 0.96f);
        static readonly Color BG_CARD    = new Color(0.080f, 0.075f, 0.095f, 0.92f);
        static readonly Color BG_TOPBAR  = new Color(0f, 0f, 0f, 0f);
        static readonly Color BG_OVERLAY = new Color(0.020f, 0.018f, 0.025f, 0.92f);

        // Accent: Crimson stamp red (도장/기밀 스탬프)
        static readonly Color ACCENT     = new Color(0.860f, 0.110f, 0.135f, 1.00f);
        static readonly Color ACCENT_DIM = new Color(0.860f, 0.110f, 0.135f, 0.28f);

        // Semantic
        static readonly Color COL_DANGER  = new Color(0.850f, 0.180f, 0.180f, 1.00f);
        static readonly Color COL_NEUTRAL = new Color(0.120f, 0.100f, 0.140f, 0.88f);

        // Text
        static readonly Color TXT_PRIMARY   = new Color(0.980f, 0.975f, 0.965f, 1.00f);
        static readonly Color TXT_SECONDARY = new Color(0.620f, 0.640f, 0.700f, 1.00f);
        static readonly Color TXT_ACCENT    = new Color(1.000f, 0.780f, 0.340f, 1.00f); // warm amber for text
        static readonly Color TXT_GOLD      = new Color(1.000f, 0.880f, 0.500f, 1.00f); // warm gold

        // Typography scale
        const int TYPE_DISPLAY = 72;
        const int TYPE_TITLE   = 56;
        const int TYPE_HEADING = 36;
        const int TYPE_BODY    = 22;
        const int TYPE_SMALL   = 19;
        const int TYPE_CAPTION = 15;

        // Spacing
        const float SAFE  = 40f;
        const float SP_XL = 48f;
        const float SP_LG = 32f;
        const float SP_MD = 24f;
        const float SP_SM = 16f;
        const float SP_XS =  8f;

        // ───────────────────────────────────────────────────────────────

        [MenuItem("Persuasion/4) Build Playable Scene")]
        public static void Build()
        {
            _font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            // atlasTextures가 없는 손상 에셋(배치 모드 생성 부산물)은 폴백 처리
            if (_font != null && (_font.atlasTextures == null || _font.atlasTextures.Length == 0))
            {
                Debug.LogWarning("[Persuasion] Pretendard SDF 에셋이 손상됨 → 삭제 후 에디터에서 재생성 필요. AppleGothic 폴백.");
                _font = null;
            }
            if (_font == null)
            {
                _font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Art/Fonts/AppleGothic SDF.asset");
                if (_font != null)
                    Debug.LogWarning("[Persuasion] Pretendard-Bold SDF 없음 → AppleGothic 폴백. Unity 에디터(GUI)에서 Pretendard-Bold.ttf 선택 후 Create > TextMeshPro > Font Asset 으로 생성하세요.");
                else
                    Debug.LogWarning("[Persuasion] 한글 폰트를 찾을 수 없습니다.");
            }

            var gameData = AssetDatabase.LoadAssetAtPath<GameData>("Assets/Resources/GameData.asset")
                        ?? AssetDatabase.LoadAssetAtPath<GameData>(GameDataPath);
            if (gameData == null) { Debug.LogError("[Persuasion] GameData 없음. 먼저 '1) Create Game Data' 실행."); return; }
            var library = AssetDatabase.LoadAssetAtPath<StagePortraitLibrary>(LibraryPath);
            var transitionLibrary = AssetDatabase.LoadAssetAtPath<TransitionArtLibrary>(TransitionLibraryPath);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // ── Camera ──
            var cameraGO = new GameObject("Main Camera");
            cameraGO.tag = "MainCamera";
            var cam = cameraGO.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = BG_DEEP;
            cam.depth = -1;
            cameraGO.AddComponent<AudioListener>();

            // ── EventSystem ──
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            // ── Canvas ──
            var canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas   = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode     = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight  = 0.5f;
            var canvasT = canvasGO.transform;

            // ── 배경 초상화 ──
            var bgGO = new GameObject("BackgroundPortrait", typeof(RectTransform), typeof(Image), typeof(AspectRatioFitter));
            bgGO.transform.SetParent(canvasT, false);
            var bgRt = bgGO.GetComponent<RectTransform>();
            bgRt.anchorMin = bgRt.anchorMax = bgRt.pivot = new Vector2(0.5f, 0.5f);
            bgRt.anchoredPosition = Vector2.zero;
            bgRt.sizeDelta = new Vector2(1920, 1080);
            var bgImage = bgGO.GetComponent<Image>();
            bgImage.color = Color.white;
            bgImage.preserveAspect = false;
            bgImage.raycastTarget = false;
            bgImage.enabled = false;
            var bgFitter = bgGO.GetComponent<AspectRatioFitter>();
            bgFitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            bgFitter.aspectRatio = 2816f / 1536f;

            // ════════════════════════════════════════════════
            // INTRO PANEL
            // ════════════════════════════════════════════════
            var introPanel = MkPanel("IntroPanel", canvasT, new Color(0.04f, 0.04f, 0.06f, 0.35f), true);

            var introBgGO = new GameObject("IntroBackgroundImage", typeof(RectTransform), typeof(Image), typeof(AspectRatioFitter));
            introBgGO.transform.SetParent(introPanel.transform, false);
            introBgGO.transform.SetAsFirstSibling();
            var introBgRt = introBgGO.GetComponent<RectTransform>();
            Full(introBgRt);
            var introBgImg = introBgGO.GetComponent<Image>();
            introBgImg.raycastTarget = false;
            introBgImg.preserveAspect = false;

            if (transitionLibrary != null && transitionLibrary.titleScreen != null)
            {
                introBgImg.sprite = transitionLibrary.titleScreen;
                introBgImg.color = Color.white;
                introBgImg.enabled = true;
            }
            else
            {
                introBgImg.enabled = false;
            }

            var introFitter = introBgGO.GetComponent<AspectRatioFitter>();
            introFitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            introFitter.aspectRatio = 16f / 9f;

            var introTopGradGO = new GameObject("TopGradient", typeof(RectTransform));
            introTopGradGO.transform.SetParent(introPanel.transform, false);
            var introTopGradRt = introTopGradGO.GetComponent<RectTransform>();
            introTopGradRt.anchorMin = new Vector2(0, 1);
            introTopGradRt.anchorMax = Vector2.one;
            introTopGradRt.pivot = new Vector2(0.5f, 1);
            introTopGradRt.sizeDelta = new Vector2(0, 300);
            introTopGradRt.anchoredPosition = Vector2.zero;
            var topGrad = introTopGradGO.AddComponent<UIVerticalGradient>();
            topGrad.topColor = new Color(0.02f, 0.02f, 0.05f, 0.95f);
            topGrad.bottomColor = new Color(0.02f, 0.02f, 0.05f, 0.00f);
            topGrad.raycastTarget = false;

            var introBotGradGO = new GameObject("BottomGradient", typeof(RectTransform));
            introBotGradGO.transform.SetParent(introPanel.transform, false);
            var introBotGradRt = introBotGradGO.GetComponent<RectTransform>();
            introBotGradRt.anchorMin = Vector2.zero;
            introBotGradRt.anchorMax = new Vector2(1, 0);
            introBotGradRt.pivot = new Vector2(0.5f, 0);
            introBotGradRt.sizeDelta = new Vector2(0, 380);
            introBotGradRt.anchoredPosition = Vector2.zero;
            var botGrad = introBotGradGO.AddComponent<UIVerticalGradient>();
            botGrad.topColor = new Color(0.02f, 0.02f, 0.05f, 0.00f);
            botGrad.bottomColor = new Color(0.02f, 0.02f, 0.05f, 0.96f);
            botGrad.raycastTarget = false;

            var topHeaderGO = new GameObject("TopHeaderBar", typeof(RectTransform));
            var introMenuBtn = MkBarButton("IntroMenuButton", introPanel.transform, "메뉴", 130f, new Color(0, 0, 0, 0.6f));
            var introMenuBtnRt = introMenuBtn.GetComponent<RectTransform>();
            AnchorTopRight(introMenuBtnRt, new Vector2(-SAFE, -SAFE), new Vector2(130f, 42f));

            var titleGroupGO = new GameObject("TitleGroup", typeof(RectTransform), typeof(CanvasGroup));
            titleGroupGO.transform.SetParent(introPanel.transform, false);
            AnchorBox(titleGroupGO.GetComponent<RectTransform>(), new Vector2(0.5f, 0.75f), new Vector2(1600, 180));
            var introTitleGroup = titleGroupGO.GetComponent<CanvasGroup>();

            var titleShadow = MkText("TitleShadow", titleGroupGO.transform, 110, TextAlignmentOptions.Center, new Color(0, 0, 0, 0.8f));
            AnchorBox(titleShadow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(1500, 150));
            titleShadow.rectTransform.anchoredPosition = new Vector2(4, -8);
            titleShadow.fontStyle = FontStyles.Bold;
            titleShadow.characterSpacing = 30f;
            titleShadow.text = gameData.gameTitle;

            var titleText = MkText("Title", titleGroupGO.transform, 110, TextAlignmentOptions.Center, new Color(0.96f, 0.96f, 0.98f, 1f));
            AnchorBox(titleText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(1500, 150));
            titleText.fontStyle = FontStyles.Bold;
            titleText.characterSpacing = 30f;
            titleText.text = gameData.gameTitle;

            var startBtn = MkButton("StartButton", introPanel.transform, "화면을 클릭하여 시작하세요", out var startBtnText);
            AnchorBox(startBtn.GetComponent<RectTransform>(), new Vector2(0.5f, 0.35f), new Vector2(560, 80));
            // Make button background transparent for a clean text-only look
            startBtn.GetComponent<Image>().color = new Color(0, 0, 0, 0f); 
            startBtnText.fontSize = 28;
            startBtnText.fontStyle = FontStyles.Bold;
            startBtnText.characterSpacing = 8f;
            startBtnText.color = new Color(1f, 1f, 1f, 0.8f);



            // 개인정보처리방침 링크 버튼 (우측 하단 작게)
            var privacyBtn = MkBarButton("PrivacyButton", introPanel.transform, "개인정보처리방침", 200f, new Color(0.12f, 0.12f, 0.15f, 0.60f));
            var privacyRt  = privacyBtn.GetComponent<RectTransform>();
            AnchorBox(privacyRt, new Vector2(0.88f, 0.03f), new Vector2(200f, 36f));
            var privacyLbl = privacyBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (privacyLbl != null) { privacyLbl.fontSize = TYPE_CAPTION; privacyLbl.color = TXT_SECONDARY; }

            // ════════════════════════════════════════════════
            // PLAY PANEL
            // ════════════════════════════════════════════════
            var playPanel = MkPanel("PlayPanel", canvasT, new Color(0, 0, 0, 0), false);

            var topLeftGradGO = new GameObject("TopLeftVignette", typeof(RectTransform));
            topLeftGradGO.transform.SetParent(playPanel.transform, false);
            var tlgRt = topLeftGradGO.GetComponent<RectTransform>();
            AnchorTopLeft(tlgRt, Vector2.zero, new Vector2(960f, 220f));
            var tlg = topLeftGradGO.AddComponent<UIVerticalGradient>();
            tlg.topColor = new Color(0.02f, 0.02f, 0.04f, 0.60f);
            tlg.bottomColor = new Color(0.02f, 0.02f, 0.04f, 0.00f);
            tlg.raycastTarget = false;

            var gradGO = new GameObject("BottomGradient", typeof(RectTransform));
            gradGO.transform.SetParent(playPanel.transform, false);
            var gradRt = gradGO.GetComponent<RectTransform>();
            gradRt.anchorMin = Vector2.zero; gradRt.anchorMax = new Vector2(1f, 0.45f);
            gradRt.offsetMin = Vector2.zero; gradRt.offsetMax = Vector2.zero;
            var grad = gradGO.AddComponent<UIVerticalGradient>();
            grad.topColor    = new Color(0f, 0f, 0f, 0f);
            grad.bottomColor = new Color(0.02f, 0.02f, 0.04f, 0.88f);
            grad.raycastTarget = false;

            var topBar = MkPanel("TopBar", playPanel.transform, new Color(0f, 0f, 0f, 0f), false);
            var topRt  = topBar.GetComponent<RectTransform>();
            topRt.anchorMin = new Vector2(0, 1); topRt.anchorMax = new Vector2(1, 1);
            topRt.pivot = new Vector2(0.5f, 1); topRt.sizeDelta = new Vector2(0, 160);
            topRt.anchoredPosition = Vector2.zero;

            var leftInfoGO = new GameObject("LeftInfoGroup", typeof(RectTransform));
            leftInfoGO.transform.SetParent(topBar.transform, false);
            var leftInfoRt = leftInfoGO.GetComponent<RectTransform>();
            AnchorTopLeft(leftInfoRt, new Vector2(SAFE + 8f, -20f), new Vector2(900, 110));

            var chapterText = MkText("Chapter", leftInfoGO.transform, 1, TextAlignmentOptions.TopLeft, new Color(0, 0, 0, 0));
            var stageText   = MkText("Stage", leftInfoGO.transform, 1, TextAlignmentOptions.TopLeft, new Color(0, 0, 0, 0));

            var goalBanner = new GameObject("GoalBanner", typeof(RectTransform));
            goalBanner.transform.SetParent(leftInfoGO.transform, false);
            var goalBanRt = goalBanner.GetComponent<RectTransform>();
            AnchorTopLeft(goalBanRt, Vector2.zero, new Vector2(900, 38f));

            var goalText = MkText("GoalText", goalBanner.transform, 26, TextAlignmentOptions.TopLeft, TXT_GOLD);
            Full(goalText.rectTransform);
            goalText.fontStyle = FontStyles.Bold;
            goalText.characterSpacing = 1.5f;
            var gtOutline = goalText.gameObject.AddComponent<Outline>();
            gtOutline.effectColor = new Color(0.00f, 0.00f, 0.01f, 0.98f);
            gtOutline.effectDistance = new Vector2(2.5f, -2.5f);
            goalText.text = "▶  수사 목표 : 자백 받아내기";

            var charNameText = MkText("CharacterName", leftInfoGO.transform, 20, TextAlignmentOptions.TopLeft, TXT_ACCENT);
            AnchorTopLeft(charNameText.rectTransform, new Vector2(0, -40f), new Vector2(900, 28f));
            charNameText.fontStyle = FontStyles.Bold;
            var cnOutline = charNameText.gameObject.AddComponent<Outline>();
            cnOutline.effectColor = new Color(0.00f, 0.00f, 0.01f, 0.98f);
            cnOutline.effectDistance = new Vector2(2f, -2f);

            var gaugeLabel = MkText("GaugeLabel", topBar.transform, TYPE_CAPTION, TextAlignmentOptions.Left, TXT_SECONDARY);
            AnchorTopRight(gaugeLabel.rectTransform, new Vector2(-440f, -18f), new Vector2(100f, 24f));
            gaugeLabel.text = "설득도";
            var glOutline = gaugeLabel.gameObject.AddComponent<Outline>();
            glOutline.effectColor = new Color(0.01f, 0.01f, 0.02f, 0.95f);

            var slider = MkSlider("PersuasionSlider", topBar.transform, persuasionMode: true);
            var sliderRt = slider.GetComponent<RectTransform>();
            sliderRt.anchorMin = new Vector2(1, 1); sliderRt.anchorMax = new Vector2(1, 1); sliderRt.pivot = new Vector2(1, 1);
            sliderRt.sizeDelta = new Vector2(420f, 26f); sliderRt.anchoredPosition = new Vector2(-SAFE, -16f);

            // 설득도 % 숫자 (게이지 우측 끝, 볼드 + 외곽선으로 채워진 바 위에서도 잘 보이게)
            var persuasionValueText = MkText("PersuasionValueText", topBar.transform, TYPE_SMALL, TextAlignmentOptions.Right, TXT_PRIMARY);
            var pvRt = persuasionValueText.rectTransform;
            pvRt.anchorMin = new Vector2(1, 1); pvRt.anchorMax = new Vector2(1, 1); pvRt.pivot = new Vector2(1, 1);
            pvRt.sizeDelta = new Vector2(110f, 26f); pvRt.anchoredPosition = new Vector2(-SAFE - 10f, -15f);
            persuasionValueText.fontStyle = FontStyles.Bold;
            persuasionValueText.text = "0%";
            var pvOutline = persuasionValueText.gameObject.AddComponent<Outline>();
            pvOutline.effectColor = new Color(0f, 0f, 0f, 0.95f);
            pvOutline.effectDistance = new Vector2(2f, -2f);

            var emotionLabel = MkText("EmotionLabel", topBar.transform, TYPE_SMALL, TextAlignmentOptions.Right, TXT_SECONDARY);
            AnchorTopRight(emotionLabel.rectTransform, new Vector2(-SAFE, -50f), new Vector2(420f, 30f));
            var elOutline = emotionLabel.gameObject.AddComponent<Outline>();
            elOutline.effectColor = new Color(0.01f, 0.01f, 0.02f, 0.95f);

            // ── 컨트롤 버튼 뒤 반투명 배경 스트립 (심문실 제어판 느낌) ──
            {
                var ctrlBg = new GameObject("CtrlBackground", typeof(RectTransform), typeof(Image));
                ctrlBg.transform.SetParent(playPanel.transform, false);
                var cbRt = ctrlBg.GetComponent<RectTransform>();
                cbRt.anchorMin = new Vector2(1, 1); cbRt.anchorMax = new Vector2(1, 1); cbRt.pivot = new Vector2(1, 1);
                cbRt.sizeDelta = new Vector2(570f, 54f); cbRt.anchoredPosition = new Vector2(-SAFE + 8f, -96f);
                ctrlBg.GetComponent<Image>().color = new Color(0.02f, 0.01f, 0.03f, 0.68f);
                ctrlBg.GetComponent<Image>().raycastTarget = false;
                // 좌측 얇은 ACCENT 세로선
                var cbLine = new GameObject("AccentLine", typeof(RectTransform), typeof(Image));
                cbLine.transform.SetParent(ctrlBg.transform, false);
                var clRt = cbLine.GetComponent<RectTransform>();
                clRt.anchorMin = new Vector2(0, 0); clRt.anchorMax = new Vector2(0, 1);
                clRt.pivot = new Vector2(0, 0.5f); clRt.sizeDelta = new Vector2(3, 0); clRt.anchoredPosition = Vector2.zero;
                cbLine.GetComponent<Image>().color = ACCENT;
                cbLine.GetComponent<Image>().raycastTarget = false;
            }

            var ctrlRow = new GameObject("PlayControls", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
            ctrlRow.transform.SetParent(playPanel.transform, false);
            var ctrlRt = ctrlRow.GetComponent<RectTransform>();
            ctrlRt.anchorMin = new Vector2(1, 1); ctrlRt.anchorMax = new Vector2(1, 1); ctrlRt.pivot = new Vector2(1, 1);
            ctrlRt.anchoredPosition = new Vector2(-SAFE, -100f);
            var ctrlHlg = ctrlRow.GetComponent<HorizontalLayoutGroup>();
            ctrlHlg.spacing = SP_XS; ctrlHlg.childAlignment = TextAnchor.MiddleRight;
            ctrlHlg.childControlWidth = true; ctrlHlg.childControlHeight = true;
            ctrlHlg.childForceExpandWidth = false; ctrlHlg.childForceExpandHeight = false;
            ctrlRow.GetComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            ctrlRow.GetComponent<ContentSizeFitter>().verticalFit   = ContentSizeFitter.FitMode.PreferredSize;

            var storyPeekBtn = MkBarButton("StoryPeekButton", ctrlRow.transform, "▶ 스토리",   142f, COL_NEUTRAL);
            var traitBtn     = MkBarButton("TraitButton",     ctrlRow.transform, "■ 수사 파일", 138f, ACCENT);
            var historyBtn   = MkBarButton("HistoryButton",   ctrlRow.transform, "▶ 대화기록",  138f, COL_NEUTRAL);
            var menuBtn      = MkBarButton("MenuButton",      ctrlRow.transform, "▶ 메뉴",      112f, COL_NEUTRAL);

            // 숨김 처리용 narrationText (PersuasionUI 바인딩 유지)
            var narrationText = MkText("CharacterTrait", playPanel.transform, 1, TextAlignmentOptions.TopLeft, new Color(0, 0, 0, 0));

            var chatScrollGO = new GameObject("ChatScroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect), typeof(RectMask2D));
            chatScrollGO.transform.SetParent(playPanel.transform, false);
            var chatScrollRt = chatScrollGO.GetComponent<RectTransform>();
            chatScrollRt.anchorMin = new Vector2(0, 0); chatScrollRt.anchorMax = new Vector2(1, 0); chatScrollRt.pivot = new Vector2(0.5f, 0);
            chatScrollRt.sizeDelta = new Vector2(-120f, 160f); chatScrollRt.anchoredPosition = new Vector2(0, 420f);
            chatScrollGO.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0f);

            var scrollRect = chatScrollGO.GetComponent<ScrollRect>();
            scrollRect.horizontal = false; scrollRect.vertical = true; scrollRect.scrollSensitivity = 30;

            var viewportGO = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            viewportGO.transform.SetParent(chatScrollGO.transform, false);
            Full(viewportGO.GetComponent<RectTransform>());

            var contentGO = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentGO.transform.SetParent(viewportGO.transform, false);
            var contentRt = contentGO.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0, 1); contentRt.anchorMax = new Vector2(1, 1); contentRt.pivot = new Vector2(0.5f, 1);
            contentRt.sizeDelta = Vector2.zero; contentRt.anchoredPosition = Vector2.zero;
            var vlg = contentGO.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(16, 16, 8, 8); vlg.spacing = SP_XS;
            vlg.childControlWidth = true; vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
            contentGO.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scrollRect.viewport = viewportGO.GetComponent<RectTransform>();
            scrollRect.content  = contentRt;

            var hintPanel = MkPanel("HintPanel", playPanel.transform, new Color(0.24f, 0.16f, 0.04f, 0.90f), false);
            var hintRt = hintPanel.GetComponent<RectTransform>();
            hintRt.anchorMin = new Vector2(0, 0); hintRt.anchorMax = new Vector2(1, 0); hintRt.pivot = new Vector2(0.5f, 0);
            hintRt.sizeDelta = new Vector2(-120f, 48f); hintRt.anchoredPosition = new Vector2(0, 306f);

            var hintText = MkText("HintText", hintPanel.transform, TYPE_SMALL, TextAlignmentOptions.Left, TXT_GOLD);
            Full(hintText.rectTransform); hintText.margin = new Vector4(SP_MD, SP_XS, SP_MD, SP_XS);
            hintText.fontStyle = FontStyles.Bold;

            // 1인칭 영화풍 수사관 대화 입력 (Invisible Direct Seamless Input)
            var inputContainer = MkPanel("InputContainer", playPanel.transform, new Color(0f, 0f, 0f, 0f), false);
            var inputContRt = inputContainer.GetComponent<RectTransform>();
            inputContRt.anchorMin = new Vector2(0, 0); inputContRt.anchorMax = new Vector2(1, 0); inputContRt.pivot = new Vector2(0.5f, 0);
            inputContRt.sizeDelta = new Vector2(-120f, 64f); inputContRt.anchoredPosition = new Vector2(0, 20f);

            var inputField = MkInputField("InputField", inputContainer.transform, "질문이나 대화를 자유롭게 입력하세요... (ENTER 키로 발언)");
            var inputRt = inputField.GetComponent<RectTransform>();
            Full(inputRt);
            inputRt.offsetMin = new Vector2(16f, 6f);
            inputRt.offsetMax = new Vector2(-160f, -6f);

            var inputBg = inputField.GetComponent<Image>();
            if (inputBg != null) inputBg.color = new Color(0f, 0f, 0f, 0f);

            foreach (var txt in inputField.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                txt.alignment = TextAlignmentOptions.Center;
                var txtOutline = txt.gameObject.GetComponent<Outline>();
                if (txtOutline == null) txtOutline = txt.gameObject.AddComponent<Outline>();
                txtOutline.effectColor = new Color(0.01f, 0.01f, 0.02f, 0.98f);
                txtOutline.effectDistance = new Vector2(2f, -2f);
            }

            var sendBtn = MkButton("SendButton", inputContainer.transform, "ENTER ▶", out var sendLbl);
            var sendRt  = sendBtn.GetComponent<RectTransform>();
            sendRt.anchorMin = new Vector2(1, 0); sendRt.anchorMax = new Vector2(1, 1); sendRt.pivot = new Vector2(1, 0.5f);
            sendRt.sizeDelta = new Vector2(140f, 0);
            sendRt.offsetMin = new Vector2(sendRt.offsetMin.x, 6f);
            sendRt.offsetMax = new Vector2(-6f, -6f);
            sendLbl.fontSize = 18;
            sendLbl.fontStyle = FontStyles.Bold;
            BtnColor(sendBtn, new Color(0.85f, 0.62f, 0.18f, 0.40f));

            // ════════════════════════════════════════════════
            // STAGE INTRO OVERLAY (단계 및 수사 목표 팝업 연출 오버레이)
            // ════════════════════════════════════════════════
            var stageIntroOverlay = MkPanel("StageIntroOverlay", canvasT, new Color(0.03f, 0.04f, 0.06f, 0.92f), true);
            var stageIntroGroup = stageIntroOverlay.AddComponent<CanvasGroup>();

            var introLineLeft = MkPanel("LineLeft", stageIntroOverlay.transform, ACCENT, false);
            AnchorBox(introLineLeft.GetComponent<RectTransform>(), new Vector2(0.24f, 0.62f), new Vector2(160, 2));

            var introLineRight = MkPanel("LineRight", stageIntroOverlay.transform, ACCENT, false);
            AnchorBox(introLineRight.GetComponent<RectTransform>(), new Vector2(0.76f, 0.62f), new Vector2(160, 2));

            var introTitleShadow = MkText("IntroTitleShadow", stageIntroOverlay.transform, 64, TextAlignmentOptions.Center, new Color(0.01f, 0.01f, 0.02f, 0.95f));
            AnchorBox(introTitleShadow.rectTransform, new Vector2(0.5f, 0.62f), new Vector2(1400, 80));
            introTitleShadow.rectTransform.anchoredPosition = new Vector2(3, -3);
            introTitleShadow.fontStyle = FontStyles.Bold;
            introTitleShadow.characterSpacing = 8f;

            var introTitleText = MkText("IntroTitleText", stageIntroOverlay.transform, 64, TextAlignmentOptions.Center, TXT_PRIMARY);
            AnchorBox(introTitleText.rectTransform, new Vector2(0.5f, 0.62f), new Vector2(1400, 80));
            introTitleText.fontStyle = FontStyles.Bold;
            introTitleText.characterSpacing = 8f;

            var introSubTitleText = MkText("IntroSubTitleText", stageIntroOverlay.transform, 28, TextAlignmentOptions.Center, TXT_ACCENT);
            AnchorBox(introSubTitleText.rectTransform, new Vector2(0.5f, 0.53f), new Vector2(1400, 48));
            introSubTitleText.fontStyle = FontStyles.Bold;

            // 강렬하게 부각되는 수사 목표 카운터 뱃지
            var introGoalCard = MkPanel("IntroGoalCard", stageIntroOverlay.transform, new Color(0.16f, 0.13f, 0.06f, 0.96f), false);
            var introGoalRt = introGoalCard.GetComponent<RectTransform>();
            AnchorBox(introGoalRt, new Vector2(0.5f, 0.40f), new Vector2(740f, 80f));

            var introGoalBorder = MkPanel("Border", introGoalCard.transform, ACCENT, false);
            var igbRt = introGoalBorder.GetComponent<RectTransform>();
            Full(igbRt);
            igbRt.offsetMin = new Vector2(-2, -2); igbRt.offsetMax = new Vector2(2, 2);
            igbRt.SetAsFirstSibling();

            var introGoalText = MkText("IntroGoalText", introGoalCard.transform, 26, TextAlignmentOptions.Center, TXT_GOLD);
            Full(introGoalText.rectTransform);
            introGoalText.fontStyle = FontStyles.Bold;
            introGoalText.text = "▶  수사 목표 : 자백 받아내기";

            var introSkipHint = MkText("IntroSkipHint", stageIntroOverlay.transform, TYPE_CAPTION, TextAlignmentOptions.Center, TXT_SECONDARY);
            AnchorBox(introSkipHint.rectTransform, new Vector2(0.5f, 0.15f), new Vector2(600, 30));
            introSkipHint.characterSpacing = 2f;
            introSkipHint.text = "CLICK  OR  PRESS  [ SPACE ]  TO  BEGIN";

            stageIntroOverlay.SetActive(false);

            // ════════════════════════════════════════════════
            // RESULT PANEL
            // ════════════════════════════════════════════════
            var resultPanel = MkPanel("ResultPanel", canvasT, BG_PANEL, true);

            // 클리어 플래시 오버레이 (전체 화면 흰 섬광)
            var clearFlashGO = new GameObject("ClearFlashOverlay", typeof(RectTransform), typeof(Image));
            clearFlashGO.transform.SetParent(resultPanel.transform, false);
            Full(clearFlashGO.GetComponent<RectTransform>());
            var clearFlashImg = clearFlashGO.GetComponent<Image>();
            clearFlashImg.color = new Color(1f, 1f, 1f, 0f);
            clearFlashImg.raycastTarget = false;
            clearFlashGO.SetActive(false);

            var resultText = MkText("ResultText", resultPanel.transform, 52, TextAlignmentOptions.Center, TXT_PRIMARY);
            resultText.fontStyle = FontStyles.Bold;
            resultText.characterSpacing = 6f;
            AnchorBox(resultText.rectTransform, new Vector2(0.5f, 0.89f), new Vector2(1400, 90));

            // 등급 텍스트 (S/A/B/C)
            var resultGradeText = MkText("ResultGradeText", resultPanel.transform, 140, TextAlignmentOptions.Center, TXT_GOLD);
            resultGradeText.fontStyle = FontStyles.Bold;
            AnchorBox(resultGradeText.rectTransform, new Vector2(0.5f, 0.74f), new Vector2(300, 170));

            // 등급 장식 선
            var glL = MkPanel("GradeLineL", resultPanel.transform, new Color(ACCENT.r, ACCENT.g, ACCENT.b, 0.50f), false);
            AnchorBox(glL.GetComponent<RectTransform>(), new Vector2(0.28f, 0.74f), new Vector2(210, 2));
            var glR = MkPanel("GradeLineR", resultPanel.transform, new Color(ACCENT.r, ACCENT.g, ACCENT.b, 0.50f), false);
            AnchorBox(glR.GetComponent<RectTransform>(), new Vector2(0.72f, 0.74f), new Vector2(210, 2));

            // 통계 텍스트 (턴 수 · 설득도)
            var resultStatsText = MkText("ResultStatsText", resultPanel.transform, TYPE_BODY, TextAlignmentOptions.Center, TXT_SECONDARY);
            AnchorBox(resultStatsText.rectTransform, new Vector2(0.5f, 0.63f), new Vector2(900, 40));

            // 베스트 발화 카드
            var resultBestMovePanel = MkPanel("ResultBestMovePanel", resultPanel.transform, new Color(0.09f, 0.11f, 0.17f, 0.92f), false);
            AnchorBox(resultBestMovePanel.GetComponent<RectTransform>(), new Vector2(0.5f, 0.50f), new Vector2(1100, 148));
            var rbmBorder = MkPanel("Border", resultBestMovePanel.transform, new Color(ACCENT.r, ACCENT.g, ACCENT.b, 0.55f), false);
            Full(rbmBorder.GetComponent<RectTransform>());
            rbmBorder.GetComponent<RectTransform>().offsetMin = new Vector2(-2, -2);
            rbmBorder.GetComponent<RectTransform>().offsetMax = new Vector2(2, 2);
            rbmBorder.transform.SetAsFirstSibling();
            var rbmLabel = MkText("BestMoveLabel", resultBestMovePanel.transform, TYPE_CAPTION, TextAlignmentOptions.TopLeft, TXT_ACCENT);
            AnchorTopLeft(rbmLabel.rectTransform, new Vector2(22f, -12f), new Vector2(700, 22));
            rbmLabel.fontStyle = FontStyles.Bold; rbmLabel.characterSpacing = 2f;
            rbmLabel.text = "▲  이번 최고의 한 마디";
            var resultBestMoveText = MkText("BestMoveText", resultBestMovePanel.transform, TYPE_SMALL, TextAlignmentOptions.Center, TXT_PRIMARY);
            resultBestMoveText.rectTransform.anchorMin = new Vector2(0f, 0f);
            resultBestMoveText.rectTransform.anchorMax = new Vector2(1f, 0.65f);
            resultBestMoveText.rectTransform.offsetMin = new Vector2(22f, 0f);
            resultBestMoveText.rectTransform.offsetMax = new Vector2(-22f, 0f);
            resultBestMoveText.enableWordWrapping = true;
            resultBestMoveText.fontStyle = FontStyles.Italic;

            // 실패 내레이션 (클리어 때는 숨김)
            var resultNarration = MkText("ResultNarration", resultPanel.transform, TYPE_BODY, TextAlignmentOptions.Center, new Color(TXT_PRIMARY.r, TXT_PRIMARY.g, TXT_PRIMARY.b, 0.88f));
            AnchorBox(resultNarration.rectTransform, new Vector2(0.5f, 0.57f), new Vector2(1200, 260));
            resultNarration.enableWordWrapping = true;
            resultNarration.lineSpacing = 8f;

            var nextBtn = MkButton("NextButton", resultPanel.transform, "다음 단계 ▶", out _);
            AnchorBox(nextBtn.GetComponent<RectTransform>(), new Vector2(0.5f, 0.31f), new Vector2(380, 88));
            BtnColor(nextBtn, ACCENT);

            var retryBtn = MkButton("RetryButton", resultPanel.transform, "다시 시도", out _);
            AnchorBox(retryBtn.GetComponent<RectTransform>(), new Vector2(0.5f, 0.31f), new Vector2(380, 88));
            BtnColor(retryBtn, COL_DANGER);

            var resultSelectBtn = MkButton("ResultSelectButton", resultPanel.transform, "스테이지 선택", out _);
            AnchorBox(resultSelectBtn.GetComponent<RectTransform>(), new Vector2(0.5f, 0.17f), new Vector2(320, 76));
            BtnColor(resultSelectBtn, COL_NEUTRAL);

            // 개인 최고 기록 텍스트 (클리어 시만 표시)
            var resultPersonalBestText = MkText("ResultPersonalBestText", resultPanel.transform, TYPE_SMALL, TextAlignmentOptions.Center, TXT_SECONDARY);
            AnchorBox(resultPersonalBestText.rectTransform, new Vector2(0.5f, 0.57f), new Vector2(700, 30));
            resultPersonalBestText.characterSpacing = 1.5f;

            // 공유 / 랭킹 버튼 (클리어 시만 표시, 하단 좌우 배치)
            var shareBtn = MkButton("ShareButton", resultPanel.transform, "결과 공유", out _);
            AnchorBox(shareBtn.GetComponent<RectTransform>(), new Vector2(0.38f, 0.06f), new Vector2(260, 64));
            BtnColor(shareBtn, new Color(0.20f, 0.40f, 0.65f, 0.85f));

            var openLbBtn = MkButton("OpenLeaderboardButton", resultPanel.transform, "★ 랭킹 보기", out _);
            AnchorBox(openLbBtn.GetComponent<RectTransform>(), new Vector2(0.63f, 0.06f), new Vector2(260, 64));
            BtnColor(openLbBtn, new Color(0.62f, 0.45f, 0.12f, 0.90f));

            // ════════════════════════════════════════════════
            // STORY BEAT PANEL (웹툰 시네마틱 컷스토리 오버레이)
            // ════════════════════════════════════════════════
            var storyBeatPanel = MkPanel("StoryBeatPanel", canvasT, BG_DEEP, true);

            // 1. 웹툰 일러스트 배경 컷 (전체 화면 100% 노출)
            var storyBeatImgGO = new GameObject("StoryBeatImage", typeof(RectTransform), typeof(Image), typeof(AspectRatioFitter));
            storyBeatImgGO.transform.SetParent(storyBeatPanel.transform, false);
            Full(storyBeatImgGO.GetComponent<RectTransform>());
            var storyBeatImage = storyBeatImgGO.GetComponent<Image>();
            storyBeatImage.preserveAspect = false;
            storyBeatImage.raycastTarget  = false;
            storyBeatImage.enabled        = false;

            var storyFitter = storyBeatImgGO.GetComponent<AspectRatioFitter>();
            storyFitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            storyFitter.aspectRatio = 16f / 9f;

            // 2. 박스 티 전혀 안 나는 시네마틱 심리스 심플 타이포그래피 (Seamless Subtitle Overlay)
            var storyTextBackGO = MkPanel("TextBacking", storyBeatPanel.transform, new Color(0f, 0f, 0f, 0f), false);
            var stbRt = storyTextBackGO.GetComponent<RectTransform>();
            AnchorBox(stbRt, new Vector2(0.5f, 0.26f), new Vector2(1000, 220));

            // 헤더 뱃지 (박스 없이 깔끔한 금빛 타이포)
            var storyHeader = MkText("StoryHeader", storyTextBackGO.transform, 17, TextAlignmentOptions.Center, TXT_GOLD);
            AnchorTopLeft(storyHeader.rectTransform, new Vector2(0, 0), new Vector2(1000, 28));
            storyHeader.fontStyle = FontStyles.Bold;
            storyHeader.characterSpacing = 2f;
            storyHeader.text = "■  [ 📖 사건 개요 | CASE NARRATIVE ]  ■";

            // 배경과 완전히 하나로 어우러지는 24pt 프리미엄 시네마틱 자막 (3.5px 딥 블랙 섀도우)
            var storyBeatText = MkText("StoryBeatText", storyTextBackGO.transform, 24, TextAlignmentOptions.Center, new Color(0.99f, 0.99f, 0.98f, 1.0f));
            var sbtRt = storyBeatText.rectTransform;
            sbtRt.anchorMin = Vector2.zero; sbtRt.anchorMax = Vector2.one;
            sbtRt.offsetMin = new Vector2(0, 56); sbtRt.offsetMax = new Vector2(0, -32);
            storyBeatText.fontStyle = FontStyles.Bold;
            storyBeatText.lineSpacing = 12f;
            storyBeatText.enableWordWrapping = true;
            storyBeatText.overflowMode = TextOverflowModes.Overflow;

            var sbtOutline = storyBeatText.gameObject.AddComponent<Outline>();
            sbtOutline.effectColor = new Color(0.01f, 0.01f, 0.02f, 0.98f);
            sbtOutline.effectDistance = new Vector2(3.5f, -3.5f);

            // 심리스 조작 버튼 (하단 중앙 플로팅 버튼)
            var storyBeatContinueBtn = MkButton("StoryBeatContinueButton", storyTextBackGO.transform, "다음 컷 ▶", out var sbBtnLbl);
            var sbBtnRt = storyBeatContinueBtn.GetComponent<RectTransform>();
            sbBtnRt.anchorMin = new Vector2(0.5f, 0); sbBtnRt.anchorMax = new Vector2(0.5f, 0); sbBtnRt.pivot = new Vector2(0.5f, 0);
            sbBtnRt.sizeDelta = new Vector2(200, 46); sbBtnRt.anchoredPosition = new Vector2(0, 0);
            sbBtnLbl.fontSize = 18;
            sbBtnLbl.fontStyle = FontStyles.Bold;
            BtnColor(storyBeatContinueBtn, ACCENT);

            // ════════════════════════════════════════════════
            // STAGE SELECT PANEL
            // ════════════════════════════════════════════════
            var stageSelectPanel = MkPanel("StageSelectPanel", canvasT, BG_PANEL, true);

            var selectTitle = MkText("SelectTitle", stageSelectPanel.transform, TYPE_HEADING, TextAlignmentOptions.Center, TXT_PRIMARY);
            selectTitle.text = "스테이지 선택";
            selectTitle.fontStyle = FontStyles.Bold;
            var selTitleRt = selectTitle.rectTransform;
            selTitleRt.anchorMin = new Vector2(0.5f, 1); selTitleRt.anchorMax = new Vector2(0.5f, 1); selTitleRt.pivot = new Vector2(0.5f, 1);
            selTitleRt.sizeDelta = new Vector2(1000, 90); selTitleRt.anchoredPosition = new Vector2(0, -SAFE);

            var backBtn = MkButton("BackButton", stageSelectPanel.transform, "뒤로", out _);
            var backRt  = backBtn.GetComponent<RectTransform>();
            backRt.anchorMin = new Vector2(0, 1); backRt.anchorMax = new Vector2(0, 1); backRt.pivot = new Vector2(0, 1);
            backRt.sizeDelta = new Vector2(160, 66); backRt.anchoredPosition = new Vector2(SAFE, -SAFE - 4f);
            BtnColor(backBtn, COL_NEUTRAL);

            var scrollRectGO = new GameObject("StageScrollRect", typeof(RectTransform), typeof(ScrollRect));
            scrollRectGO.transform.SetParent(stageSelectPanel.transform, false);
            var srRt = scrollRectGO.GetComponent<RectTransform>();
            srRt.anchorMin = new Vector2(0, 0); srRt.anchorMax = new Vector2(1, 1); srRt.pivot = new Vector2(0.5f, 0.5f);
            srRt.offsetMin = new Vector2(0, 0); srRt.offsetMax = new Vector2(0, -90); // header space

            var stageViewportGO = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            stageViewportGO.transform.SetParent(scrollRectGO.transform, false);
            var stageVpRt = stageViewportGO.GetComponent<RectTransform>();
            stageVpRt.anchorMin = Vector2.zero; stageVpRt.anchorMax = Vector2.one;
            stageVpRt.sizeDelta = Vector2.zero; stageVpRt.anchoredPosition = Vector2.zero;
            var stageVpImg = stageViewportGO.GetComponent<Image>();
            stageVpImg.color = new Color(1, 1, 1, 0.005f);
            stageViewportGO.GetComponent<Mask>().showMaskGraphic = false;

            var gridGO = new GameObject("StageGrid", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            gridGO.transform.SetParent(stageViewportGO.transform, false);
            var gridRt = gridGO.GetComponent<RectTransform>();
            gridRt.anchorMin = new Vector2(0, 0); gridRt.anchorMax = new Vector2(1, 1); gridRt.pivot = new Vector2(0, 0.5f);
            gridRt.sizeDelta = Vector2.zero; gridRt.anchoredPosition = Vector2.zero;
            
            var grid = gridGO.GetComponent<HorizontalLayoutGroup>();
            grid.spacing = 80;
            grid.padding = new RectOffset(420, 420, 100, 100);
            grid.childAlignment = TextAnchor.MiddleCenter;
            grid.childControlWidth = false;
            grid.childControlHeight = false;

            var csf = gridGO.AddComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

            var sr = scrollRectGO.GetComponent<ScrollRect>();
            sr.content = gridRt;
            sr.viewport = stageVpRt;
            sr.horizontal = true;
            sr.vertical = false;
            sr.movementType = ScrollRect.MovementType.Elastic;
            sr.inertia = true;
            sr.scrollSensitivity = 40f;
            scrollRectGO.AddComponent<ScrollSnapper>();

            var stageButtonPrefab = CreateStageButtonPrefab("StageSelectButton");

            // ════════════════════════════════════════════════
            // HISTORY PANEL
            // ════════════════════════════════════════════════
            var historyPanel = MkPanel("HistoryPanel", canvasT, BG_PANEL, true);

            var histTitle = MkText("HistoryTitle", historyPanel.transform, TYPE_TITLE, TextAlignmentOptions.Center, TXT_PRIMARY);
            histTitle.fontStyle = FontStyles.Bold; histTitle.text = "대화 기록";
            AnchorBox(histTitle.rectTransform, new Vector2(0.5f, 0.92f), new Vector2(800, 80));

            CreateScrollView("HistoryScroll", historyPanel.transform, new Vector2(1300, 720), new Vector2(0, -SP_MD),
                             out var histContentRt, out var histScrollRect);

            var histCloseBtn = MkButton("HistoryCloseButton", historyPanel.transform, "닫기", out _);
            AnchorBox(histCloseBtn.GetComponent<RectTransform>(), new Vector2(0.5f, 0.06f), new Vector2(240, 74));
            BtnColor(histCloseBtn, COL_NEUTRAL);

            // ════════════════════════════════════════════════
            // MENU PANEL
            // ════════════════════════════════════════════════
            var menuPanel = MkPanel("MenuPanel", canvasT, BG_OVERLAY, true);

            var menuCard = new GameObject("Card", typeof(RectTransform), typeof(Image));
            menuCard.transform.SetParent(menuPanel.transform, false);
            AnchorBox(menuCard.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(640, 540));
            menuCard.GetComponent<Image>().color = BG_CARD;

            var menuTitle = MkText("MenuTitle", menuCard.transform, TYPE_HEADING, TextAlignmentOptions.Center, TXT_PRIMARY);
            menuTitle.fontStyle = FontStyles.Bold; menuTitle.text = "메뉴";
            AnchorBox(menuTitle.rectTransform, new Vector2(0.5f, 0.88f), new Vector2(560, 60));

            var volLabel = MkText("VolumeLabel", menuCard.transform, TYPE_SMALL, TextAlignmentOptions.Left, TXT_SECONDARY);
            AnchorBox(volLabel.rectTransform, new Vector2(0.5f, 0.73f), new Vector2(500, 30));
            volLabel.text = "소리 크기";

            var volumeSlider = MkSlider("VolumeSlider", menuCard.transform);
            volumeSlider.minValue = 0f; volumeSlider.maxValue = 1f;
            volumeSlider.wholeNumbers = false; volumeSlider.value = 1f;
            volumeSlider.interactable = true; volumeSlider.transition = Selectable.Transition.None;
            AnchorBox(volumeSlider.GetComponent<RectTransform>(), new Vector2(0.5f, 0.63f), new Vector2(500, 28));

            var guideOpenBtn = MkButton("GuideOpenButton", menuCard.transform, "게임 설명서 보기", out _);
            AnchorBox(guideOpenBtn.GetComponent<RectTransform>(), new Vector2(0.5f, 0.50f), new Vector2(480, 72));
            BtnColor(guideOpenBtn, ACCENT);

            var homeBtn = MkButton("HomeButton", menuCard.transform, "홈화면으로 나가기", out _);
            AnchorBox(homeBtn.GetComponent<RectTransform>(), new Vector2(0.5f, 0.34f), new Vector2(480, 72));
            BtnColor(homeBtn, new Color(0.3f, 0.5f, 0.8f, 1f)); // Blue color

            var quitBtn = MkButton("QuitButton", menuCard.transform, "게임 나가기", out _);
            AnchorBox(quitBtn.GetComponent<RectTransform>(), new Vector2(0.5f, 0.18f), new Vector2(480, 72));
            BtnColor(quitBtn, COL_DANGER);

            var menuCloseBtn = MkButton("MenuCloseButton", menuCard.transform, "닫기", out _);
            AnchorBox(menuCloseBtn.GetComponent<RectTransform>(), new Vector2(0.5f, 0.04f), new Vector2(480, 64));
            BtnColor(menuCloseBtn, COL_NEUTRAL);

            // ════════════════════════════════════════════════
            // GAME GUIDE PANEL (수사 지침서 팝업)
            // ════════════════════════════════════════════════
            var guidePanel = MkPanel("GuidePanel", canvasT, new Color(0.02f, 0.02f, 0.04f, 0.92f), true);

            // 마닐라 가죽 서류철 베이스 (GuideBox)
            var guideBox = MkPanel("GuideBox", guidePanel.transform, new Color(0.20f, 0.14f, 0.09f, 0.98f), false);
            var guideRt = guideBox.GetComponent<RectTransform>();
            AnchorBox(guideRt, new Vector2(0.5f, 0.50f), new Vector2(1560, 840));

            var boxBorder = MkPanel("FolderBorder", guideBox.transform, new Color(0.42f, 0.30f, 0.18f, 0.90f), false);
            Full(boxBorder.GetComponent<RectTransform>());
            boxBorder.GetComponent<RectTransform>().offsetMin = new Vector2(-2, -2);
            boxBorder.GetComponent<RectTransform>().offsetMax = new Vector2( 2,  2);

            var topLine = MkPanel("TopBorder", guideBox.transform, ACCENT, false);
            var topLineRt = topLine.GetComponent<RectTransform>();
            topLineRt.anchorMin = new Vector2(0, 1); topLineRt.anchorMax = Vector2.one;
            topLineRt.pivot = new Vector2(0.5f, 1); topLineRt.sizeDelta = new Vector2(0, 4);

            var guideHeader = MkText("DirectiveHeader", guideBox.transform, 28, TextAlignmentOptions.Left, new Color(0.98f, 0.94f, 0.88f, 1.0f));
            AnchorTopLeft(guideHeader.rectTransform, new Vector2(36, -22), new Vector2(1000, 44));
            guideHeader.fontStyle = FontStyles.Bold;
            guideHeader.characterSpacing = 1.5f;
            guideHeader.text = "■  경찰청 현장 수사관 심문 지침서  |  INTERROGATION DIRECTIVE";

            // 🔴 우측 상단 붉은색 [ 기밀 지침 ] 스탬프 뱃지
            var stampBadge = MkPanel("ConfidentialStamp", guideBox.transform, new Color(0.85f, 0.12f, 0.14f, 0.90f), false);
            AnchorTopRight(stampBadge.GetComponent<RectTransform>(), new Vector2(-36, -14), new Vector2(250, 48));
            var stampBorder = MkPanel("StampBorder", stampBadge.transform, new Color(1.0f, 0.35f, 0.35f, 0.95f), false);
            Full(stampBorder.GetComponent<RectTransform>());
            stampBorder.GetComponent<RectTransform>().offsetMin = new Vector2(-2, -2);
            stampBorder.GetComponent<RectTransform>().offsetMax = new Vector2( 2,  2);

            var stampText = MkText("StampText", stampBadge.transform, 16, TextAlignmentOptions.Center, Color.white);
            Full(stampText.rectTransform);
            stampText.fontStyle = FontStyles.Bold;
            stampText.characterSpacing = 1.5f;
            stampText.text = "🔴  [ 1급 기밀 지침 ]\nCONFIDENTIAL MANUAL";

            var divGO = MkPanel("Divider", guideBox.transform, new Color(0.55f, 0.44f, 0.32f, 0.40f), false);
            AnchorTopLeft(divGO.GetComponent<RectTransform>(), new Vector2(36, -72), new Vector2(1488, 2));

            // 내부에 얹힌 서류 종이 바탕 (Aged Paper Directive Sheet)
            var contentCard = MkPanel("ContentCard", guideBox.transform, new Color(0.94f, 0.92f, 0.86f, 0.99f), false);
            AnchorTopLeft(contentCard.GetComponent<RectTransform>(), new Vector2(36, -86), new Vector2(1488, 660));

            var cBorder = MkPanel("CardBorder", contentCard.transform, new Color(0.72f, 0.65f, 0.52f, 0.70f), false);
            Full(cBorder.GetComponent<RectTransform>());
            cBorder.GetComponent<RectTransform>().offsetMin = new Vector2(-1, -1);
            cBorder.GetComponent<RectTransform>().offsetMax = new Vector2(1, 1);
            cBorder.transform.SetAsFirstSibling();

            // 3단 세부 지침 카피 (Cream Paper Cards)
            Color CARD_BG     = new Color(0.98f, 0.96f, 0.92f, 0.98f);
            Color INK_TEXT    = new Color(0.14f, 0.12f, 0.10f, 1.0f);
            Color INK_TITLE1  = new Color(0.72f, 0.11f, 0.10f, 1.0f); // Mission Header
            Color INK_TITLE2  = new Color(0.10f, 0.18f, 0.50f, 1.0f); // Tactics Header
            Color INK_TITLE3  = new Color(0.78f, 0.16f, 0.12f, 1.0f); // Warning Header
            Color BORDER_CARD = new Color(0.78f, 0.72f, 0.60f, 0.60f);

            // Col 1: Mission
            var col1 = MkPanel("Col1_Mission", contentCard.transform, CARD_BG, false);
            AnchorTopLeft(col1.GetComponent<RectTransform>(), new Vector2(24, -20), new Vector2(464, 620));
            var c1B = MkPanel("Border", col1.transform, BORDER_CARD, false);
            Full(c1B.GetComponent<RectTransform>()); c1B.GetComponent<RectTransform>().offsetMin = new Vector2(-1, -1); c1B.GetComponent<RectTransform>().offsetMax = new Vector2(1, 1);
            c1B.transform.SetAsFirstSibling();

            var c1Title = MkText("Title", col1.transform, 23, TextAlignmentOptions.Left, INK_TITLE1);
            AnchorTopLeft(c1Title.rectTransform, new Vector2(20, -18), new Vector2(420, 38));
            c1Title.fontStyle = FontStyles.Bold;
            c1Title.text = "■  [ 1. 작전 목표 (MISSION) ]";

            var c1Body = MkText("Body", col1.transform, 20, TextAlignmentOptions.TopLeft, INK_TEXT);
            AnchorTopLeft(c1Body.rectTransform, new Vector2(20, -64), new Vector2(424, 530));
            c1Body.lineSpacing = 8f;
            c1Body.enableWordWrapping = true;
            c1Body.text =
                "<b>• 목표 자백 유도</b>\n" +
                "피의자의 성격과 저항 포인트를 분석해 결정적 진술을 이끌어내십시오.\n\n" +
                "<b>• 설득도 100% 달성</b>\n" +
                "제한된 턴 이내에 설득도 게이지를 100%까지 끌어올리면 성공합니다.";

            // Col 2: Tactics
            var col2 = MkPanel("Col2_Tactics", contentCard.transform, CARD_BG, false);
            AnchorTopLeft(col2.GetComponent<RectTransform>(), new Vector2(512, -20), new Vector2(464, 620));
            var c2B = MkPanel("Border", col2.transform, BORDER_CARD, false);
            Full(c2B.GetComponent<RectTransform>()); c2B.GetComponent<RectTransform>().offsetMin = new Vector2(-1, -1); c2B.GetComponent<RectTransform>().offsetMax = new Vector2(1, 1);
            c2B.transform.SetAsFirstSibling();

            var c2Title = MkText("Title", col2.transform, 23, TextAlignmentOptions.Left, INK_TITLE2);
            AnchorTopLeft(c2Title.rectTransform, new Vector2(20, -18), new Vector2(420, 38));
            c2Title.fontStyle = FontStyles.Bold;
            c2Title.text = "■  [ 2. 심문 기법 (TACTICS) ]";

            var c2Body = MkText("Body", col2.transform, 20, TextAlignmentOptions.TopLeft, INK_TEXT);
            AnchorTopLeft(c2Body.rectTransform, new Vector2(20, -64), new Vector2(424, 530));
            c2Body.lineSpacing = 8f;
            c2Body.enableWordWrapping = true;
            c2Body.text =
                "<b>• 맞춤형 접근법</b>\n" +
                "감정 / 자존심 / 논리 / 신뢰 중 대상의 심리 약점에 맞춰 질문하십시오.\n\n" +
                "<b>• 어조 & 행동 묘사</b>\n" +
                "괄호로 행동을 입력하세요.\n" +
                "예: <color=#8A4B00>(차분하게)</color> 또는 <color=#8A4B00>(책상을 두드리며)</color>\n\n" +
                "<b>• 공략 힌트 해금</b>\n" +
                "설득도 50% 이상 달성 시 실시간 힌트 노출.";

            // Col 3: Warnings
            var col3 = MkPanel("Col3_Warnings", contentCard.transform, CARD_BG, false);
            AnchorTopLeft(col3.GetComponent<RectTransform>(), new Vector2(1000, -20), new Vector2(464, 620));
            var c3B = MkPanel("Border", col3.transform, BORDER_CARD, false);
            Full(c3B.GetComponent<RectTransform>()); c3B.GetComponent<RectTransform>().offsetMin = new Vector2(-1, -1); c3B.GetComponent<RectTransform>().offsetMax = new Vector2(1, 1);
            c3B.transform.SetAsFirstSibling();

            var c3Title = MkText("Title", col3.transform, 23, TextAlignmentOptions.Left, INK_TITLE3);
            AnchorTopLeft(c3Title.rectTransform, new Vector2(20, -18), new Vector2(420, 38));
            c3Title.fontStyle = FontStyles.Bold;
            c3Title.text = "■  [ 3. 주의 사항 (WARNINGS) ]";

            var c3Body = MkText("Body", col3.transform, 20, TextAlignmentOptions.TopLeft, INK_TEXT);
            AnchorTopLeft(c3Body.rectTransform, new Vector2(20, -64), new Vector2(424, 530));
            c3Body.lineSpacing = 8f;
            c3Body.enableWordWrapping = true;
            c3Body.text =
                "<b>• 반발 및 감정 악화</b>\n" +
                "무분별한 위협, 욕설, 부적절한 언행은 피의자를 자극해 설득도를 감소시킵니다.\n\n" +
                "<b>• 심문 실패 조건</b>\n" +
                "3연속 설득도 0% 유지 또는 제한 턴 초과 시 심문 실패 처리됩니다.";

            var guideCloseBtn = MkButton("GuideCloseButton", guideBox.transform, "[ 📁 지침서 확인 완료 ]", out var guideCloseLbl);
            AnchorBox(guideCloseBtn.GetComponent<RectTransform>(), new Vector2(0.5f, 0.05f), new Vector2(340, 60));
            guideCloseLbl.fontSize = 22;
            guideCloseLbl.fontStyle = FontStyles.Bold;
            BtnColor(guideCloseBtn, ACCENT);

            // ════════════════════════════════════════════════
            // TRAIT PANEL — 경찰청 수사 서류 파일 v2 (세련된 Dossier)
            // ════════════════════════════════════════════════
            var traitPanel = MkPanel("TraitPanel", canvasT, new Color(0.05f, 0.03f, 0.02f, 0.97f), true);

            // ── Dossier 전용 색 ──
            Color D_PAPER  = new Color(0.95f, 0.92f, 0.85f, 1.00f);
            Color D_PAPER2 = new Color(0.89f, 0.85f, 0.77f, 1.00f);   // 마진 영역 (약간 어두운 종이)
            Color D_INK    = new Color(0.09f, 0.08f, 0.07f, 1.00f);
            Color D_RED    = new Color(0.60f, 0.06f, 0.06f, 1.00f);
            Color D_GOLD   = new Color(0.80f, 0.65f, 0.20f, 1.00f);   // 경찰청 금색 라인
            Color D_MUT    = new Color(0.38f, 0.32f, 0.26f, 1.00f);
            Color D_RULE   = new Color(0.58f, 0.52f, 0.42f, 0.38f);
            Color D_MARGIN = new Color(0.68f, 0.12f, 0.12f, 0.55f);
            Color D_TAPE   = new Color(0.99f, 0.97f, 0.87f, 0.65f);
            Color D_STAMP  = new Color(0.58f, 0.05f, 0.05f, 0.21f);
            Color D_AGENCY = new Color(0.48f, 0.04f, 0.04f, 1.00f);

            // ── 1) 낙차 그림자 ──
            {
                var sh = new GameObject("PaperShadow", typeof(RectTransform), typeof(Image));
                sh.transform.SetParent(traitPanel.transform, false);
                var shRt = sh.GetComponent<RectTransform>();
                shRt.anchorMin = shRt.anchorMax = shRt.pivot = new Vector2(0.5f, 0.5f);
                shRt.sizeDelta = new Vector2(916, 960); shRt.anchoredPosition = new Vector2(10f, -9f);
                sh.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.52f);
                sh.GetComponent<Image>().raycastTarget = false;
            }

            // ── 2) 왼쪽 마진 영역 (살짝 어두운 종이색, 심 바인더 느낌) ──
            var traitBox = new GameObject("TraitBox", typeof(RectTransform), typeof(Image));
            traitBox.transform.SetParent(traitPanel.transform, false);
            var tbRt = traitBox.GetComponent<RectTransform>();
            tbRt.anchorMin = tbRt.anchorMax = tbRt.pivot = new Vector2(0.5f, 0.5f);
            tbRt.sizeDelta = new Vector2(920, 950); tbRt.anchoredPosition = Vector2.zero;
            traitBox.GetComponent<Image>().color = D_PAPER2;
            traitBox.GetComponent<Image>().raycastTarget = false;

            // 오른쪽 메인 종이면 (마진 제외)
            {
                var mainPaper = new GameObject("MainPaper", typeof(RectTransform), typeof(Image));
                mainPaper.transform.SetParent(traitBox.transform, false);
                var mpRt = mainPaper.GetComponent<RectTransform>();
                mpRt.anchorMin = new Vector2(0, 0); mpRt.anchorMax = new Vector2(1, 1);
                mpRt.offsetMin = new Vector2(42, 0); mpRt.offsetMax = Vector2.zero;
                mainPaper.GetComponent<Image>().color = D_PAPER;
                mainPaper.GetComponent<Image>().raycastTarget = false;
            }

            // ── 3) 스테이플 (상단 2개) ──
            foreach (var sx in new float[] { 320f, 380f })
            {
                var stp = new GameObject("Staple", typeof(RectTransform), typeof(Image));
                stp.transform.SetParent(traitBox.transform, false);
                var stpRt = stp.GetComponent<RectTransform>();
                stpRt.anchorMin = stpRt.anchorMax = new Vector2(0.5f, 1f);
                stpRt.pivot = new Vector2(0.5f, 1f);
                stpRt.sizeDelta = new Vector2(36, 9); stpRt.anchoredPosition = new Vector2(sx - 350f, -6f);
                stp.GetComponent<Image>().color = new Color(0.48f, 0.50f, 0.52f, 0.85f);
                stp.GetComponent<Image>().raycastTarget = false;
            }

            // ── 4) 테이프 스트립 (최상단) ──
            {
                var tape = new GameObject("Tape", typeof(RectTransform), typeof(Image));
                tape.transform.SetParent(traitBox.transform, false);
                var tRt = tape.GetComponent<RectTransform>();
                tRt.anchorMin = new Vector2(0, 1); tRt.anchorMax = Vector2.one;
                tRt.pivot = new Vector2(0.5f, 1); tRt.sizeDelta = new Vector2(0, 20);
                tRt.anchoredPosition = Vector2.zero;
                tape.GetComponent<Image>().color = D_TAPE;
                tape.GetComponent<Image>().raycastTarget = false;
            }

            // ── 5) 경찰청 헤더 바 (agency bar) ──
            {
                var agBar = new GameObject("AgencyBar", typeof(RectTransform), typeof(Image));
                agBar.transform.SetParent(traitBox.transform, false);
                var abRt = agBar.GetComponent<RectTransform>();
                abRt.anchorMin = new Vector2(0, 1); abRt.anchorMax = Vector2.one;
                abRt.pivot = new Vector2(0.5f, 1); abRt.sizeDelta = new Vector2(0, 60);
                abRt.anchoredPosition = new Vector2(0, -20);
                agBar.GetComponent<Image>().color = D_AGENCY;
                agBar.GetComponent<Image>().raycastTarget = false;

                // 상단 금색 줄
                var agGold = new GameObject("GoldLine", typeof(RectTransform), typeof(Image));
                agGold.transform.SetParent(agBar.transform, false);
                var agGRt = agGold.GetComponent<RectTransform>();
                agGRt.anchorMin = new Vector2(0, 1); agGRt.anchorMax = Vector2.one;
                agGRt.pivot = new Vector2(0.5f, 1); agGRt.sizeDelta = new Vector2(0, 3);
                agGRt.anchoredPosition = Vector2.zero;
                agGold.GetComponent<Image>().color = D_GOLD;
                agGold.GetComponent<Image>().raycastTarget = false;

                // 하단 금색 줄
                var agGoldB = new GameObject("GoldLineB", typeof(RectTransform), typeof(Image));
                agGoldB.transform.SetParent(agBar.transform, false);
                var agGBRt = agGoldB.GetComponent<RectTransform>();
                agGBRt.anchorMin = Vector2.zero; agGBRt.anchorMax = new Vector2(1, 0);
                agGBRt.pivot = new Vector2(0.5f, 0); agGBRt.sizeDelta = new Vector2(0, 3);
                agGBRt.anchoredPosition = Vector2.zero;
                agGoldB.GetComponent<Image>().color = D_GOLD;
                agGoldB.GetComponent<Image>().raycastTarget = false;

                var agTxt = MkText("AgencyName", agBar.transform, 14, TextAlignmentOptions.Center,
                                   new Color(1f, 0.96f, 0.88f, 0.96f));
                Full(agTxt.rectTransform); agTxt.rectTransform.offsetMin = new Vector2(0, 8); agTxt.rectTransform.offsetMax = new Vector2(0, -8);
                agTxt.fontStyle = FontStyles.Bold; agTxt.characterSpacing = 3.5f;
                agTxt.text = "대한민국 경찰청     KOREA NATIONAL POLICE AGENCY     수사과";
            }

            // ── 6) 문서 제목 ──
            var traitTitleText = MkText("TitleText", traitBox.transform, 23, TextAlignmentOptions.Left,
                                        new Color(D_RED.r, D_RED.g, D_RED.b, 1f));
            AnchorTopLeft(traitTitleText.rectTransform, new Vector2(96, -94), new Vector2(520, 40));
            traitTitleText.fontStyle = FontStyles.Bold; traitTitleText.characterSpacing = 0.8f;
            traitTitleText.text = "■  피의자 수사 분석 보고서";

            // ── 7) CASE 메타 (작은 메타데이터 2줄) ──
            {
                var cm = MkText("CaseMeta", traitBox.transform, 11, TextAlignmentOptions.Left, D_MUT);
                AnchorTopLeft(cm.rectTransform, new Vector2(96, -138), new Vector2(460, 20));
                cm.characterSpacing = 1.5f;
                cm.text = "CASE FILE  //  KBI-2024-RESTRICTED  //  수사 전담 요원 한정 열람";
            }

            // ── 8) 수평 구분선 (두껍게) ──
            {
                var div1 = new GameObject("Div1", typeof(RectTransform), typeof(Image));
                div1.transform.SetParent(traitBox.transform, false);
                AnchorTopLeft(div1.GetComponent<RectTransform>(), new Vector2(58, -166), new Vector2(800, 3));
                div1.GetComponent<Image>().color = D_RED;
                div1.GetComponent<Image>().raycastTarget = false;

                var div2 = new GameObject("Div2", typeof(RectTransform), typeof(Image));
                div2.transform.SetParent(traitBox.transform, false);
                AnchorTopLeft(div2.GetComponent<RectTransform>(), new Vector2(58, -170), new Vector2(800, 1));
                div2.GetComponent<Image>().color = new Color(D_RED.r, D_RED.g, D_RED.b, 0.35f);
                div2.GetComponent<Image>().raycastTarget = false;
            }

            // ── 9) 폴라로이드 사진 슬롯 (우측 상단) — 얼굴 스캔라인 포함 ──
            Image traitPhotoImageRef = null;
            {
                var pFrame = new GameObject("PhotoFrame", typeof(RectTransform), typeof(Image));
                pFrame.transform.SetParent(traitBox.transform, false);
                AnchorTopRight(pFrame.GetComponent<RectTransform>(), new Vector2(-52, -80), new Vector2(136, 170));
                pFrame.GetComponent<Image>().color = new Color(0.18f, 0.16f, 0.14f, 1f);
                pFrame.GetComponent<Image>().raycastTarget = false;

                var pInner = new GameObject("PhotoGrey", typeof(RectTransform), typeof(Image));
                pInner.transform.SetParent(pFrame.transform, false);
                Full(pInner.GetComponent<RectTransform>());
                pInner.GetComponent<RectTransform>().offsetMin = new Vector2(5, 26);
                pInner.GetComponent<RectTransform>().offsetMax = new Vector2(-5, -5);
                var pInnerImg = pInner.GetComponent<Image>();
                pInnerImg.color = Color.white;
                pInnerImg.raycastTarget = false;
                traitPhotoImageRef = pInnerImg;

                // 얼굴 스캔라인 (3줄 수평) — 사진 분석 감식 느낌
                foreach (var scanY in new float[] { 0.65f, 0.45f, 0.28f })
                {
                    var sl = new GameObject("ScanLine", typeof(RectTransform), typeof(Image));
                    sl.transform.SetParent(pInner.transform, false);
                    var slRt = sl.GetComponent<RectTransform>();
                    slRt.anchorMin = new Vector2(0, scanY); slRt.anchorMax = new Vector2(1, scanY);
                    slRt.pivot = new Vector2(0.5f, 0.5f); slRt.sizeDelta = new Vector2(0, 1);
                    slRt.anchoredPosition = Vector2.zero;
                    sl.GetComponent<Image>().color = new Color(0.62f, 0.60f, 0.56f, 0.55f);
                    sl.GetComponent<Image>().raycastTarget = false;
                }

                var pLabel = MkText("PhotoLabel", pFrame.transform, 9, TextAlignmentOptions.Center,
                                    new Color(0.78f, 0.75f, 0.70f, 0.90f));
                AnchorTopLeft(pLabel.rectTransform, new Vector2(0, -148), new Vector2(136, 24));
                pLabel.fontStyle = FontStyles.Bold; pLabel.characterSpacing = 1.5f;
                pLabel.text = "EVIDENCE PHOTO";

                var pSub = MkText("PhotoSub", pFrame.transform, 8, TextAlignmentOptions.Center,
                                  new Color(0.62f, 0.58f, 0.52f, 0.88f));
                AnchorTopLeft(pSub.rectTransform, new Vector2(0, -161), new Vector2(136, 16));
                pSub.text = "용의자 식별 사진";
            }

            // ── 10) 붉은 왼쪽 마진 선 ──
            {
                var mLine = new GameObject("MarginLine", typeof(RectTransform), typeof(Image));
                mLine.transform.SetParent(traitBox.transform, false);
                AnchorTopLeft(mLine.GetComponent<RectTransform>(), new Vector2(88, -172), new Vector2(2, 672));
                mLine.GetComponent<Image>().color = D_MARGIN;
                mLine.GetComponent<Image>().raycastTarget = false;
            }

            // ── 11) 펀치 구멍 3개 — TMP Text "●" (원형) ──
            foreach (var hy in new float[] { -270f, -470f, -670f })
            {
                var hole = MkText("PunchHole", traitBox.transform, 26, TextAlignmentOptions.Center,
                                  new Color(0.05f, 0.03f, 0.02f, 0.92f));
                hole.rectTransform.anchorMin = hole.rectTransform.anchorMax = new Vector2(0, 1);
                hole.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                hole.rectTransform.sizeDelta = new Vector2(28, 28);
                hole.rectTransform.anchoredPosition = new Vector2(16, hy);
                hole.text = "●"; hole.enableWordWrapping = false;
            }

            // ── 12) 룰드 라인 (14줄, 더 선명하게) ──
            for (int li = 0; li < 14; li++)
            {
                var rl = new GameObject("RuledLine" + li, typeof(RectTransform), typeof(Image));
                rl.transform.SetParent(traitBox.transform, false);
                AnchorTopLeft(rl.GetComponent<RectTransform>(), new Vector2(90, -175f - li * 48f), new Vector2(768, 1));
                rl.GetComponent<Image>().color = D_RULE;
                rl.GetComponent<Image>().raycastTarget = false;
            }

            // ── 13) 기밀 도장 — 테두리 상자 + 텍스트 (더 강렬하게) ──
            {
                // 도장 외곽 박스
                var stampFrame = new GameObject("StampFrame", typeof(RectTransform), typeof(Image));
                stampFrame.transform.SetParent(traitBox.transform, false);
                var sfRt = stampFrame.GetComponent<RectTransform>();
                sfRt.anchorMin = sfRt.anchorMax = sfRt.pivot = new Vector2(0.5f, 0.5f);
                sfRt.sizeDelta = new Vector2(580, 110); sfRt.anchoredPosition = new Vector2(-20, 60);
                sfRt.transform.localEulerAngles = new Vector3(0, 0, -20f);
                stampFrame.GetComponent<Image>().color = new Color(D_RED.r, D_RED.g, D_RED.b, 0.13f);
                stampFrame.GetComponent<Image>().raycastTarget = false;

                // 도장 내부 (살짝 다른 색)
                var stampInner = new GameObject("StampInner", typeof(RectTransform), typeof(Image));
                stampInner.transform.SetParent(stampFrame.transform, false);
                Full(stampInner.GetComponent<RectTransform>());
                stampInner.GetComponent<RectTransform>().offsetMin = new Vector2(6, 6);
                stampInner.GetComponent<RectTransform>().offsetMax = new Vector2(-6, -6);
                stampInner.GetComponent<Image>().color = new Color(D_RED.r, D_RED.g, D_RED.b, 0.07f);
                stampInner.GetComponent<Image>().raycastTarget = false;

                // 도장 텍스트
                var stampTxt = MkText("ClassifiedStamp", traitBox.transform, 70, TextAlignmentOptions.Center, D_STAMP);
                var stRt = stampTxt.rectTransform;
                stRt.anchorMin = stRt.anchorMax = stRt.pivot = new Vector2(0.5f, 0.5f);
                stRt.sizeDelta = new Vector2(600, 120); stRt.anchoredPosition = new Vector2(-20, 60);
                stampTxt.fontStyle = FontStyles.Bold; stampTxt.characterSpacing = 12f;
                stampTxt.text = "기  밀  //  SECRET";
                stampTxt.enableWordWrapping = false;
                stampTxt.transform.localEulerAngles = new Vector3(0, 0, -20f);
            }

            // ── 14) 본문 텍스트 ──
            var traitText = MkText("TraitText", traitBox.transform, 20, TextAlignmentOptions.TopLeft, D_INK);
            AnchorTopLeft(traitText.rectTransform, new Vector2(96, -174), new Vector2(690, 666));
            traitText.enableWordWrapping = true; traitText.lineSpacing = 10f;

            // ── 15) 하단 분류 띠 (SECRET footer) ──
            {
                var footerBar = new GameObject("FooterBar", typeof(RectTransform), typeof(Image));
                footerBar.transform.SetParent(traitBox.transform, false);
                var fbRt = footerBar.GetComponent<RectTransform>();
                fbRt.anchorMin = Vector2.zero; fbRt.anchorMax = new Vector2(1, 0);
                fbRt.pivot = new Vector2(0.5f, 0); fbRt.sizeDelta = new Vector2(0, 34);
                fbRt.anchoredPosition = new Vector2(0, 78);
                footerBar.GetComponent<Image>().color = D_AGENCY;
                footerBar.GetComponent<Image>().raycastTarget = false;

                // 금색 상단선
                var fGold = new GameObject("FooterGold", typeof(RectTransform), typeof(Image));
                fGold.transform.SetParent(footerBar.transform, false);
                var fgRt = fGold.GetComponent<RectTransform>();
                fgRt.anchorMin = new Vector2(0, 1); fgRt.anchorMax = Vector2.one;
                fgRt.pivot = new Vector2(0.5f, 1); fgRt.sizeDelta = new Vector2(0, 2);
                fgRt.anchoredPosition = Vector2.zero;
                fGold.GetComponent<Image>().color = D_GOLD;
                fGold.GetComponent<Image>().raycastTarget = false;

                var footerTxt = MkText("FooterText", footerBar.transform, 11, TextAlignmentOptions.Center,
                                       new Color(1f, 0.96f, 0.88f, 0.75f));
                Full(footerTxt.rectTransform);
                footerTxt.fontStyle = FontStyles.Bold; footerTxt.characterSpacing = 3f;
                footerTxt.text = "SECRET  //  FOR OFFICIAL USE ONLY  //  열람 후 즉시 파기";
            }

            // ── 16) 닫기 버튼 ──
            var traitCloseBtn = MkButton("TraitCloseButton", traitBox.transform, "서류철 닫기", out var traitCloseLbl);
            AnchorBox(traitCloseBtn.GetComponent<RectTransform>(), new Vector2(0.5f, 0.058f), new Vector2(300, 56));
            traitCloseLbl.fontSize = 20;
            traitCloseLbl.fontStyle = FontStyles.Bold; traitCloseLbl.characterSpacing = 2f;
            BtnColor(traitCloseBtn, D_RED);

            // ════════════════════════════════════════════════
            // LEADERBOARD PANEL (글로벌 랭킹)
            // ════════════════════════════════════════════════
            var leaderboardPanel = MkPanel("LeaderboardPanel", canvasT, BG_OVERLAY, true);

            var lbCard = MkPanel("Card", leaderboardPanel.transform, BG_CARD, false);
            AnchorBox(lbCard.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(920, 960));

            var lbTopBorder = MkPanel("TopBorder", lbCard.transform, ACCENT, false);
            var lbTopBorderRt = lbTopBorder.GetComponent<RectTransform>();
            lbTopBorderRt.anchorMin = new Vector2(0, 1); lbTopBorderRt.anchorMax = Vector2.one;
            lbTopBorderRt.pivot = new Vector2(0.5f, 1); lbTopBorderRt.sizeDelta = new Vector2(0, 4);

            var lbTitle = MkText("LbTitle", lbCard.transform, TYPE_TITLE, TextAlignmentOptions.Center, TXT_GOLD);
            lbTitle.fontStyle = FontStyles.Bold;
            lbTitle.text = "★  랭킹";
            AnchorBox(lbTitle.rectTransform, new Vector2(0.5f, 0.92f), new Vector2(840, 72));

            var lbMyRecord = MkText("LbMyRecord", lbCard.transform, TYPE_SMALL, TextAlignmentOptions.Center, TXT_SECONDARY);
            AnchorBox(lbMyRecord.rectTransform, new Vector2(0.5f, 0.845f), new Vector2(840, 30));
            lbMyRecord.text = "이번 기록 :  -";

            // 닉네임 입력 + 등록 버튼 (한 줄)
            var lbNameInput = MkInputField("LbNameInput", lbCard.transform, "닉네임 (최대 12자)");
            AnchorBox(lbNameInput.GetComponent<RectTransform>(), new Vector2(0.40f, 0.775f), new Vector2(500, 58));
            lbNameInput.characterLimit = 12;

            var lbSubmitBtn = MkButton("LbSubmitButton", lbCard.transform, "내 기록 등록", out var lbSubmitLbl);
            AnchorBox(lbSubmitBtn.GetComponent<RectTransform>(), new Vector2(0.775f, 0.775f), new Vector2(230, 58));
            lbSubmitLbl.fontSize = TYPE_SMALL;
            BtnColor(lbSubmitBtn, ACCENT);

            // 랭킹 리스트 (스크롤 + 단일 텍스트)
            CreateScrollView("LbScroll", lbCard.transform, new Vector2(840, 520), new Vector2(0, -70),
                             out var lbContentRt, out var lbScrollRect);
            var lbListText = MkText("LbListText", lbContentRt, TYPE_BODY, TextAlignmentOptions.TopLeft, TXT_PRIMARY);
            lbListText.lineSpacing = 14f;
            lbListText.text = "불러오는 중…";

            var lbCloseBtn = MkButton("LbCloseButton", lbCard.transform, "닫기", out _);
            AnchorBox(lbCloseBtn.GetComponent<RectTransform>(), new Vector2(0.5f, 0.05f), new Vector2(300, 62));
            BtnColor(lbCloseBtn, COL_NEUTRAL);

            // ════════════════════════════════════════════════
            // 채팅 대사 프리팹
            // ════════════════════════════════════════════════
            var playerPrefab = CreateBubblePrefab("PlayerBubble", true);
            var npcPrefab    = CreateBubblePrefab("NpcBubble", false);

            // ────────────────────────────────────────────────
            gameData = AssetDatabase.LoadAssetAtPath<GameData>("Assets/Resources/GameData.asset")
                    ?? AssetDatabase.LoadAssetAtPath<GameData>(GameDataPath);
            library = AssetDatabase.LoadAssetAtPath<StagePortraitLibrary>("Assets/Resources/StagePortraitLibrary.asset")
                    ?? AssetDatabase.LoadAssetAtPath<StagePortraitLibrary>(LibraryPath);
            transitionLibrary = AssetDatabase.LoadAssetAtPath<TransitionArtLibrary>("Assets/Resources/TransitionArtLibrary.asset")
                    ?? AssetDatabase.LoadAssetAtPath<TransitionArtLibrary>(TransitionLibraryPath);

            // ════════════════════════════════════════════════
            // 시스템
            // ════════════════════════════════════════════════
            var systemsGO = new GameObject("Systems");
            var manager   = systemsGO.AddComponent<PersuasionManager>();
            var llm       = systemsGO.AddComponent<LLMClient>();
            var portrait  = systemsGO.AddComponent<CharacterPortrait>();
            var audioMgr  = systemsGO.AddComponent<AudioManager>();
            var lbManager = systemsGO.AddComponent<LeaderboardManager>();

            Wire(manager, "gameData", gameData);
            Wire(manager, "llmClient", llm);
            Wire(portrait, "manager", manager);
            Wire(portrait, "library", library);
            Wire(portrait, "targetImage", bgImage);

            var audioLib = AssetDatabase.LoadAssetAtPath<AudioLibrary>("Assets/Resources/AudioLibrary.asset");
            if (audioLib == null) Debug.LogWarning("[Persuasion] AudioLibrary 없음. '8) Setup Audio' 실행 권장.");
            Wire(audioMgr, "manager", manager);
            Wire(audioMgr, "library", audioLib);

            // ════════════════════════════════════════════════
            // PersuasionUI 배선
            // ════════════════════════════════════════════════
            var uiGO = new GameObject("PersuasionUI");
            uiGO.transform.SetParent(canvasT, false);
            var ui = uiGO.AddComponent<PersuasionUI>();

            Wire(ui, "manager",                manager);
            Wire(ui, "introPanel",             introPanel);
            Wire(ui, "gameTitleText",          titleText);
            Wire(ui, "introTitleGroup",        introTitleGroup);
            Wire(ui, "startButton",            startBtn);
            Wire(ui, "introBackgroundImage",   introBgImg);
            Wire(ui, "introMenuButton",        introMenuBtn);
            Wire(ui, "transitionArtLibrary",   transitionLibrary);
            Wire(ui, "storyBeatPanel",         storyBeatPanel);
            Wire(ui, "storyBeatImage",         storyBeatImage);
            Wire(ui, "storyBeatText",          storyBeatText);
            Wire(ui, "storyBeatContinueButton",storyBeatContinueBtn);
            Wire(ui, "stageIntroOverlay",      stageIntroOverlay);
            Wire(ui, "stageIntroGroup",        stageIntroGroup);
            Wire(ui, "introStageTitleText",    introTitleText);
            Wire(ui, "introStageSubTitleText", introSubTitleText);
            Wire(ui, "introGoalText",          introGoalText);
            Wire(ui, "introGoalCardRt",        introGoalRt);
            Wire(ui, "playPanel",              playPanel);
            Wire(ui, "chapterTitleText",       chapterText);
            Wire(ui, "stageTitleText",         stageText);
            Wire(ui, "characterNameText",      charNameText);
            Wire(ui, "backgroundNarrationText",narrationText);
            Wire(ui, "goalText",               goalText);
            Wire(ui, "chatContent",            contentGO.transform);
            Wire(ui, "chatScrollRect",         scrollRect);
            Wire(ui, "playerBubblePrefab",     playerPrefab);
            Wire(ui, "npcBubblePrefab",        npcPrefab);
            Wire(ui, "inputField",             inputField);
            Wire(ui, "sendButton",             sendBtn);
            Wire(ui, "storyPeekButton",        storyPeekBtn);
            Wire(ui, "traitButton",            traitBtn);
            Wire(ui, "historyButton",          historyBtn);
            Wire(ui, "menuButton",             menuBtn);
            Wire(ui, "persuasionSlider",       slider);
            Wire(ui, "persuasionValueText",    persuasionValueText);
            Wire(ui, "emotionLabel",           emotionLabel);
            Wire(ui, "hintPanel",              hintPanel);
            Wire(ui, "hintText",               hintText);
            Wire(ui, "traitPanel",             traitPanel);
            Wire(ui, "traitTitleText",         traitTitleText);
            Wire(ui, "traitText",              traitText);
            Wire(ui, "traitPhotoImage",        traitPhotoImageRef);
            Wire(ui, "traitCloseButton",       traitCloseBtn);
            Wire(ui, "historyPanel",           historyPanel);
            Wire(ui, "historyContent",         histContentRt);
            Wire(ui, "historyScrollRect",      histScrollRect);
            Wire(ui, "historyCloseButton",     histCloseBtn);
            Wire(ui, "menuPanel",              menuPanel);
            Wire(ui, "volumeSlider",           volumeSlider);
            Wire(ui, "homeButton",             homeBtn);
            Wire(ui, "quitButton",             quitBtn);
            Wire(ui, "menuCloseButton",        menuCloseBtn);
            Wire(ui, "guidePanel",             guidePanel);
            Wire(ui, "guideOpenButton",        guideOpenBtn);
            Wire(ui, "guideCloseButton",       guideCloseBtn);
            Wire(ui, "resultPanel",            resultPanel);
            Wire(ui, "resultText",             resultText);
            Wire(ui, "resultNarrationText",    resultNarration);
            Wire(ui, "resultGradeText",        resultGradeText);
            Wire(ui, "resultStatsText",        resultStatsText);
            Wire(ui, "resultBestMovePanel",      resultBestMovePanel);
            Wire(ui, "resultBestMoveText",      resultBestMoveText);
            Wire(ui, "resultPersonalBestText",  resultPersonalBestText);
            Wire(ui, "clearFlashOverlay",       clearFlashImg);
            Wire(ui, "nextButton",              nextBtn);
            Wire(ui, "retryButton",             retryBtn);
            Wire(ui, "resultSelectButton",      resultSelectBtn);
            Wire(ui, "shareButton",             shareBtn);
            Wire(ui, "privacyButton",           privacyBtn);
            Wire(ui, "leaderboard",             lbManager);
            Wire(ui, "openLeaderboardButton",   openLbBtn);
            Wire(ui, "leaderboardPanel",        leaderboardPanel);
            Wire(ui, "leaderboardTitleText",    lbTitle);
            Wire(ui, "leaderboardMyRecordText", lbMyRecord);
            Wire(ui, "leaderboardListText",     lbListText);
            Wire(ui, "leaderboardNameInput",    lbNameInput);
            Wire(ui, "leaderboardSubmitButton", lbSubmitBtn);
            Wire(ui, "leaderboardCloseButton",  lbCloseBtn);
            Wire(ui, "stageSelectPanel",       stageSelectPanel);
            Wire(ui, "stageButtonContainer",   gridGO.transform);
            Wire(ui, "stageButtonPrefab",      stageButtonPrefab);
            Wire(ui, "portraitLibrary",        library);
            Wire(ui, "stageSelectBackButton",  backBtn);

            // 저장 + 빌드 세팅 등록
            if (!System.IO.Directory.Exists(Application.dataPath + "/Scenes"))
                AssetDatabase.CreateFolder("Assets", "Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuild(ScenePath);

            Debug.Log("[Persuasion] PlayScene 생성 완료: " + ScenePath + "  ▶ 이제 상단 Play 버튼으로 실행하세요.");
        }

        // ── 레이아웃 헬퍼 ───────────────────────────────────────────

        private static RectTransform Full(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            return rt;
        }

        private static void AnchorBox(RectTransform rt, Vector2 anchor, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor; rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size; rt.anchoredPosition = Vector2.zero;
        }

        private static void AnchorTopLeft(RectTransform rt, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0, 1);
            rt.sizeDelta = size; rt.anchoredPosition = pos;
        }

        private static void AnchorTopRight(RectTransform rt, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(1, 1); rt.pivot = new Vector2(1, 1);
            rt.sizeDelta = size; rt.anchoredPosition = pos;
        }

        // ── UI 팩토리 ────────────────────────────────────────────────

        private static GameObject MkPanel(string name, Transform parent, Color bg, bool blockRaycast)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            Full(go.GetComponent<RectTransform>());
            var img = go.GetComponent<Image>();
            img.color = bg;
            img.raycastTarget = blockRaycast;
            return go;
        }

        private static TextMeshProUGUI MkText(string name, Transform parent, int size, TextAlignmentOptions align, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<TextMeshProUGUI>();
            if (_font != null) t.font = _font;
            t.fontSize            = size;
            t.alignment           = align;
            t.color               = color;
            t.text                = "";
            t.enableWordWrapping  = true;
            t.overflowMode        = TextOverflowModes.Overflow;
            t.raycastTarget       = false;
            return t;
        }

        private static Button MkButton(string name, Transform parent, string label, out TextMeshProUGUI lbl)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = COL_NEUTRAL;
            img.raycastTarget = true;

            // Stamp border: brighter thin line around each button
            var bdrGO = new GameObject("Border", typeof(RectTransform), typeof(Image));
            bdrGO.transform.SetParent(go.transform, false);
            bdrGO.transform.SetAsFirstSibling();
            Full(bdrGO.GetComponent<RectTransform>());
            bdrGO.GetComponent<RectTransform>().offsetMin = new Vector2(-2, -2);
            bdrGO.GetComponent<RectTransform>().offsetMax = new Vector2( 2,  2);
            var bdrImg = bdrGO.GetComponent<Image>();
            bdrImg.color = new Color(1f, 1f, 1f, 0.10f);
            bdrImg.raycastTarget = false;

            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img;
            btn.colors = MkColorBlock(COL_NEUTRAL);

            lbl = MkText("Label", go.transform, TYPE_BODY, TextAlignmentOptions.Center, TXT_PRIMARY);
            Full(lbl.rectTransform);
            lbl.text           = label;
            lbl.fontStyle      = FontStyles.Bold;
            lbl.characterSpacing = 2f;
            lbl.raycastTarget  = false;
            return btn;
        }

        private static ColorBlock MkColorBlock(Color normal)
        {
            var cb = ColorBlock.defaultColorBlock;
            cb.normalColor      = normal;
            cb.highlightedColor = Color.Lerp(normal, Color.white, 0.20f);
            cb.pressedColor     = Color.Lerp(normal, Color.black, 0.25f);
            cb.selectedColor    = normal;
            cb.disabledColor    = new Color(0.28f, 0.28f, 0.30f, 0.55f);
            cb.colorMultiplier  = 1f;
            cb.fadeDuration     = 0.08f;
            return cb;
        }

        private static void BtnColor(Button btn, Color c)
        {
            btn.GetComponent<Image>().color = c;
            btn.colors = MkColorBlock(c);
            // Update stamp border to a brighter derived color
            var bdr = btn.transform.Find("Border");
            if (bdr != null)
                bdr.GetComponent<Image>().color = new Color(
                    Mathf.Min(c.r * 1.5f + 0.15f, 1f),
                    Mathf.Min(c.g * 1.5f + 0.15f, 1f),
                    Mathf.Min(c.b * 1.5f + 0.15f, 1f),
                    0.55f);
        }

        private static Button MkBarButton(string name, Transform parent, string label, float width, Color color)
        {
            var btn = MkButton(name, parent, label, out var lbl);
            lbl.fontSize = TYPE_SMALL;
            BtnColor(btn, color);
            var le = btn.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = width; le.preferredHeight = 46f;
            return btn;
        }

        private static Slider MkSlider(string name, Transform parent, bool persuasionMode = false)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Slider));
            go.transform.SetParent(parent, false);
            var slider = go.GetComponent<Slider>();

            var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(go.transform, false);
            Full(bg.GetComponent<RectTransform>());
            bg.GetComponent<Image>().color = new Color(0.06f, 0.07f, 0.10f, 0.85f);

            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(go.transform, false);
            Full(fillArea.GetComponent<RectTransform>());

            var fillGO = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillGO.transform.SetParent(fillArea.transform, false);
            var fillRt  = fillGO.GetComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero; fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = fillRt.offsetMax = Vector2.zero;
            var fillImg = fillGO.GetComponent<Image>();
            fillImg.color = persuasionMode ? new Color(0.75f, 0.17f, 0.17f, 1f) : ACCENT;

            slider.fillRect      = fillRt;
            slider.direction     = Slider.Direction.LeftToRight;
            slider.minValue      = 0; slider.maxValue = 100;
            slider.wholeNumbers  = true; slider.value = 0;
            slider.interactable  = false;
            slider.transition    = Selectable.Transition.None;

            if (persuasionMode)
            {
                var sc = go.AddComponent<PersuasionSliderColor>();
                Wire(sc, "fill", fillImg);
            }
            return slider;
        }

        private static GameObject CreateScrollView(string name, Transform parent, Vector2 size, Vector2 pos,
                                                   out RectTransform content, out ScrollRect scrollRect)
        {
            var scrollGO = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(ScrollRect), typeof(RectMask2D));
            scrollGO.transform.SetParent(parent, false);
            var rt = scrollGO.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size; rt.anchoredPosition = pos;
            scrollGO.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
            scrollRect = scrollGO.GetComponent<ScrollRect>();
            scrollRect.horizontal = false; scrollRect.vertical = true; scrollRect.scrollSensitivity = 30;

            var viewportGO = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            viewportGO.transform.SetParent(scrollGO.transform, false);
            Full(viewportGO.GetComponent<RectTransform>());

            var contentGO = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentGO.transform.SetParent(viewportGO.transform, false);
            content = contentGO.GetComponent<RectTransform>();
            content.anchorMin = new Vector2(0, 1); content.anchorMax = new Vector2(1, 1); content.pivot = new Vector2(0.5f, 1);
            content.sizeDelta = Vector2.zero; content.anchoredPosition = Vector2.zero;
            var vlg = contentGO.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(16, 16, 8, 8); vlg.spacing = SP_XS;
            vlg.childControlWidth = true; vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
            contentGO.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scrollRect.viewport = viewportGO.GetComponent<RectTransform>();
            scrollRect.content  = content;
            return scrollGO;
        }

        private static TMP_InputField MkInputField(string name, Transform parent, string placeholder)
        {
            var go = TMP_DefaultControls.CreateInputField(new TMP_DefaultControls.Resources());
            go.name = name;
            go.transform.SetParent(parent, false);
            var input = go.GetComponent<TMP_InputField>();

            var inputBg = go.GetComponent<Image>();
            if (inputBg != null) inputBg.color = new Color(0.08f, 0.09f, 0.12f, 0.95f);

            foreach (var t in go.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (_font != null) t.font = _font;
                t.fontSize = TYPE_BODY;
                t.color = TXT_PRIMARY;
                t.enableWordWrapping = true;
            }
            if (input.placeholder is TextMeshProUGUI ph)
            {
                ph.text = placeholder;
                ph.color = TXT_SECONDARY;
            }
            input.lineType = TMP_InputField.LineType.SingleLine;
            return input;
        }

        // ── 프리팹 팩토리 ────────────────────────────────────────────

        private static GameObject CreateStageButtonPrefab(string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(StageButton), typeof(LayoutElement));
            var rootRt = go.GetComponent<RectTransform>();
            rootRt.sizeDelta = new Vector2(900, 560);
            
            var le = go.GetComponent<LayoutElement>();
            le.preferredWidth = 900;
            le.preferredHeight = 560;

            var rootImg = go.GetComponent<Image>();
            rootImg.color = BG_CARD;
            rootImg.raycastTarget = true;
            var btn = go.GetComponent<Button>();
            btn.targetGraphic = rootImg;
            btn.colors = MkColorBlock(BG_CARD);

            var thumbGO = new GameObject("Thumbnail", typeof(RectTransform), typeof(Image));
            thumbGO.transform.SetParent(go.transform, false);
            Full(thumbGO.GetComponent<RectTransform>());
            var thumb = thumbGO.GetComponent<Image>();
            thumb.raycastTarget = false; thumb.preserveAspect = false;

            // Remove red border. Instead, maybe add a subtle outline or just let it be clean.
            // A semi-transparent black overlay for text readability at the bottom.
            var stripGO = new GameObject("Strip", typeof(RectTransform), typeof(Image));
            stripGO.transform.SetParent(go.transform, false);
            var stripRt = stripGO.GetComponent<RectTransform>();
            stripRt.anchorMin = new Vector2(0, 0); stripRt.anchorMax = new Vector2(1, 0); stripRt.pivot = new Vector2(0.5f, 0);
            stripRt.sizeDelta = new Vector2(0, 200); stripRt.anchoredPosition = Vector2.zero;
            // A simple solid dark bar for now, or you can implement a gradient if you have a gradient component.
            // Using a simple dark bar with high transparency
            stripGO.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.85f);
            stripGO.GetComponent<Image>().raycastTarget = false;

            var chapter = MkText("Chapter", go.transform, TYPE_SMALL, TextAlignmentOptions.TopLeft, TXT_SECONDARY);
            var chRt = chapter.rectTransform;
            chRt.anchorMin = new Vector2(0, 0); chRt.anchorMax = new Vector2(1, 0); chRt.pivot = new Vector2(0, 0);
            chRt.sizeDelta = new Vector2(-SP_LG * 2, 40); chRt.anchoredPosition = new Vector2(SP_LG, 120);
            // 유저 요청에 의해 "상황1 취조실..." 등 복잡한 챕터 텍스트는 UI에서 숨김 처리하여 깔끔하게 만듭니다.
            chapter.gameObject.SetActive(false);

            var title = MkText("Title", go.transform, TYPE_TITLE, TextAlignmentOptions.BottomLeft, TXT_PRIMARY);
            title.fontStyle = FontStyles.Bold;
            title.fontSize = 42; // Large cinematic title
            var tRt = title.rectTransform;
            tRt.anchorMin = new Vector2(0, 0); tRt.anchorMax = new Vector2(1, 0); tRt.pivot = new Vector2(0, 0);
            tRt.sizeDelta = new Vector2(-SP_LG * 2, 80); tRt.anchoredPosition = new Vector2(SP_LG, 30);

            var lockGO = new GameObject("LockOverlay", typeof(RectTransform), typeof(Image));
            lockGO.transform.SetParent(go.transform, false);
            Full(lockGO.GetComponent<RectTransform>());
            lockGO.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.90f);
            lockGO.GetComponent<Image>().raycastTarget = true;
            var lockLabel = MkText("LockLabel", lockGO.transform, TYPE_HEADING, TextAlignmentOptions.Center, TXT_SECONDARY);
            Full(lockLabel.rectTransform);
            lockLabel.text = "LOCKED";

            var sb = go.GetComponent<StageButton>();
            Wire(sb, "thumbnail",   thumb);
            Wire(sb, "titleText",   title);
            Wire(sb, "chapterText", chapter);
            Wire(sb, "button",      btn);
            Wire(sb, "lockOverlay", lockGO);

            return go;

            if (!System.IO.Directory.Exists(Application.dataPath + "/Prefabs"))
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            string path = "Assets/Prefabs/" + name + ".prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject CreateBubblePrefab(string name, bool isPlayer)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(CanvasGroup),
                typeof(VerticalLayoutGroup), typeof(ContentSizeFitter), typeof(ChatBubble));

            var img = go.GetComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0f);
            img.raycastTarget = false;

            var vlg = go.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(16, 16, 4, 4);
            vlg.childControlWidth = true; vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;

            var csf = go.GetComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            var textGO = new GameObject("BodyText", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGO.transform.SetParent(go.transform, false);
            var t = textGO.GetComponent<TextMeshProUGUI>();
            if (_font != null) t.font = _font;
            t.fontSize = TYPE_BODY + 2;
            t.color = TXT_PRIMARY;
            t.alignment = TextAlignmentOptions.Center;
            t.enableWordWrapping = true;
            t.overflowMode = TextOverflowModes.Overflow;
            t.raycastTarget = false;

            var textOutline = textGO.AddComponent<Outline>();
            textOutline.effectColor = new Color(0.01f, 0.01f, 0.02f, 0.95f);
            textOutline.effectDistance = new Vector2(1.5f, -1.5f);

            Wire(go.GetComponent<ChatBubble>(), "bodyText", t);

            if (!System.IO.Directory.Exists(Application.dataPath + "/Prefabs"))
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            string path = "Assets/Prefabs/" + name + ".prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        // ── 배선 / 빌드세팅 ─────────────────────────────────────────

        private static void Wire(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            var p  = so.FindProperty(field);
            if (p == null)
            {
                Debug.LogError($"[Persuasion] 필드를 찾지 못함: {target.GetType().Name}.{field}");
                return;
            }
            p.objectReferenceValue = value;
            so.ApplyModifiedProperties();
        }

        private static void AddSceneToBuild(string path)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (scenes.Exists(s => s.path == path)) return;
            scenes.Insert(0, new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
#endif
