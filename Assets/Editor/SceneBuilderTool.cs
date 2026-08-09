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

        // ── Dossier(수사 서류) 리얼리즘 에셋 ──────────────────────
        private const string DossierPaperPath       = "Assets/Art/Textures/dossier_paper_background.png";
        private const string DossierGrainPath       = "Assets/Art/UI/Dossier/paper_grain.png";
        private const string DossierStampBorderPath = "Assets/Art/UI/Dossier/stamp_border.png";

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

            // ── 오프닝 배경 슬라이드쇼 (3장 크로스페이드 + 은은한 줌) ──
            var introBgBGO = new GameObject("IntroBackgroundImageB", typeof(RectTransform), typeof(Image), typeof(AspectRatioFitter));
            introBgBGO.transform.SetParent(introPanel.transform, false);
            introBgBGO.transform.SetSiblingIndex(1);   // A(0) 위, 그라데이션 아래
            Full(introBgBGO.GetComponent<RectTransform>());
            var introBgImgB = introBgBGO.GetComponent<Image>();
            introBgImgB.raycastTarget = false;
            introBgImgB.preserveAspect = false;
            introBgImgB.color = new Color(1f, 1f, 1f, 0f);
            var introFitterB = introBgBGO.GetComponent<AspectRatioFitter>();
            introFitterB.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            introFitterB.aspectRatio = 16f / 9f;

            var openSlides = new System.Collections.Generic.List<Sprite>();
            foreach (var sp in new[] {
                LoadAsSprite("Assets/Art/Transitions/opening_slide1.png"),
                LoadAsSprite("Assets/Art/Transitions/opening_slide2.png"),
                LoadAsSprite("Assets/Art/Transitions/opening_slide3.png") })
                if (sp != null) openSlides.Add(sp);

            if (openSlides.Count > 0)
            {
                introBgImg.sprite  = openSlides[0];
                introBgImg.color   = Color.white;
                introBgImg.enabled = true;
                var slideshow = introPanel.AddComponent<IntroSlideshow>();
                Wire(slideshow, "layerA", introBgImg);
                Wire(slideshow, "layerB", introBgImgB);
                WireArray(slideshow, "slides", openSlides.ToArray());
            }
            else Debug.LogWarning("[Persuasion] opening_slide 이미지를 찾지 못함 — 슬라이드쇼 생략");

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
            // 게임 시작 아래 '설정 | 종료' 나란히 (레퍼런스 배치)
            var introMenuBtn = MkButton("IntroMenuButton", introPanel.transform, "설정", out var introMenuLbl);
            AnchorBox(introMenuBtn.GetComponent<RectTransform>(), new Vector2(0.42f, 0.18f), new Vector2(180f, 56f));
            introMenuBtn.GetComponent<Image>().color = new Color(0.12f, 0.12f, 0.14f, 0.85f);
            if (introMenuBtn.transform.Find("Border") != null) {
                var bdr = introMenuBtn.transform.Find("Border").GetComponent<Image>();
                bdr.color = new Color(1f, 1f, 1f, 0.15f);
                var bdrRt = bdr.GetComponent<RectTransform>();
                bdrRt.offsetMin = new Vector2(-1.5f, -1.5f); bdrRt.offsetMax = new Vector2(1.5f, 1.5f);
            }
            introMenuLbl.fontSize = 22; 
            introMenuLbl.color = new Color(0.9f, 0.9f, 0.9f, 1f);
            introMenuLbl.characterSpacing = 2f;

            var introQuitBtn = MkButton("IntroQuitButton", introPanel.transform, "종료", out var introQuitLbl);
            AnchorBox(introQuitBtn.GetComponent<RectTransform>(), new Vector2(0.58f, 0.18f), new Vector2(180f, 56f));
            introQuitBtn.GetComponent<Image>().color = new Color(0.12f, 0.12f, 0.14f, 0.85f);
            if (introQuitBtn.transform.Find("Border") != null) {
                var bdr = introQuitBtn.transform.Find("Border").GetComponent<Image>();
                bdr.color = new Color(1f, 1f, 1f, 0.15f);
                var bdrRt = bdr.GetComponent<RectTransform>();
                bdrRt.offsetMin = new Vector2(-1.5f, -1.5f); bdrRt.offsetMax = new Vector2(1.5f, 1.5f);
            }
            introQuitLbl.fontSize = 22; 
            introQuitLbl.color = new Color(0.9f, 0.9f, 0.9f, 1f);
            introQuitLbl.characterSpacing = 2f;

            var titleGroupGO = new GameObject("TitleGroup", typeof(RectTransform), typeof(CanvasGroup));
            titleGroupGO.transform.SetParent(introPanel.transform, false);
            AnchorBox(titleGroupGO.GetComponent<RectTransform>(), new Vector2(0.5f, 0.75f), new Vector2(1600, 180));
            var introTitleGroup = titleGroupGO.GetComponent<CanvasGroup>();

            var titleShadow = MkText("TitleShadow", titleGroupGO.transform, 120, TextAlignmentOptions.Center, new Color(0, 0, 0, 0.35f));
            AnchorBox(titleShadow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(1500, 160));
            titleShadow.rectTransform.anchoredPosition = new Vector2(3, -4);
            titleShadow.fontStyle = FontStyles.Bold;
            titleShadow.characterSpacing = 6f;
            titleShadow.text = gameData.gameTitle;

            var titleText = MkText("Title", titleGroupGO.transform, 150, TextAlignmentOptions.Center, Color.white);
            AnchorBox(titleText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(1500, 160));
            titleText.fontStyle = FontStyles.Bold;
            titleText.characterSpacing = 2f;
            titleText.text = gameData.gameTitle;

            // 타이틀 아래 붉은색 포인트 라인 (레퍼런스 느낌)
            var titleLine = MkPanel("TitleLine", titleGroupGO.transform, new Color(0.88f, 0.22f, 0.15f, 0.95f), false);
            AnchorBox(titleLine.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(500, 3));
            titleLine.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -68);

            var subtitle = MkText("Subtitle", titleGroupGO.transform, 38, TextAlignmentOptions.Center, Color.white);
            AnchorBox(subtitle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(1200, 50));
            subtitle.rectTransform.anchoredPosition = new Vector2(0, -115);
            subtitle.fontStyle = FontStyles.Bold;
            subtitle.characterSpacing = 4f;
            subtitle.text = "진실을 밝혀내는 심리전";

            var startBtn = MkButton("StartButton", introPanel.transform, "게임 시작", out var startBtnText);
            AnchorBox(startBtn.GetComponent<RectTransform>(), new Vector2(0.5f, 0.32f), new Vector2(430, 92));
            startBtn.GetComponent<Image>().color = new Color(0.88f, 0.22f, 0.15f, 0.95f); // 세련된 레드오렌지
            var startBorder = startBtn.transform.Find("Border");
            if (startBorder != null)
            {
                var sbImg = startBorder.GetComponent<Image>();
                sbImg.color = new Color(1f, 1f, 1f, 0.15f);   // 미묘한 화이트 하이라이트 테두리
                var sbRt = startBorder.GetComponent<RectTransform>();
                sbRt.offsetMin = new Vector2(-1.5f, -1.5f); sbRt.offsetMax = new Vector2(1.5f, 1.5f);
            }
            startBtnText.fontSize = 42;
            startBtnText.fontStyle = FontStyles.Bold;
            startBtnText.characterSpacing = 6f;
            startBtnText.color = Color.white; // 가독성을 위한 화이트 텍스트



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

            // ── 컨트롤 버튼 (미니멀하고 세련된 플로팅 캡슐 디자인) ──
            var ctrlRow = new GameObject("PlayControls", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
            ctrlRow.transform.SetParent(playPanel.transform, false);
            var ctrlRt = ctrlRow.GetComponent<RectTransform>();
            ctrlRt.anchorMin = new Vector2(1, 1); ctrlRt.anchorMax = new Vector2(1, 1); ctrlRt.pivot = new Vector2(1, 1);
            ctrlRt.anchoredPosition = new Vector2(-SAFE - 24f, -90f); // 여백 확보
            var ctrlHlg = ctrlRow.GetComponent<HorizontalLayoutGroup>();
            ctrlHlg.spacing = 16f; ctrlHlg.childAlignment = TextAnchor.MiddleRight;
            ctrlHlg.childControlWidth = true; ctrlHlg.childControlHeight = true;
            ctrlHlg.childForceExpandWidth = false; ctrlHlg.childForceExpandHeight = false;
            ctrlRow.GetComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            ctrlRow.GetComponent<ContentSizeFitter>().verticalFit   = ContentSizeFitter.FitMode.PreferredSize;

            // 미니멀 프리미엄 컬러: 다크 그레이 반투명, 포인트는 딥 레드
            Color sleekBtn = new Color(0.05f, 0.05f, 0.06f, 0.6f);
            Color sleekAccent = new Color(0.7f, 0.15f, 0.15f, 0.8f);

            // 아이콘 없이 깔끔한 텍스트 캡슐로만 구성
            var storyPeekBtn = MkBarButton("StoryPeekButton", ctrlRow.transform, "스토리",       100f, sleekBtn);
            var traitBtn     = MkBarButton("TraitButton",     ctrlRow.transform, "수사 파일",    120f, sleekAccent);
            var historyBtn   = MkBarButton("HistoryButton",   ctrlRow.transform, "대화 기록",    120f, sleekBtn);
            var menuBtn      = MkBarButton("MenuButton",      ctrlRow.transform, "설 정",        80f, sleekBtn);

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

            var hintPanel = MkPanel("HintPanel", playPanel.transform, new Color(0.04f, 0.04f, 0.06f, 0.7f), false);
            var hintRt = hintPanel.GetComponent<RectTransform>();
            hintRt.anchorMin = new Vector2(0.5f, 0); hintRt.anchorMax = new Vector2(0.5f, 0); hintRt.pivot = new Vector2(0.5f, 0);
            hintRt.sizeDelta = new Vector2(600f, 44f); hintRt.anchoredPosition = new Vector2(0, 320f);
            
            var hintImg = hintPanel.GetComponent<Image>();
            Round(hintImg, 22f); // 캡슐 모양 둥근 모서리

            var hintOutln = hintPanel.AddComponent<Outline>();
            hintOutln.effectColor = new Color(0.85f, 0.62f, 0.18f, 0.5f); // 은은한 골드빛 아웃라인
            hintOutln.effectDistance = new Vector2(1f, -1f);

            var hintText = MkText("HintText", hintPanel.transform, 16, TextAlignmentOptions.Center, TXT_GOLD);
            Full(hintText.rectTransform);
            hintText.fontStyle = FontStyles.Bold;
            hintText.margin = new Vector4(0, 2f, 0, 0); // 텍스트 수직 중앙 정렬 미세조정

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
            // EVIDENCE BAR (하단 증거 획득 인벤토리)
            // ════════════════════════════════════════════════
            var evidenceContainer = MkPanel("EvidenceContainer", playPanel.transform, new Color(0f, 0f, 0f, 0f), false);
            var evContRt = evidenceContainer.GetComponent<RectTransform>();
            evContRt.anchorMin = new Vector2(0, 0); evContRt.anchorMax = new Vector2(1, 0); evContRt.pivot = new Vector2(0.5f, 0);
            evContRt.sizeDelta = new Vector2(-120f, 80f); evContRt.anchoredPosition = new Vector2(0, 100f);
            
            var evLayout = evidenceContainer.AddComponent<HorizontalLayoutGroup>();
            evLayout.padding = new RectOffset(16, 16, 8, 8);
            evLayout.spacing = 16f;
            evLayout.childAlignment = TextAnchor.MiddleLeft;
            evLayout.childControlWidth = false;
            evLayout.childControlHeight = true;
            evLayout.childForceExpandWidth = false;
            evLayout.childForceExpandHeight = false;

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
            Round(introGoalCard.GetComponent<Image>());
            AddShadow(introGoalCard, 22f, 0.45f, -6f);

            var introGoalBorder = MkPanel("Border", introGoalCard.transform, ACCENT, false);
            var igbRt = introGoalBorder.GetComponent<RectTransform>();
            Full(igbRt);
            igbRt.offsetMin = new Vector2(-2, -2); igbRt.offsetMax = new Vector2(2, 2);
            igbRt.SetAsFirstSibling();
            Round(introGoalBorder.GetComponent<Image>());

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
            resultBestMoveText.textWrappingMode = TMPro.TextWrappingModes.Normal;
            resultBestMoveText.fontStyle = FontStyles.Italic;

            // 실패 내레이션 (클리어 때는 숨김)
            var resultNarration = MkText("ResultNarration", resultPanel.transform, TYPE_BODY, TextAlignmentOptions.Center, new Color(TXT_PRIMARY.r, TXT_PRIMARY.g, TXT_PRIMARY.b, 0.88f));
            AnchorBox(resultNarration.rectTransform, new Vector2(0.5f, 0.57f), new Vector2(1200, 260));
            resultNarration.textWrappingMode = TMPro.TextWrappingModes.Normal;
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
            storyFitter.aspectMode = AspectRatioFitter.AspectMode.None; // 런타임 DisplayStoryPage에서 EnvelopeParent로 설정
            storyFitter.aspectRatio = 16f / 9f;

            // 2. 하단 그라데이션 오버레이 (텍스트 가독성) — 더 깊게
            var storyBotOverlay = new GameObject("StoryBottomOverlay", typeof(RectTransform));
            storyBotOverlay.transform.SetParent(storyBeatPanel.transform, false);
            var sboRt = storyBotOverlay.GetComponent<RectTransform>();
            sboRt.anchorMin = Vector2.zero; sboRt.anchorMax = new Vector2(1f, 0.65f);
            sboRt.offsetMin = Vector2.zero; sboRt.offsetMax = Vector2.zero;
            var sboGrad = storyBotOverlay.AddComponent<UIVerticalGradient>();
            sboGrad.topColor    = new Color(0f, 0f, 0f, 0f);
            sboGrad.bottomColor = new Color(0.01f, 0.01f, 0.03f, 0.97f);
            sboGrad.raycastTarget = false;

            // 2b. 상단 그라데이션 (레터박스 아래로 자연스럽게)
            var storyTopOverlay = new GameObject("StoryTopOverlay", typeof(RectTransform));
            storyTopOverlay.transform.SetParent(storyBeatPanel.transform, false);
            var stoRt = storyTopOverlay.GetComponent<RectTransform>();
            stoRt.anchorMin = new Vector2(0f, 0.72f); stoRt.anchorMax = Vector2.one;
            stoRt.offsetMin = Vector2.zero; stoRt.offsetMax = Vector2.zero;
            var stoGrad = storyTopOverlay.AddComponent<UIVerticalGradient>();
            stoGrad.topColor    = new Color(0.01f, 0.01f, 0.03f, 0.88f);
            stoGrad.bottomColor = new Color(0f, 0f, 0f, 0f);
            stoGrad.raycastTarget = false;

            // 3. 텍스트 영역 — 하단 전체 가로, 버튼 공간 확보
            var storyTextBackGO = MkPanel("TextBacking", storyBeatPanel.transform, new Color(0f, 0f, 0f, 0f), false);
            var stbRt = storyTextBackGO.GetComponent<RectTransform>();
            stbRt.anchorMin = new Vector2(0f, 0f); stbRt.anchorMax = new Vector2(1f, 0f);
            stbRt.pivot = new Vector2(0.5f, 0f);
            stbRt.sizeDelta = new Vector2(0f, 340f);
            stbRt.anchoredPosition = new Vector2(0f, 68f);

            // 황금 구분선 (텍스트 영역 상단)
            var storyDivider = MkPanel("StoryDivider", storyTextBackGO.transform, TXT_GOLD, false);
            {
                var rt = storyDivider.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.08f, 1f); rt.anchorMax = new Vector2(0.92f, 1f);
                rt.pivot = new Vector2(0.5f, 1f); rt.sizeDelta = new Vector2(0f, 1f); rt.anchoredPosition = Vector2.zero;
                storyDivider.GetComponent<Image>().color = new Color(TXT_GOLD.r, TXT_GOLD.g, TXT_GOLD.b, 0.55f);
            }

            // 헤더 뱃지 — 동적 (씬 / 캐릭터 / 전환 구분)
            var storyHeader = MkText("StoryHeader", storyTextBackGO.transform, 14, TextAlignmentOptions.Left, TXT_GOLD);
            AnchorTopLeft(storyHeader.rectTransform, new Vector2(80f, -10f), new Vector2(700f, 26f));
            storyHeader.fontStyle = FontStyles.Bold;
            storyHeader.characterSpacing = 3f;
            storyHeader.text = "◆  사건 현장";

            // 페이지 인디케이터 (우측)
            var storyPageIndicator = MkText("StoryPageIndicator", storyTextBackGO.transform, 13, TextAlignmentOptions.Right, TXT_SECONDARY);
            AnchorTopRight(storyPageIndicator.rectTransform, new Vector2(-80f, -11f), new Vector2(200f, 22f));
            storyPageIndicator.characterSpacing = 2f;
            storyPageIndicator.text = "1 / 1";

            // 스토리 본문 텍스트 — 한 문장씩, 크게
            var storyBeatText = MkText("StoryBeatText", storyTextBackGO.transform, 30, TextAlignmentOptions.Center, new Color(0.99f, 0.99f, 0.98f, 1.0f));
            var sbtRt = storyBeatText.rectTransform;
            sbtRt.anchorMin = Vector2.zero; sbtRt.anchorMax = Vector2.one;
            sbtRt.offsetMin = new Vector2(120f, 10f); sbtRt.offsetMax = new Vector2(-120f, -42f);
            storyBeatText.fontStyle = FontStyles.Bold;
            storyBeatText.lineSpacing = 18f;
            storyBeatText.textWrappingMode = TMPro.TextWrappingModes.Normal;
            storyBeatText.overflowMode = TextOverflowModes.Overflow;

            var sbtOutline = storyBeatText.gameObject.AddComponent<Outline>();
            sbtOutline.effectColor = new Color(0f, 0f, 0f, 0.98f);
            sbtOutline.effectDistance = new Vector2(3f, -3f);

            // 4. 상단 레터박스 바 (시네마틱)
            var storyTopBar = MkPanel("StoryTopBar", storyBeatPanel.transform, new Color(0f, 0f, 0f, 0.85f), false);
            {
                var rt = storyTopBar.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = Vector2.one;
                rt.pivot = new Vector2(0.5f, 1f); rt.sizeDelta = new Vector2(0f, 72f); rt.anchoredPosition = Vector2.zero;
            }
            // 뱃지 레이블 (좌)
            var storyBadgeLabel = MkText("StoryBadgeLabel", storyTopBar.transform, 13, TextAlignmentOptions.Left, TXT_SECONDARY);
            AnchorTopLeft(storyBadgeLabel.rectTransform, new Vector2(48f, -24f), new Vector2(600f, 24f));
            storyBadgeLabel.characterSpacing = 2f;
            storyBadgeLabel.text = "☆  형사과 · DETECTIVE DIVISION";

            // 케이스 번호 (우, 동적)
            var storyCaseLabel = MkText("StoreCaseLabel", storyTopBar.transform, 13, TextAlignmentOptions.Right, TXT_GOLD);
            AnchorTopRight(storyCaseLabel.rectTransform, new Vector2(-48f, -24f), new Vector2(240f, 24f));
            storyCaseLabel.fontStyle = FontStyles.Bold;
            storyCaseLabel.characterSpacing = 2f;
            storyCaseLabel.text = "CASE #001";

            // 다음 컷 버튼 — 화면 하단 고정
            var storyBeatContinueBtn = MkButton("StoryBeatContinueButton", storyBeatPanel.transform, "다음 ▶", out var sbBtnLbl);
            var sbBtnRt = storyBeatContinueBtn.GetComponent<RectTransform>();
            sbBtnRt.anchorMin = new Vector2(0.5f, 0f); sbBtnRt.anchorMax = new Vector2(0.5f, 0f); sbBtnRt.pivot = new Vector2(0.5f, 0f);
            sbBtnRt.sizeDelta = new Vector2(200f, 50f); sbBtnRt.anchoredPosition = new Vector2(0f, 10f);
            sbBtnLbl.fontSize = 17;
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
            AnchorBox(menuCard.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(680, 660));
            var menuCardImg = menuCard.GetComponent<Image>();
            menuCardImg.color = BG_CARD; Round(menuCardImg); AddShadow(menuCard, 28f, 0.5f, -8f);

            var menuTitle = MkText("MenuTitle", menuCard.transform, TYPE_HEADING, TextAlignmentOptions.Center, TXT_PRIMARY);
            menuTitle.fontStyle = FontStyles.Bold; menuTitle.text = "설정";
            AnchorBox(menuTitle.rectTransform, new Vector2(0.5f, 0.925f), new Vector2(560, 60));

            // 볼륨 3채널 (음악 / 효과음 / 음성) — 텍스트 라벨 + 원형핸들 슬라이더
            var musicSlider = MkVolumeRow(menuCard.transform, "배경음",   0.80f);
            var sfxSlider   = MkVolumeRow(menuCard.transform, "효과음", 0.715f);
            var voiceSlider = MkVolumeRow(menuCard.transform, "음성",     0.63f);

            var guideOpenBtn = MkButton("GuideOpenButton", menuCard.transform, "게임 설명서 보기", out _);
            AnchorBox(guideOpenBtn.GetComponent<RectTransform>(), new Vector2(0.5f, 0.485f), new Vector2(500, 66));
            BtnColor(guideOpenBtn, ACCENT);

            var homeBtn = MkButton("HomeButton", menuCard.transform, "홈화면으로 나가기", out _);
            AnchorBox(homeBtn.GetComponent<RectTransform>(), new Vector2(0.5f, 0.355f), new Vector2(500, 66));
            BtnColor(homeBtn, new Color(0.3f, 0.5f, 0.8f, 1f)); // Blue color

            var quitBtn = MkButton("QuitButton", menuCard.transform, "게임 나가기", out _);
            AnchorBox(quitBtn.GetComponent<RectTransform>(), new Vector2(0.5f, 0.225f), new Vector2(500, 66));
            BtnColor(quitBtn, COL_DANGER);

            var menuCloseBtn = MkButton("MenuCloseButton", menuCard.transform, "닫기", out _);
            AnchorBox(menuCloseBtn.GetComponent<RectTransform>(), new Vector2(0.5f, 0.075f), new Vector2(500, 60));
            BtnColor(menuCloseBtn, COL_NEUTRAL);

            // ════════════════════════════════════════════════
            // GAME GUIDE PANEL (수사 지침서 팝업) - Dossier Reference Style
            // ════════════════════════════════════════════════
            var guidePanel = MkPanel("GuidePanel", canvasT, new Color(0.02f, 0.02f, 0.04f, 0.92f), true);

            // 바닥 드롭섀도우 — 서류가 어두운 배경 위에 살짝 떠 보이도록
            var guideShadow = MkPanel("GuideShadow", guidePanel.transform, new Color(0f, 0f, 0f, 0.40f), false);
            AnchorBox(guideShadow.GetComponent<RectTransform>(), new Vector2(0.5f, 0.50f), new Vector2(1560, 840));
            guideShadow.GetComponent<RectTransform>().anchoredPosition += new Vector2(12f, -12f);
            guideShadow.transform.localEulerAngles = new Vector3(0, 0, -0.9f);

            // 바탕이 되는 서류철 (Dossier Folder Base) — 실제 종이 텍스처 + 미세 기울임
            var guideBox = MkPanel("GuideBox", guidePanel.transform, new Color(0.76f, 0.73f, 0.65f, 1.0f), false);
            var guideRt = guideBox.GetComponent<RectTransform>();
            AnchorBox(guideRt, new Vector2(0.5f, 0.50f), new Vector2(1560, 840));
            guideBox.transform.localEulerAngles = new Vector3(0, 0, -0.9f);
            {
                var paperSprite = LoadUiSprite(DossierPaperPath);
                if (paperSprite != null)
                {
                    var gbImg = guideBox.GetComponent<Image>();
                    gbImg.sprite = paperSprite;
                    gbImg.type   = Image.Type.Simple;
                    gbImg.color  = new Color(0.88f, 0.85f, 0.78f, 1.0f);
                }
            }

            var boxBorder = MkPanel("FolderBorder", guideBox.transform, new Color(0.48f, 0.44f, 0.38f, 0.80f), false);
            Full(boxBorder.GetComponent<RectTransform>());
            boxBorder.GetComponent<RectTransform>().offsetMin = new Vector2(-2, -2);
            boxBorder.GetComponent<RectTransform>().offsetMax = new Vector2( 2,  2);
            boxBorder.transform.SetAsFirstSibling();

            // 왼쪽 쇠집게 클립 느낌의 오브젝트 (간이 구현)
            var metalClip = MkPanel("MetalClip", guideBox.transform, new Color(0.8f, 0.8f, 0.8f, 1f), false);
            AnchorBox(metalClip.GetComponent<RectTransform>(), new Vector2(0f, 0.7f), new Vector2(80, 160));
            metalClip.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 0);
            var clipBorder = MkPanel("ClipBorder", metalClip.transform, new Color(0.4f, 0.4f, 0.4f, 1f), false);
            Full(clipBorder.GetComponent<RectTransform>()); clipBorder.GetComponent<RectTransform>().offsetMin = new Vector2(-2, -2); clipBorder.GetComponent<RectTransform>().offsetMax = new Vector2(2, 2); clipBorder.transform.SetAsFirstSibling();

            // 헤더 영역
            var guideHeader = MkText("DirectiveHeader", guideBox.transform, 31, TextAlignmentOptions.Left, new Color(0.12f, 0.12f, 0.12f, 1.0f));
            AnchorTopLeft(guideHeader.rectTransform, new Vector2(40, -32), new Vector2(1200, 44));
            guideHeader.fontStyle = FontStyles.Bold;
            guideHeader.characterSpacing = 1.5f;
            guideHeader.text = "■ 수사관 전술 운용 지침 (PROTOCOL // STRICTLY CONFIDENTIAL)";

            // 🔴 우측 상단 붉은색 스탬프 — 낡은 고무도장처럼 비뚤게 찍힌 느낌
            var stampBadge = MkPanel("ConfidentialStamp", guideBox.transform, new Color(0,0,0,0), false);
            AnchorTopRight(stampBadge.GetComponent<RectTransform>(), new Vector2(-24, -6), new Vector2(280, 108));
            stampBadge.transform.localEulerAngles = new Vector3(0, 0, -12f);
            {
                var stampBorderSprite = LoadUiSprite(DossierStampBorderPath);
                if (stampBorderSprite != null)
                {
                    var stampArt = MkPanel("StampBorderArt", stampBadge.transform, new Color(1f, 1f, 1f, 0.85f), false);
                    Full(stampArt.GetComponent<RectTransform>());
                    stampArt.GetComponent<Image>().sprite = stampBorderSprite;
                }
            }
            var stampText = MkText("StampText", stampBadge.transform, 17, TextAlignmentOptions.Center, new Color(0.62f, 0.09f, 0.08f, 0.90f));
            Full(stampText.rectTransform);
            stampText.fontStyle = FontStyles.Bold;
            stampText.characterSpacing = 2f;
            stampText.text = "1급 비밀 기밀\nCLASSIFIED";

            var divGO = MkPanel("Divider", guideBox.transform, new Color(0.55f, 0.50f, 0.42f, 0.60f), false);
            AnchorTopLeft(divGO.GetComponent<RectTransform>(), new Vector2(40, -82), new Vector2(1480, 4));

            // 내부에 얹힌 서류 종이 바탕 (Aged Paper Directive Sheet)
            var contentCard = MkPanel("ContentCard", guideBox.transform, new Color(0.92f, 0.90f, 0.86f, 1.0f), false);
            AnchorTopLeft(contentCard.GetComponent<RectTransform>(), new Vector2(40, -96), new Vector2(1480, 680));
            var cBorder = MkPanel("CardBorder", contentCard.transform, new Color(0.75f, 0.72f, 0.65f, 1.0f), false);
            Full(cBorder.GetComponent<RectTransform>());
            cBorder.GetComponent<RectTransform>().offsetMin = new Vector2(-2, -2); cBorder.GetComponent<RectTransform>().offsetMax = new Vector2(2, 2); cBorder.transform.SetAsFirstSibling();

            // 3단 세부 지침 카피
            Color INK_TEXT    = new Color(0.12f, 0.12f, 0.10f, 1.0f);
            Color INK_TITLE   = new Color(0.18f, 0.20f, 0.24f, 1.0f); // 진한 잉크
            Color INK_WARN    = new Color(0.72f, 0.11f, 0.10f, 1.0f); // 경고용 빨강
            Color BORDER_CARD = new Color(0.82f, 0.80f, 0.74f, 1.0f); // 내부 테두리
            Color INNER_BG    = new Color(0.96f, 0.94f, 0.88f, 1.0f);

            // Col 1: Mission
            var col1 = MkPanel("Col1_Mission", contentCard.transform, INNER_BG, false);
            AnchorTopLeft(col1.GetComponent<RectTransform>(), new Vector2(20, -20), new Vector2(466, 640));
            var c1B = MkPanel("Border", col1.transform, BORDER_CARD, false);
            Full(c1B.GetComponent<RectTransform>()); c1B.GetComponent<RectTransform>().offsetMin = new Vector2(-2, -2); c1B.GetComponent<RectTransform>().offsetMax = new Vector2(2, 2); c1B.transform.SetAsFirstSibling();

            var c1Title = MkText("Title", col1.transform, 27, TextAlignmentOptions.TopLeft, INK_TITLE);
            AnchorTopLeft(c1Title.rectTransform, new Vector2(24, -24), new Vector2(420, 70));
            c1Title.fontStyle = FontStyles.Bold;
            c1Title.text = "[PROTOCOL // A]\n사건 목표 (OBJECTIVE)";

            var c1Div = MkPanel("Div", col1.transform, BORDER_CARD, false);
            AnchorTopLeft(c1Div.GetComponent<RectTransform>(), new Vector2(24, -94), new Vector2(418, 2));

            var c1Body = MkText("Body", col1.transform, 20, TextAlignmentOptions.TopLeft, INK_TEXT);
            AnchorTopLeft(c1Body.rectTransform, new Vector2(24, -114), new Vector2(418, 510));
            c1Body.lineSpacing = 10f;
            c1Body.textWrappingMode = TMPro.TextWrappingModes.Normal;
            c1Body.fontStyle = FontStyles.Normal; // 제목(Bold)과 대비되도록 본문은 레귤러로 — 위계 강화
            c1Body.text =
                "• 자백 유도 시스템 (System Overwhelm)\n" +
                "피의자의 핵심 저항 지점을 분석하고 무력화하여, 결정적인 진술을 확보하십시오.\n\n" +
                "• 설득 시뮬레이션 달성 (100% Persuasion)\n" +
                "제한된 시간(턴) 내에 '심리적 무장 해제' 게이지를 100%까지 올려 시뮬레이션을 성공적으로 완료하십시오.";

            // Col 2: Tactics
            var col2 = MkPanel("Col2_Tactics", contentCard.transform, INNER_BG, false);
            AnchorTopLeft(col2.GetComponent<RectTransform>(), new Vector2(506, -20), new Vector2(466, 640));
            var c2B = MkPanel("Border", col2.transform, BORDER_CARD, false);
            Full(c2B.GetComponent<RectTransform>()); c2B.GetComponent<RectTransform>().offsetMin = new Vector2(-2, -2); c2B.GetComponent<RectTransform>().offsetMax = new Vector2(2, 2); c2B.transform.SetAsFirstSibling();

            var c2Title = MkText("Title", col2.transform, 27, TextAlignmentOptions.TopLeft, INK_TITLE);
            AnchorTopLeft(c2Title.rectTransform, new Vector2(24, -24), new Vector2(420, 70));
            c2Title.fontStyle = FontStyles.Bold;
            c2Title.text = "[TACTICS // B]\n심문 전술 (TACTICS)";

            var c2Div = MkPanel("Div", col2.transform, BORDER_CARD, false);
            AnchorTopLeft(c2Div.GetComponent<RectTransform>(), new Vector2(24, -94), new Vector2(418, 2));

            var c2Body = MkText("Body", col2.transform, 20, TextAlignmentOptions.TopLeft, INK_TEXT);
            AnchorTopLeft(c2Body.rectTransform, new Vector2(24, -114), new Vector2(418, 510));
            c2Body.lineSpacing = 10f;
            c2Body.textWrappingMode = TMPro.TextWrappingModes.Normal;
            c2Body.fontStyle = FontStyles.Normal;
            c2Body.text =
                "• 맞춤형 접근 (Adaptive Profiling)\n" +
                "감성, 자존심, 논리, 신뢰 등 네 가지 '정서적 결함' 중 대상에게 가장 적합한 약점을 공략하십시오.\n\n" +
                "• 어조 및 동작 서술 (Mannerisms)\n" +
                "질문 입력창에 대괄호 [ ]를 사용하여 행동을 서술하십시오. 예: [차분하게] 또는 [단호하게]\n\n" +
                "• 전술 힌트 해금 (Tactical Unlocking)\n" +
                "설득 시뮬레이션 50% 달성 시, 실시간 심층 분석 데이터가 해금됩니다.";

            // Col 3: Warnings
            var col3 = MkPanel("Col3_Warnings", contentCard.transform, INNER_BG, false);
            AnchorTopLeft(col3.GetComponent<RectTransform>(), new Vector2(992, -20), new Vector2(466, 640));
            var c3B = MkPanel("Border", col3.transform, BORDER_CARD, false);
            Full(c3B.GetComponent<RectTransform>()); c3B.GetComponent<RectTransform>().offsetMin = new Vector2(-2, -2); c3B.GetComponent<RectTransform>().offsetMax = new Vector2(2, 2); c3B.transform.SetAsFirstSibling();

            var c3Title = MkText("Title", col3.transform, 27, TextAlignmentOptions.TopLeft, INK_WARN);
            AnchorTopLeft(c3Title.rectTransform, new Vector2(24, -24), new Vector2(420, 70));
            c3Title.fontStyle = FontStyles.Bold;
            c3Title.text = "[WARNINGS // C]\n경고 사항 (WARNINGS)";

            var c3Div = MkPanel("Div", col3.transform, BORDER_CARD, false);
            AnchorTopLeft(c3Div.GetComponent<RectTransform>(), new Vector2(24, -94), new Vector2(418, 2));

            var c3Body = MkText("Body", col3.transform, 20, TextAlignmentOptions.TopLeft, INK_TEXT);
            AnchorTopLeft(c3Body.rectTransform, new Vector2(24, -114), new Vector2(418, 510));
            c3Body.lineSpacing = 10f;
            c3Body.textWrappingMode = TMPro.TextWrappingModes.Normal;
            c3Body.fontStyle = FontStyles.Normal;
            c3Body.text =
                "• 저항 및 대화 단절 (Resistance)\n" +
                "협박, 인격 모독 등 부적절한 언행은 피의자의 심리적 장벽을 높이고 대화를 단절시킵니다.\n\n" +
                "• 심문 실패 조건 (Protocol Fail)\n" +
                "3회 연속 설득 게이지 0% 유지 또는 시간 초과 시 심문이 실패한 것으로 간주됩니다.";

            // 종이 그레인/얼룩 오버레이 — GuideBox 전체 위에 얹혀 인쇄물 질감을 더함
            {
                var grainSprite = LoadUiSprite(DossierGrainPath);
                if (grainSprite != null)
                {
                    var guideGrain = MkPanel("PaperGrainOverlay", guideBox.transform, new Color(1f, 1f, 1f, 0.55f), false);
                    Full(guideGrain.GetComponent<RectTransform>());
                    guideGrain.GetComponent<Image>().sprite = grainSprite;
                }
            }

            var guideCloseBtn = MkButton("GuideCloseButton", guideBox.transform, "[ 전술 운용 지침 확인 완료 ]\n(닫기)", out var guideCloseLbl);
            AnchorBox(guideCloseBtn.GetComponent<RectTransform>(), new Vector2(0.5f, -0.06f), new Vector2(400, 72));
            guideCloseLbl.fontSize = 20;
            guideCloseLbl.fontStyle = FontStyles.Bold;
            BtnColor(guideCloseBtn, new Color(0.7f, 0.72f, 0.74f, 1.0f)); // Metallic button
            if (guideCloseBtn.transform.Find("Border") != null) guideCloseBtn.transform.Find("Border").GetComponent<Image>().color = new Color(0.4f, 0.4f, 0.4f, 1f);


            // ════════════════════════════════════════════════
            // TRAIT PANEL — 경찰청 수사 서류 파일 v2 (세련된 Dossier)
            // ════════════════════════════════════════════════
            var traitPanel = MkPanel("TraitPanel", canvasT, new Color(0.05f, 0.03f, 0.02f, 0.97f), true);

            // 챕터별 헤더 텍스트 참조 (런타임에 PersuasionUI가 교체)
            TMP_Text traitAgencyRef = null, traitSubRef = null, traitFooterRef = null;

            // 바닥 드롭섀도우
            var traitShadow = MkPanel("TraitShadow", traitPanel.transform, new Color(0f, 0f, 0f, 0.40f), false);
            AnchorBox(traitShadow.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(1000, 950));
            traitShadow.GetComponent<RectTransform>().anchoredPosition += new Vector2(12f, -12f);
            traitShadow.transform.localEulerAngles = new Vector3(0, 0, 0.8f);

            // 바탕 서류철 — 실제 종이 텍스처 + 미세 기울임
            var traitBox = MkPanel("TraitBox", traitPanel.transform, new Color(0.76f, 0.73f, 0.65f, 1.0f), false);
            var tbRt = traitBox.GetComponent<RectTransform>();
            AnchorBox(tbRt, new Vector2(0.5f, 0.5f), new Vector2(1000, 950));
            traitBox.transform.localEulerAngles = new Vector3(0, 0, 0.8f);
            {
                var paperSprite = LoadUiSprite(DossierPaperPath);
                if (paperSprite != null)
                {
                    var tbImg = traitBox.GetComponent<Image>();
                    tbImg.sprite = paperSprite;
                    tbImg.type   = Image.Type.Simple;
                    tbImg.color  = new Color(0.88f, 0.85f, 0.78f, 1.0f);
                }
            }

            var t_boxBorder = MkPanel("FolderBorder", traitBox.transform, new Color(0.48f, 0.44f, 0.38f, 0.80f), false);
            Full(t_boxBorder.GetComponent<RectTransform>()); t_boxBorder.GetComponent<RectTransform>().offsetMin = new Vector2(-2, -2); t_boxBorder.GetComponent<RectTransform>().offsetMax = new Vector2(2, 2); t_boxBorder.transform.SetAsFirstSibling();

            // 왼쪽 쇠집게 클립
            var t_metalClip = MkPanel("MetalClip", traitBox.transform, new Color(0.8f, 0.8f, 0.8f, 1f), false);
            AnchorBox(t_metalClip.GetComponent<RectTransform>(), new Vector2(0f, 0.75f), new Vector2(80, 160));
            var t_clipBorder = MkPanel("ClipBorder", t_metalClip.transform, new Color(0.4f, 0.4f, 0.4f, 1f), false);
            Full(t_clipBorder.GetComponent<RectTransform>()); t_clipBorder.GetComponent<RectTransform>().offsetMin = new Vector2(-2, -2); t_clipBorder.GetComponent<RectTransform>().offsetMax = new Vector2(2, 2); t_clipBorder.transform.SetAsFirstSibling();

            // 헤더
            var traitTitleText = MkText("TitleText", traitBox.transform, 31, TextAlignmentOptions.Left, new Color(0.12f, 0.12f, 0.12f, 1.0f));
            AnchorTopLeft(traitTitleText.rectTransform, new Vector2(40, -32), new Vector2(600, 44));
            traitTitleText.fontStyle = FontStyles.Bold; traitTitleText.characterSpacing = 1.5f;
            traitTitleText.text = "■ 피의자 수사 분석 보고서 (DOSSIER // STRICTLY CONFIDENTIAL)";

            // 우상단 스탬프 — 낡은 고무도장처럼 비뚤게 찍힌 느낌
            var t_stampBadge = MkPanel("ConfidentialStamp", traitBox.transform, new Color(0,0,0,0), false);
            AnchorTopRight(t_stampBadge.GetComponent<RectTransform>(), new Vector2(-24, -6), new Vector2(260, 100));
            t_stampBadge.transform.localEulerAngles = new Vector3(0, 0, 9f);
            {
                var stampBorderSprite = LoadUiSprite(DossierStampBorderPath);
                if (stampBorderSprite != null)
                {
                    var t_stampArt = MkPanel("StampBorderArt", t_stampBadge.transform, new Color(1f, 1f, 1f, 0.85f), false);
                    Full(t_stampArt.GetComponent<RectTransform>());
                    t_stampArt.GetComponent<Image>().sprite = stampBorderSprite;
                }
            }
            var t_stampText = MkText("StampText", t_stampBadge.transform, 16, TextAlignmentOptions.Center, new Color(0.62f, 0.09f, 0.08f, 0.90f));
            Full(t_stampText.rectTransform); t_stampText.fontStyle = FontStyles.Bold; t_stampText.characterSpacing = 1.5f;
            t_stampText.text = "1급 비밀 기밀\nCLASSIFIED";

            var t_divGO = MkPanel("Divider", traitBox.transform, new Color(0.55f, 0.50f, 0.42f, 0.60f), false);
            AnchorTopLeft(t_divGO.GetComponent<RectTransform>(), new Vector2(40, -82), new Vector2(920, 4));

            // 내부에 얹힌 서류 종이 바탕
            var t_contentCard = MkPanel("ContentCard", traitBox.transform, new Color(0.92f, 0.90f, 0.86f, 1.0f), false);
            AnchorTopLeft(t_contentCard.GetComponent<RectTransform>(), new Vector2(40, -96), new Vector2(920, 810));
            var t_cBorder = MkPanel("CardBorder", t_contentCard.transform, new Color(0.75f, 0.72f, 0.65f, 1.0f), false);
            Full(t_cBorder.GetComponent<RectTransform>()); t_cBorder.GetComponent<RectTransform>().offsetMin = new Vector2(-2, -2); t_cBorder.GetComponent<RectTransform>().offsetMax = new Vector2(2, 2); t_cBorder.transform.SetAsFirstSibling();

            // 내부 요소 (종이 위)
            var agTxt = MkText("AgencyName", t_contentCard.transform, 13, TextAlignmentOptions.Center, new Color(0.18f, 0.20f, 0.24f, 1.0f));
            AnchorTopLeft(agTxt.rectTransform, new Vector2(20, -16), new Vector2(880, 24));
            agTxt.fontStyle = FontStyles.Bold; agTxt.characterSpacing = 2.5f;
            agTxt.text = "대한민국 경찰청  /  KOREA NATIONAL POLICE AGENCY  /  수사과";
            traitAgencyRef = agTxt;

            var cm = MkText("CaseMeta", t_contentCard.transform, 11, TextAlignmentOptions.Left, new Color(0.18f, 0.20f, 0.24f, 1.0f));
            AnchorTopLeft(cm.rectTransform, new Vector2(24, -48), new Vector2(500, 18));
            cm.characterSpacing = 1.2f;
            cm.text = "CASE FILE  //  KR-2024-██████-RESTRICTED  //  수사 진행 요원 한정 열람";
            traitSubRef = cm;

            var divInner = MkPanel("DivInner", t_contentCard.transform, new Color(0.82f, 0.80f, 0.74f, 1.0f), false);
            AnchorTopLeft(divInner.GetComponent<RectTransform>(), new Vector2(20, -72), new Vector2(880, 2));

            // 본문 텍스트 (사진 칸 제거 → 전체 폭 사용)
            var traitText = MkText("TraitText", t_contentCard.transform, 20, TextAlignmentOptions.TopLeft, new Color(0.12f, 0.12f, 0.10f, 1.0f));
            AnchorTopLeft(traitText.rectTransform, new Vector2(24, -90), new Vector2(876, 680));
            traitText.textWrappingMode = TMPro.TextWrappingModes.Normal; traitText.lineSpacing = 12f;
            traitText.fontStyle = FontStyles.Normal; // 제목(Bold)과 대비

            // 하단 분류 띠
            var footerBar = MkPanel("FooterBar", t_contentCard.transform, new Color(0.90f, 0.86f, 0.80f, 1.0f), false);
            var fbRt = footerBar.GetComponent<RectTransform>();
            fbRt.anchorMin = Vector2.zero; fbRt.anchorMax = new Vector2(1, 0); fbRt.pivot = new Vector2(0.5f, 0); fbRt.sizeDelta = new Vector2(0, 34); fbRt.anchoredPosition = Vector2.zero;
            var footerTxt = MkText("FooterText", footerBar.transform, 11, TextAlignmentOptions.Center, new Color(0.72f, 0.11f, 0.10f, 1.0f));
            Full(footerTxt.rectTransform); footerTxt.fontStyle = FontStyles.Bold; footerTxt.characterSpacing = 3f;
            footerTxt.text = "SECRET  //  FOR OFFICIAL USE ONLY  //  열람 후 즉시 파기";
            traitFooterRef = footerTxt;

            // 종이 그레인/얼룩 오버레이
            {
                var grainSprite = LoadUiSprite(DossierGrainPath);
                if (grainSprite != null)
                {
                    var traitGrain = MkPanel("PaperGrainOverlay", traitBox.transform, new Color(1f, 1f, 1f, 0.55f), false);
                    Full(traitGrain.GetComponent<RectTransform>());
                    traitGrain.GetComponent<Image>().sprite = grainSprite;
                }
            }

            // 닫기 버튼
            var traitCloseBtn = MkButton("TraitCloseButton", traitBox.transform, "[ 수사 서류 확인 완료 ]\n(닫기)", out var traitCloseLbl);
            AnchorBox(traitCloseBtn.GetComponent<RectTransform>(), new Vector2(0.5f, 0.05f), new Vector2(400, 64));
            traitCloseLbl.fontSize = 20; traitCloseLbl.fontStyle = FontStyles.Bold;
            BtnColor(traitCloseBtn, new Color(0.7f, 0.72f, 0.74f, 1.0f)); // Metallic button
            if (traitCloseBtn.transform.Find("Border") != null) traitCloseBtn.transform.Find("Border").GetComponent<Image>().color = new Color(0.4f, 0.4f, 0.4f, 1f);

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

            // 효과음 배선: 타이핑(키보드) 루프 + 차 브레이크 원샷
            var typingClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/keyboard_type.wav");
            var brakeClip  = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/car_brake.wav");
            if (typingClip != null) Wire(audioMgr, "typingSfx", typingClip);
            else Debug.LogWarning("[Persuasion] keyboard_type.wav 없음");
            if (brakeClip != null) Wire(audioMgr, "carBrakeSfx", brakeClip);
            else Debug.LogWarning("[Persuasion] car_brake.wav 없음");

            // 효과음 배선: 설득도 상승/하락, 클리어/실패 스팅어, 버튼 클릭 (파일 없으면 조용히 스킵)
            void WireSfxIfExists(string fileName, string fieldName)
            {
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/" + fileName);
                if (clip != null) Wire(audioMgr, fieldName, clip);
                else Debug.LogWarning($"[Persuasion] {fileName} 없음 (선택 사항 — Assets/Audio/SFX/에 추가하면 자동 배선됨)");
            }
            WireSfxIfExists("persuasion_up.wav",   "persuasionUpSfx");
            WireSfxIfExists("persuasion_down.wav", "persuasionDownSfx");
            WireSfxIfExists("clear_stinger.wav",   "clearStingerSfx");
            WireSfxIfExists("fail_stinger.wav",    "failStingerSfx");
            WireSfxIfExists("button_click.wav",    "buttonClickSfx");

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
            Wire(ui, "introQuitButton",        introQuitBtn);
            Wire(ui, "transitionArtLibrary",   transitionLibrary);
            Wire(ui, "storyBeatPanel",         storyBeatPanel);
            Wire(ui, "storyBeatImage",         storyBeatImage);
            Wire(ui, "storyBeatText",          storyBeatText);
            Wire(ui, "storyBeatContinueButton",storyBeatContinueBtn);
            Wire(ui, "storyHeaderLabel",       storyHeader);
            Wire(ui, "storyPageIndicator",     storyPageIndicator);
            Wire(ui, "storyCaseLabel",         storyCaseLabel);
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
            Wire(ui, "evidenceContainer",      evidenceContainer.transform);
            Wire(ui, "traitPanel",             traitPanel);
            Wire(ui, "traitTitleText",         traitTitleText);
            Wire(ui, "traitText",              traitText);
            // traitPhotoImage 제거됨 (사진 칸 없앰)
            Wire(ui, "traitAgencyText",        traitAgencyRef);
            Wire(ui, "traitSubHeaderText",     traitSubRef);
            Wire(ui, "traitFooterText",        traitFooterRef);
            Wire(ui, "traitCloseButton",       traitCloseBtn);
            Wire(ui, "historyPanel",           historyPanel);
            Wire(ui, "historyContent",         histContentRt);
            Wire(ui, "historyScrollRect",      histScrollRect);
            Wire(ui, "historyCloseButton",     histCloseBtn);
            Wire(ui, "menuPanel",              menuPanel);
            Wire(ui, "musicSlider",            musicSlider);
            Wire(ui, "sfxSlider",              sfxSlider);
            Wire(ui, "voiceSlider",            voiceSlider);
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

        // ── 라운드/섀도 스프라이트 (각진 사각형 대신 둥근 모서리+깊이 → PPT 느낌 제거) ──
        private static Sprite _spRound, _spShadow;
        private const int ROUND_RADIUS = 28;
        private const int SHADOW_BLUR  = 22;

        private static Sprite RoundSprite  { get { if (_spRound  == null) _spRound  = EnsureSprite("ui_round",  ROUND_RADIUS, false, 0);           return _spRound;  } }
        private static Sprite ShadowSprite { get { if (_spShadow == null) _spShadow = EnsureSprite("ui_shadow", ROUND_RADIUS, true,  SHADOW_BLUR); return _spShadow; } }

        // PNG 에셋으로 저장한 9-slice 스프라이트를 생성/로드 (인메모리 스프라이트는 빌드에서 참조 유실됨)
        private static Sprite EnsureSprite(string fileName, int radius, bool shadow, int blur)
        {
            const string dir = "Assets/Art/Generated";
            string path = dir + "/" + fileName + ".png";
            if (!System.IO.Directory.Exists(dir)) { System.IO.Directory.CreateDirectory(dir); AssetDatabase.Refresh(); }

            var tex = shadow ? MakeShadowTex(radius, blur) : MakeRoundedTex(radius);
            System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);

            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
            ti.textureType         = TextureImporterType.Sprite;
            ti.spriteImportMode    = SpriteImportMode.Single;
            ti.mipmapEnabled       = false;
            ti.alphaIsTransparency = true;
            ti.wrapMode            = TextureWrapMode.Clamp;
            ti.filterMode          = FilterMode.Bilinear;
            ti.textureCompression  = TextureImporterCompression.Uncompressed;
            var st = new TextureImporterSettings();
            ti.ReadTextureSettings(st);
            int bd = shadow ? radius + blur : radius;
            st.spriteBorder    = new Vector4(bd, bd, bd, bd);
            st.spriteMeshType  = SpriteMeshType.FullRect;
            st.spriteGenerateFallbackPhysicsShape = false;
            ti.SetTextureSettings(st);
            ti.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        // 모서리 반경 radius인 흰색 라운드 사각형(안티에일리어스). 색은 Image.color로 틴트.
        private static Texture2D MakeRoundedTex(int radius)
        {
            int s = radius * 2 + 4;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false, true);
            var px = new Color32[s * s];
            float half = s * 0.5f, b = half;
            for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float dx = Mathf.Abs(x + 0.5f - half) - (b - radius);
                float dy = Mathf.Abs(y + 0.5f - half) - (b - radius);
                float o  = Mathf.Sqrt(Mathf.Max(dx,0f)*Mathf.Max(dx,0f) + Mathf.Max(dy,0f)*Mathf.Max(dy,0f));
                float i  = Mathf.Min(Mathf.Max(dx, dy), 0f);
                float d  = o + i - radius;                 // rounded-rect SDF
                float a  = Mathf.Clamp01(0.5f - d);        // 1px AA
                px[y*s + x] = new Color32(255,255,255,(byte)(a*255));
            }
            tex.SetPixels32(px); tex.Apply();
            return tex;
        }

        // 부드러운 드롭섀도(반경 radius, blur 만큼 알파 감쇠). 검정 틴트로 사용.
        private static Texture2D MakeShadowTex(int radius, int blur)
        {
            int ext = radius + blur;
            int s = ext * 2 + 4;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false, true);
            var px = new Color32[s * s];
            float half = s * 0.5f, b = half - blur;
            for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float dx = Mathf.Abs(x + 0.5f - half) - (b - radius);
                float dy = Mathf.Abs(y + 0.5f - half) - (b - radius);
                float o  = Mathf.Sqrt(Mathf.Max(dx,0f)*Mathf.Max(dx,0f) + Mathf.Max(dy,0f)*Mathf.Max(dy,0f));
                float i  = Mathf.Min(Mathf.Max(dx, dy), 0f);
                float d  = o + i - radius;
                float a  = d <= 0f ? 1f : Mathf.Clamp01(1f - d/blur); a *= a;
                px[y*s + x] = new Color32(255,255,255,(byte)(a*255));
            }
            tex.SetPixels32(px); tex.Apply();
            return tex;
        }

        // ── 설정 아이콘(음표/스피커/마이크) 절차적 생성 ──
        private static Sprite _icoMusic, _icoSpeaker, _icoMic;
        private static Sprite IconMusic   { get { if (_icoMusic   == null) _icoMusic   = EnsureIcon("ico_music",   DrawMusic);   return _icoMusic;   } }
        private static Sprite IconSpeaker { get { if (_icoSpeaker == null) _icoSpeaker = EnsureIcon("ico_speaker", DrawSpeaker); return _icoSpeaker; } }
        private static Sprite IconMic     { get { if (_icoMic     == null) _icoMic     = EnsureIcon("ico_mic",     DrawMic);     return _icoMic;     } }

        private static Sprite EnsureIcon(string fileName, System.Action<Color32[], int> draw)
        {
            const string dir = "Assets/Art/Generated";
            string path = dir + "/" + fileName + ".png";
            if (!System.IO.Directory.Exists(dir)) { System.IO.Directory.CreateDirectory(dir); AssetDatabase.Refresh(); }
            const int S = 72;
            var tex = new Texture2D(S, S, TextureFormat.RGBA32, false, true);
            var px = new Color32[S * S];
            draw(px, S);
            tex.SetPixels32(px); tex.Apply();
            System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);
            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
            ti.textureType = TextureImporterType.Sprite; ti.spriteImportMode = SpriteImportMode.Single;
            ti.mipmapEnabled = false; ti.alphaIsTransparency = true; ti.filterMode = FilterMode.Bilinear;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        // 픽셀 프리미티브 (백색, 안티에일리어스 근사)
        private static void Px(Color32[] p, int S, int x, int y, float a)
        {
            if (x < 0 || y < 0 || x >= S || y >= S || a <= 0f) return;
            int i = y * S + x; byte na = (byte)(Mathf.Clamp01(a) * 255);
            if (na > p[i].a) p[i] = new Color32(255, 255, 255, na);
        }
        private static void Disc(Color32[] p, int S, float cx, float cy, float r)
        {
            for (int y = (int)(cy - r - 1); y <= cy + r + 1; y++)
            for (int x = (int)(cx - r - 1); x <= cx + r + 1; x++)
            { float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)); Px(p, S, x, y, Mathf.Clamp01(r - d + 0.5f)); }
        }
        private static void FRect(Color32[] p, int S, float x0, float y0, float x1, float y1)
        {
            for (int y = (int)y0; y <= y1; y++) for (int x = (int)x0; x <= x1; x++) Px(p, S, x, y, 1f);
        }
        private static void Ring(Color32[] p, int S, float cx, float cy, float r, float th, float a0, float a1)
        {
            for (int y = (int)(cy - r - th); y <= cy + r + th; y++)
            for (int x = (int)(cx - r - th); x <= cx + r + th; x++)
            {
                float dx = x - cx, dy = y - cy; float d = Mathf.Sqrt(dx * dx + dy * dy);
                float ang = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg; if (ang < 0) ang += 360;
                if (ang < a0 || ang > a1) continue;
                Px(p, S, x, y, Mathf.Clamp01(th - Mathf.Abs(d - r) + 0.5f));
            }
        }
        private static void DrawMusic(Color32[] p, int S)
        {
            Disc(p, S, 24, 50, 9);                 // 음표 머리
            FRect(p, S, 31, 18, 34, 51);           // 기둥
            FRect(p, S, 34, 18, 50, 22);           // 깃발 상단
            FRect(p, S, 46, 22, 50, 34);           // 깃발 세로
        }
        private static void DrawSpeaker(Color32[] p, int S)
        {
            FRect(p, S, 16, 30, 26, 42);           // 스피커 몸통
            for (int y = 22; y <= 50; y++)         // 원뿔
            { float w = (Mathf.Abs(y - 36) <= 6) ? 14 : 14 - (Mathf.Abs(y - 36) - 6) * 1.4f; if (w > 0) FRect(p, S, 26, y, 26 + w, y); }
            Ring(p, S, 40, 36, 12, 2.2f, -45, 45); // 음파 1
            Ring(p, S, 40, 36, 19, 2.2f, -45, 45); // 음파 2
        }
        private static void DrawMic(Color32[] p, int S)
        {
            FRect(p, S, 29, 14, 43, 40); Disc(p, S, 36, 14, 7); Disc(p, S, 36, 40, 7); // 캡슐 머리
            Ring(p, S, 36, 38, 15, 2.4f, 200, 340);   // 받침 곡선
            FRect(p, S, 34.5f, 52, 37.5f, 60);         // 스탠드
            FRect(p, S, 27, 60, 45, 63);               // 받침대
        }

        // 이미지에 둥근 모서리 적용
        private static void Round(Image img, float cornerScale = 2.0f)
        {
            if (img == null) return;
            img.sprite = RoundSprite;
            img.type   = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = cornerScale;
        }

        // 대상 뒤에 부드러운 그림자 추가(첫 자식)
        private static void AddShadow(GameObject target, float spread = 20f, float alpha = 0.38f, float yOffset = -5f)
        {
            if (target == null) return;
            var sh = new GameObject("Shadow", typeof(RectTransform), typeof(Image));
            sh.transform.SetParent(target.transform, false);
            sh.transform.SetAsFirstSibling();
            var rt = sh.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(-spread, -spread + yOffset);
            rt.offsetMax = new Vector2( spread,  spread + yOffset);
            var img = sh.GetComponent<Image>();
            img.sprite = ShadowSprite;
            img.type   = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = 1f;
            img.color  = new Color(0f, 0f, 0f, alpha);
            img.raycastTarget = false;
        }

        // ── UI 팩토리 ────────────────────────────────────────────────

        // 외부 PNG를 Sprite(Single)로 강제 임포트 후 로드. Dossier 종이/그레인/스탬프 텍스처용.
        private static Sprite LoadUiSprite(string assetPath)
        {
            var imp = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (imp != null && (imp.textureType != TextureImporterType.Sprite || imp.spriteImportMode != SpriteImportMode.Single))
            {
                imp.textureType      = TextureImporterType.Sprite;
                imp.spriteImportMode = SpriteImportMode.Single;
                imp.mipmapEnabled    = false;
                imp.alphaIsTransparency = true;
                imp.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        }

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
            t.textWrappingMode = TMPro.TextWrappingModes.Normal;
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
            Round(img);   // 둥근 모서리

            // Stamp border: brighter thin rounded line around each button
            var bdrGO = new GameObject("Border", typeof(RectTransform), typeof(Image));
            bdrGO.transform.SetParent(go.transform, false);
            bdrGO.transform.SetAsFirstSibling();
            Full(bdrGO.GetComponent<RectTransform>());
            bdrGO.GetComponent<RectTransform>().offsetMin = new Vector2(-1.5f, -1.5f);
            bdrGO.GetComponent<RectTransform>().offsetMax = new Vector2( 1.5f,  1.5f);
            var bdrImg = bdrGO.GetComponent<Image>();
            bdrImg.color = new Color(1f, 1f, 1f, 0.12f);
            bdrImg.raycastTarget = false;
            Round(bdrImg);

            // 부드러운 그림자로 깊이감
            AddShadow(go, 16f, 0.32f, -4f);

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
            var bgImg = bg.GetComponent<Image>();
            bgImg.color = new Color(0.06f, 0.07f, 0.10f, 0.85f);
            Round(bgImg, 3.2f);   // 둥근 캡슐 트랙

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
            Round(fillImg, 3.2f);   // 둥근 채움

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

        // 설정용 원형 핸들 볼륨 슬라이더 (둥근 트랙 + 흰 원형 핸들)
        private static Sprite _spCircle;
        private static Sprite CircleSprite { get { if (_spCircle == null) _spCircle = EnsureIcon("ui_circle", (p, S) => Disc(p, S, S / 2f, S / 2f, S / 2f - 1.5f)); return _spCircle; } }

        private static Slider MkVolumeSlider(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Slider));
            go.transform.SetParent(parent, false);
            var slider = go.GetComponent<Slider>();

            var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(go.transform, false);
            var bgRt = bg.GetComponent<RectTransform>();
            bgRt.anchorMin = new Vector2(0, 0.5f); bgRt.anchorMax = new Vector2(1, 0.5f);
            bgRt.sizeDelta = new Vector2(0, 14); bgRt.anchoredPosition = Vector2.zero;
            var bgImg = bg.GetComponent<Image>(); bgImg.color = new Color(0.17f, 0.19f, 0.25f, 1f); Round(bgImg, 3.4f);

            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(go.transform, false);
            var faRt = fillArea.GetComponent<RectTransform>();
            faRt.anchorMin = new Vector2(0, 0.5f); faRt.anchorMax = new Vector2(1, 0.5f);
            faRt.sizeDelta = new Vector2(0, 14); faRt.anchoredPosition = Vector2.zero;
            var fillGO = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillGO.transform.SetParent(fillArea.transform, false);
            var fillRt = fillGO.GetComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero; fillRt.anchorMax = Vector2.one; fillRt.offsetMin = fillRt.offsetMax = Vector2.zero;
            var fillImg = fillGO.GetComponent<Image>(); fillImg.color = new Color(0.82f, 0.86f, 0.96f, 1f); Round(fillImg, 3.4f);

            var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleArea.transform.SetParent(go.transform, false);
            Full(handleArea.GetComponent<RectTransform>());
            var handleGO = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handleGO.transform.SetParent(handleArea.transform, false);
            var hRt = handleGO.GetComponent<RectTransform>();
            hRt.sizeDelta = new Vector2(26, 26);
            var hImg = handleGO.GetComponent<Image>(); hImg.sprite = CircleSprite; hImg.color = Color.white;

            slider.fillRect = fillRt; slider.handleRect = hRt; slider.targetGraphic = hImg;
            slider.direction = Slider.Direction.LeftToRight;
            slider.transition = Selectable.Transition.None;
            slider.navigation = new Navigation { mode = Navigation.Mode.None };
            slider.minValue = 0f; slider.maxValue = 1f; slider.wholeNumbers = false;
            slider.interactable = true; slider.value = 1f;
            return slider;
        }

        // 라벨 + 볼륨 슬라이더 한 줄
        private static Slider MkVolumeRow(Transform parent, string labelText, float anchorY)
        {
            var txtGO = MkText("Label", parent, 24, TextAlignmentOptions.Right, new Color(0.90f, 0.92f, 0.97f, 1f));
            txtGO.text = labelText;
            txtGO.fontStyle = FontStyles.Bold;
            var txtRt = txtGO.GetComponent<RectTransform>();
            txtRt.anchorMin = txtRt.anchorMax = new Vector2(0.5f, anchorY); txtRt.pivot = new Vector2(1f, 0.5f);
            txtRt.sizeDelta = new Vector2(120, 40); txtRt.anchoredPosition = new Vector2(-190, 0);

            var slider = MkVolumeSlider("Slider", parent);
            var sRt = slider.GetComponent<RectTransform>();
            sRt.anchorMin = sRt.anchorMax = new Vector2(0.5f, anchorY); sRt.pivot = new Vector2(0.5f, 0.5f);
            sRt.sizeDelta = new Vector2(410, 30); sRt.anchoredPosition = new Vector2(35, 0);
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
            if (inputBg != null) { inputBg.color = new Color(0.08f, 0.09f, 0.12f, 0.95f); Round(inputBg); }

            foreach (var t in go.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (_font != null) t.font = _font;
                t.fontSize = TYPE_BODY;
                t.color = TXT_PRIMARY;
                t.textWrappingMode = TMPro.TextWrappingModes.Normal;
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
            t.textWrappingMode = TMPro.TextWrappingModes.Normal;
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

        // 오브젝트 배열 필드 배선 (Sprite[] 등)
        private static void WireArray(Object target, string field, Object[] values)
        {
            var so = new SerializedObject(target);
            var p  = so.FindProperty(field);
            if (p == null) { Debug.LogError($"[Persuasion] 배열 필드 못 찾음: {target.GetType().Name}.{field}"); return; }
            p.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedProperties();
        }

        // PNG를 Sprite 타입으로 임포트 (배경 슬라이드용). 이미 Sprite면 통과.
        private static Sprite LoadAsSprite(string path)
        {
            var ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti != null && ti.textureType != TextureImporterType.Sprite)
            {
                ti.textureType     = TextureImporterType.Sprite;
                ti.spriteImportMode = SpriteImportMode.Single;
                ti.mipmapEnabled    = false;
                ti.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
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
