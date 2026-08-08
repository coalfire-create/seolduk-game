using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using Persuasion.AI;
using Persuasion.Core;
using Persuasion.Audio;

namespace Persuasion.UI
{
    /// <summary>
    /// 인트로 → 진행 → 클리어/실패 화면을 전환하며 진행하는 UI 컨트롤러.
    /// 모든 필드는 Unity Editor에서 씬의 실제 UI 오브젝트를 드래그해 연결한다.
    /// </summary>
    public class PersuasionUI : MonoBehaviour
    {
        [Header("참조")]
        [SerializeField] private PersuasionManager manager;

        [Header("인트로 패널")]
        [SerializeField] private GameObject introPanel;
        [SerializeField] private TMP_Text gameTitleText;
        [SerializeField] private CanvasGroup introTitleGroup;   // 타이틀 페이드/스케일 애니메이션 대상
        [SerializeField] private Button startButton;
        [SerializeField] private Image introBackgroundImage;
        [SerializeField] private Graphic recDotGraphic;
        [SerializeField] private Button introMenuButton;        // 인트로 '설정' 버튼
        [SerializeField] private Button introQuitButton;        // 인트로 '종료' 버튼

        [Header("스토리 비트 패널 (인트로컷/전환컷)")]
        [SerializeField] private GameObject storyBeatPanel;
        [SerializeField] private Image storyBeatImage;
        [SerializeField] private TMP_Text storyBeatText;
        [SerializeField] private Button storyBeatContinueButton;
        [SerializeField] private TransitionArtLibrary transitionArtLibrary;
        [SerializeField] private TMP_Text storyHeaderLabel;    // "◆ 사건 현장" / "◆ 용의자 프로파일"
        [SerializeField] private TMP_Text storyPageIndicator; // "1 / 5"
        [SerializeField] private TMP_Text storyCaseLabel;     // "CASE #001"
        [Header("단계 및 목표 연출 오버레이")]
        [SerializeField] private GameObject stageIntroOverlay;
        [SerializeField] private CanvasGroup stageIntroGroup;
        [SerializeField] private TMP_Text introStageTitleText;
        [SerializeField] private TMP_Text introStageSubTitleText;
        [SerializeField] private TMP_Text introGoalText;
        [SerializeField] private RectTransform introGoalCardRt;

        [Header("스테이지 선택 패널")]
        [SerializeField] private GameObject stageSelectPanel;
        [SerializeField] private Transform stageButtonContainer;
        [SerializeField] private GameObject stageButtonPrefab;
        [SerializeField] private StagePortraitLibrary portraitLibrary;
        [SerializeField] private Button stageSelectBackButton;

        [Header("진행 패널")]
        [SerializeField] private GameObject playPanel;
        [SerializeField] private TMP_Text chapterTitleText;
        [SerializeField] private TMP_Text stageTitleText;
        [SerializeField] private TMP_Text characterNameText;
        [SerializeField] private TMP_Text backgroundNarrationText; // 이제 캐릭터 특징(persona)을 표시
        [SerializeField] private TMP_Text goalText;                // 목표 배너
        [SerializeField] private Transform chatContent;            // ScrollView > Viewport > Content
        [SerializeField] private ScrollRect chatScrollRect;
        [SerializeField] private GameObject playerBubblePrefab;
        [SerializeField] private GameObject npcBubblePrefab;
        [SerializeField] private TMP_InputField inputField;
        [SerializeField] private Button sendButton;
        [SerializeField] private Button storyPeekButton;          // "스토리 보기"
        [SerializeField] private Button traitButton;              // "특징 보기"
        [SerializeField] private Button historyButton;            // "대화 기록"
        [SerializeField] private Button menuButton;               // "메뉴"
        [SerializeField] private Slider persuasionSlider;
        [SerializeField] private TMP_Text persuasionValueText;   // 설득도 % 숫자 표시
        [SerializeField] private TMP_Text emotionLabel;
        [SerializeField] private GameObject hintPanel;
        [SerializeField] private TMP_Text hintText;
        [SerializeField] private Transform evidenceContainer; // 하단 증거 바

        [Header("피의자 특징 프로필 패널")]
        [SerializeField] private GameObject traitPanel;
        [SerializeField] private TMP_Text traitTitleText;
        [SerializeField] private TMP_Text traitText;
        [SerializeField] private Image traitPhotoImage;
        [SerializeField] private Button traitCloseButton;
        [SerializeField] private TMP_Text traitAgencyText;    // 상단 기관명 바 (챕터별)
        [SerializeField] private TMP_Text traitSubHeaderText; // 부제(CASE FILE // ...) (챕터별)
        [SerializeField] private TMP_Text traitFooterText;    // 하단 분류 띠 (챕터별)

        [Header("대화 기록 패널 (읽기 전용)")]
        [SerializeField] private GameObject historyPanel;
        [SerializeField] private Transform historyContent;
        [SerializeField] private ScrollRect historyScrollRect;
        [SerializeField] private Button historyCloseButton;

        [Header("메뉴 패널 (소리/나가기)")]
        [SerializeField] private GameObject menuPanel;
        [SerializeField] private Slider musicSlider;   // 음악
        [SerializeField] private Slider sfxSlider;     // 효과음
        [SerializeField] private Slider voiceSlider;   // 음성
        [SerializeField] private Button homeButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private Button menuCloseButton;

        [Header("게임 설명서 패널 (지침서 모달)")]
        [SerializeField] private GameObject guidePanel;
        [SerializeField] private Button guideOpenButton;       // 메뉴 내부 '게임 설명서' 버튼
        [SerializeField] private Button guideCloseButton;      // 설명서 닫기 버튼
        [SerializeField] private Button introGuideButton;       // 인트로 화면 '게임 설명서' 버튼 (하위 호환)

        [Header("결과 패널")]
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private TMP_Text resultText;
        [SerializeField] private TMP_Text resultNarrationText;
        [SerializeField] private Button nextButton;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button resultSelectButton;

        [Header("결과 패널 – 클리어 연출")]
        [SerializeField] private TMP_Text resultGradeText;
        [SerializeField] private TMP_Text resultStatsText;
        [SerializeField] private TMP_Text resultPersonalBestText;
        [SerializeField] private GameObject resultBestMovePanel;
        [SerializeField] private TMP_Text resultBestMoveText;
        [SerializeField] private Image clearFlashOverlay;

        [Header("결과 패널 – 공유")]
        [SerializeField] private Button shareButton;

        [Header("인트로 패널 – 개인정보")]
        [SerializeField] private Button privacyButton;

        [Header("글로벌 리더보드")]
        [SerializeField] private LeaderboardManager leaderboard;
        [SerializeField] private Button openLeaderboardButton;   // 결과 화면 '랭킹' 버튼
        [SerializeField] private GameObject leaderboardPanel;
        [SerializeField] private TMP_Text leaderboardTitleText;
        [SerializeField] private TMP_Text leaderboardMyRecordText;
        [SerializeField] private TMP_Text leaderboardListText;
        [SerializeField] private TMP_InputField leaderboardNameInput;
        [SerializeField] private Button leaderboardSubmitButton;
        [SerializeField] private Button leaderboardCloseButton;

        // 결과 패널 런타임 컬러
        static readonly Color s_Gold   = new Color(0.96f, 0.83f, 0.55f, 1f);
        static readonly Color s_Danger = new Color(0.90f, 0.40f, 0.40f, 1f);

        // 첫 플레이 시 게임 설명서 자동 표시
        private bool _hasShownGuide = false;

        // 대화 기록(현재 스테이지) — 기록 보기 패널 재구성용
        private readonly List<ChatEntry> _chatLog = new List<ChatEntry>();
        private ChatBubble _pendingNpcBubble;   // NPC 응답 대기 중인 인디케이터 말풍선
        private Coroutine _scrollRoutine;
        private Coroutine _apiTimeoutRoutine;
        private Coroutine _clearCoroutine;
        private Coroutine _sendLabelRoutine;
        private const float ApiTimeoutSec = 30f;
        private KoreanInputBridge _korBridge;
        private Coroutine _storyTypeRoutine;    // 스토리 나래이션 타자기 효과
        private bool _storyTyping;              // 나래이션 타이핑 진행 중 여부

        private struct ChatEntry { public bool isPlayer; public string text; }

        private void Awake()
        {
            startButton.onClick.AddListener(() => manager.GoToStageSelect());
            if (storyBeatContinueButton != null) storyBeatContinueButton.onClick.AddListener(OnStoryBeatContinue);
            // 오프닝 배경은 IntroSlideshow(3장 크로스페이드)가 담당하므로 여기서 titleScreen을 덮어쓰지 않는다.
            sendButton.onClick.AddListener(OnClickSend);
            if (inputField != null) inputField.onSubmit.AddListener(_ => OnClickSend()); // 엔터로 전송

            // 한글 IME 브리지 (WebGL Windows 대응)
            _korBridge = gameObject.AddComponent<KoreanInputBridge>();
            _korBridge.Init(inputField, OnClickSend);
            nextButton.onClick.AddListener(() => manager.AdvanceToNextStage());
            retryButton.onClick.AddListener(() => manager.RetryCurrentStage());
            if (stageSelectBackButton != null) stageSelectBackButton.onClick.AddListener(() => { manager.GoToIntro(); });
            if (resultSelectButton != null) resultSelectButton.onClick.AddListener(() => manager.GoToStageSelect());
            if (shareButton   != null) shareButton.onClick.AddListener(OnClickShare);
            if (privacyButton != null) privacyButton.onClick.AddListener(OnClickPrivacy);
            if (openLeaderboardButton   != null) openLeaderboardButton.onClick.AddListener(OpenLeaderboard);
            if (leaderboardSubmitButton != null) leaderboardSubmitButton.onClick.AddListener(SubmitScore);
            if (leaderboardCloseButton  != null) leaderboardCloseButton.onClick.AddListener(CloseLeaderboard);

            // 스토리 보기 / 특징 보기 / 대화 기록 / 메뉴 / 게임 설명서
            if (storyPeekButton != null) storyPeekButton.onClick.AddListener(OnClickStoryPeek);
            if (traitButton != null) traitButton.onClick.AddListener(OpenTraitPanel);
            if (traitCloseButton != null) traitCloseButton.onClick.AddListener(CloseTraitPanel);
            if (historyButton != null) historyButton.onClick.AddListener(OpenHistory);
            if (historyCloseButton != null) historyCloseButton.onClick.AddListener(CloseHistory);
            if (menuButton != null) menuButton.onClick.AddListener(OpenMenu);
            if (introMenuButton != null) introMenuButton.onClick.AddListener(OpenMenu);
            if (introQuitButton != null) introQuitButton.onClick.AddListener(QuitGame);
            if (menuCloseButton != null) menuCloseButton.onClick.AddListener(CloseMenu);
            if (homeButton != null) homeButton.onClick.AddListener(GoToHome);
            if (quitButton != null) quitButton.onClick.AddListener(QuitGame);
            if (guideOpenButton != null) guideOpenButton.onClick.AddListener(OpenGuide);
            if (introGuideButton != null) introGuideButton.onClick.AddListener(OpenGuide);
            if (guideCloseButton != null) guideCloseButton.onClick.AddListener(CloseGuide);
            SetupVolumeSlider(musicSlider, () => AudioManager.Instance?.MusicVolume01 ?? 1f, v => AudioManager.Instance?.SetMusicVolume(v));
            SetupVolumeSlider(sfxSlider,   () => AudioManager.Instance?.SfxVolume01   ?? 1f, v => AudioManager.Instance?.SetSfxVolume(v));
            SetupVolumeSlider(voiceSlider, () => AudioManager.Instance?.VoiceVolume01 ?? 1f, v => AudioManager.Instance?.SetVoiceVolume(v));

            // 씬에 이미 배치된 모든 버튼에 클릭 사운드 훅을 일괄 연결 (Canvas 전체 대상)
            var canvasRoot = transform.parent != null ? transform.parent : transform;
            foreach (var btn in canvasRoot.GetComponentsInChildren<Button>(true))
                btn.onClick.AddListener(() => AudioManager.Instance?.PlayButtonClick());
        }

        /// <summary>볼륨 슬라이더를 저장값으로 초기화하고 변경 시 해당 채널에 적용.</summary>
        private void SetupVolumeSlider(Slider slider, System.Func<float> getter, System.Action<float> setter)
        {
            if (slider == null) return;
            slider.SetValueWithoutNotify(Mathf.Clamp01(getter()));
            slider.onValueChanged.AddListener(v => setter(v));
        }

        private void OnEnable()
        {
            manager.OnNPCReplied += HandleNPCReplied;
            manager.OnError += HandleError;
            manager.OnPhaseChanged += HandlePhaseChanged;
            manager.OnEvidenceUnlocked += HandleEvidenceUnlocked;
        }

        private void OnDisable()
        {
            manager.OnNPCReplied -= HandleNPCReplied;
            manager.OnError -= HandleError;
            manager.OnPhaseChanged -= HandlePhaseChanged;
            manager.OnEvidenceUnlocked -= HandleEvidenceUnlocked;
        }

        private void Start()
        {
            hintPanel.SetActive(false);
            if (stageIntroOverlay != null) stageIntroOverlay.SetActive(false);
            if (traitPanel != null) traitPanel.SetActive(false);
            if (historyPanel != null) historyPanel.SetActive(false);
            if (menuPanel != null) menuPanel.SetActive(false);
            if (guidePanel != null) guidePanel.SetActive(false);
            if (guidePanel != null) guidePanel.SetActive(false);
            if (leaderboardPanel != null) leaderboardPanel.SetActive(false);
            if (evidenceContainer != null) evidenceContainer.gameObject.SetActive(false);
            ShowOnly(introPanel);
        }

        private void OpenTraitPanel()
        {
            if (traitPanel == null) return;
            var stage = manager != null ? manager.CurrentStage : null;
            if (stage != null)
            {
                // 챕터별 수사 파일 형식 (경찰 취조 / 첩보 공작 / AI 저항군)
                string chap = stage.chapterId ?? "";
                string agency, title, sub, footer, sec1, sec2, sec3, sec4;
                string lblName, lblRank, rankVal, lblBg, lblPsy, lblTacticHdr, lblTactic, lblGoal;
                if (chap == "ch2_spy")
                {
                    agency = "정보총국     COVERT OPERATIONS BUREAU     대상분석과";
                    title  = "■  공작 대상 정보 파일";
                    sub    = "DOSSIER  //  EYES-ONLY  //  현장 공작원 한정 열람";
                    footer = "TOP SECRET  //  열람 후 즉시 소각  //  발각 주의";
                    sec1 = "1. 대상 인적사항 & 접근 등급";  sec2 = "2. 배경 및 정황";
                    sec3 = "3. 심리 성향 및 경계 수준";      sec4 = "4. 현장 공작 지침";
                    lblName = "성명 / 직위"; lblRank = "접근 분류"; rankVal = "최우선 공략 대상 (기밀 확보 목표)";
                    lblBg = "배경 정황"; lblPsy = "성향 및 경계";
                    lblTacticHdr = "침투 전술"; lblTactic = "대상의 성향과 경계 수준을 파악해 신뢰를 얻고 방심을 유도";
                    lblGoal = "공작 목표";
                }
                else if (chap == "ch3_ai")
                {
                    agency = "인류 저항군 사령부     HUMAN RESISTANCE COMMAND     정보분석반";
                    title  = "■  대상 분석 파일";
                    sub    = "TARGET ANALYSIS  //  ENCRYPTED  //  저항군 요원 한정 열람";
                    footer = "CLASSIFIED  //  전송 후 즉시 파기  //  AI 감청 주의";
                    sec1 = "1. 대상 식별정보 & 위협 등급";  sec2 = "2. 배경 및 정황";
                    sec3 = "3. 행동 패턴 및 방어 체계";      sec4 = "4. 공략 지침";
                    lblName = "식별명 / 유형"; lblRank = "위협 등급"; 
                    rankVal = (stage.stageId == "stage_ai_terminal") ? "하위 (말단 감시 단말기)" : "최상위 (핵심 정보 보유)";
                    lblBg = "배경 정황"; lblPsy = "행동 패턴";
                    lblTacticHdr = "공략 전술"; lblTactic = "대상의 논리 체계와 허점을 파악해 방어 로직을 우회";
                    lblGoal = "작전 목표";
                }
                else // ch1_interrogation (기본: 경찰)
                {
                    agency = "대한민국 경찰청     KOREA NATIONAL POLICE AGENCY     수사과";
                    title  = "■  피의자 수사 분석 보고서";
                    sub    = "CASE FILE  //  KBI-2024-RESTRICTED  //  수사 전담 요원 한정 열람";
                    footer = "SECRET  //  FOR OFFICIAL USE ONLY  //  열람 후 즉시 파기";
                    sec1 = "1. 피의자 인적사항 & 수사 등급"; sec2 = "2. 배경 사건 및 정황";
                    sec3 = "3. 심리 성향 및 진술 패턴 분석";  sec4 = "4. 담당 수사관 심문 지침";
                    lblName = "성명 / 직업"; lblRank = "수사 분류"; rankVal = "1급 자백 유도 대상 (주요 피의자)";
                    lblBg = "사건 정황"; lblPsy = "행동 특성";
                    lblTacticHdr = "권장 전술"; lblTactic = "상대의 성향과 관계, 약점을 파악해 심리적 동요 유도";
                    lblGoal = "수사 목표";
                }

                if (traitTitleText     != null) traitTitleText.text     = title;
                if (traitAgencyText    != null) traitAgencyText.text    = agency;
                if (traitSubHeaderText != null) traitSubHeaderText.text = sub;
                if (traitFooterText    != null) traitFooterText.text    = footer;

                if (traitPhotoImage != null && portraitLibrary != null)
                {
                    var set = portraitLibrary.Find(stage.stageId);
                    // introSprite(캐릭터 소개 일러스트) 우선, 없으면 baseSprite 폴백
                    Sprite photo = (set != null) ? (set.introSprite != null ? set.introSprite : set.baseSprite) : null;
                    if (photo != null)
                    {
                        traitPhotoImage.sprite = photo;
                        traitPhotoImage.color = Color.white;
                        traitPhotoImage.enabled = true;

                        var fitter = traitPhotoImage.GetComponent<AspectRatioFitter>();
                        if (fitter == null) fitter = traitPhotoImage.gameObject.AddComponent<AspectRatioFitter>();
                        fitter.aspectRatio = photo.rect.width / photo.rect.height;
                        fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                    }
                }

                if (traitText != null)
                {
                    // 페르소나 공략 정보 태그 제거
                    string personaShown = stage.characterPersona ?? string.Empty;
                    int tagIdx = personaShown.IndexOf('[');
                    if (tagIdx > 0) personaShown = personaShown.Substring(0, tagIdx).Trim();

                    // 배경/정황: 줄바꿈을 한 문단으로 정리 (인라인 불릿 깨짐 방지)
                    string bgSummary = string.Empty;
                    if (!string.IsNullOrEmpty(stage.backgroundNarration))
                    {
                        bgSummary = stage.backgroundNarration
                            .Replace("\r\n", "\n").Replace("\n\n", " ").Replace("\n", " ").Trim();
                    }

                    traitText.text =
                        $"<b>[{sec1}]</b>\n" +
                        $"  • <b>{lblName}</b> : {stage.characterName}\n" +
                        $"  • <b>{lblRank}</b> : {rankVal}\n\n" +
                        $"<b>[{sec2}]</b>\n" +
                        $"  • <b>{lblBg}</b> : {bgSummary}\n\n" +
                        $"<b>[{sec3}]</b>\n" +
                        $"  • <b>{lblPsy}</b> : {personaShown}\n\n" +
                        $"<b>[{sec4}]</b>\n" +
                        $"  • <b>{lblTacticHdr}</b> : {lblTactic}\n" +
                        $"  • <b>{lblGoal}</b> : {stage.goal}";
                }
            }
            traitPanel.SetActive(true);
        }

        private void CloseTraitPanel()
        {
            if (traitPanel != null) traitPanel.SetActive(false);
        }

        private void Update()
        {
            if (introPanel != null && introPanel.activeSelf)
            {
                bool startTriggered = false;
#if ENABLE_INPUT_SYSTEM
                if (Keyboard.current != null)
                {
                    if (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame)
                        startTriggered = true;
                }
#else
                if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
                    startTriggered = true;
#endif
                if (startTriggered && startButton != null && startButton.interactable)
                {
                    startButton.onClick.Invoke();
                }
            }

            if (recDotGraphic != null && introPanel != null && introPanel.activeSelf)
            {
                float alpha = 0.35f + Mathf.PingPong(Time.unscaledTime * 2.8f, 0.65f);
                Color c = recDotGraphic.color;
                c.a = alpha;
                recDotGraphic.color = c;
            }

            // 플레이 진행 중: 키보드를 누르면 별도 클릭 없이 여백에 즉시 입력되도록 포커스 유지
            // (HTML 오버레이가 활성 중이면 이미 입력 처리 중이므로 추가 포커스 불필요)
            if (playPanel != null && playPanel.activeSelf && inputField != null && inputField.interactable)
            {
                bool overlayActive = _korBridge != null && _korBridge.IsOverlayActive;
                if (!inputField.isFocused && !overlayActive)
                {
#if ENABLE_INPUT_SYSTEM
                    if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
                        FocusInput();
#else
                    if (Input.anyKeyDown)
                        FocusInput();
#endif
                }
            }
        }

        // ================= 1. 타이틀 애니메이션 =================

        private void PlayIntroTitleAnim()
        {
            if (gameTitleText == null || !isActiveAndEnabled) return;
            StartCoroutine(IntroTitleRoutine());
        }

        private IEnumerator IntroTitleRoutine()
        {
            var title = gameTitleText;
            if (title == null) yield break;
            if (introTitleGroup != null)
            {
                introTitleGroup.alpha = 1f;
                introTitleGroup.GetComponent<RectTransform>().localScale = Vector3.one;
            }

            // 그룹 내 그림자/부제 찾기
            TMP_Text shadow = null, sub = null;
            if (introTitleGroup != null)
            {
                var st = introTitleGroup.transform.Find("TitleShadow"); if (st != null) shadow = st.GetComponent<TMP_Text>();
                var su = introTitleGroup.transform.Find("Subtitle");    if (su != null) sub    = su.GetComponent<TMP_Text>();
            }
            if (sub != null) { var c = sub.color; c.a = 0f; sub.color = c; }

            const float stagger = 0.11f, fallDur = 0.5f, dropH = 470f;
            title.ForceMeshUpdate();
            int count = title.textInfo.characterCount;
            float total = stagger * Mathf.Max(0, count - 1) + fallDur + 0.15f;

            float t = 0f;
            while (t < total)
            {
                t += Time.unscaledDeltaTime;
                CascadeApply(title, t, stagger, fallDur, dropH);
                if (shadow != null) CascadeApply(shadow, t, stagger, fallDur, dropH);
                yield return null;
            }
            title.ForceMeshUpdate();
            if (shadow != null) shadow.ForceMeshUpdate();

            // 부제는 타이틀 착지 후 페이드인
            if (sub != null)
            {
                float ft = 0f;
                while (ft < 0.45f) { ft += Time.unscaledDeltaTime; var c = sub.color; c.a = Mathf.Clamp01(ft / 0.45f); sub.color = c; yield return null; }
                var cc = sub.color; cc.a = 1f; sub.color = cc;
            }
        }

        // 글자마다 위에서 떨어지며 퉁퉁 튕기는 효과 적용 (TMP 글자별 메시 조작)
        private static void CascadeApply(TMP_Text txt, float t, float stagger, float fallDur, float dropH)
        {
            txt.ForceMeshUpdate();
            var info = txt.textInfo;
            for (int i = 0; i < info.characterCount; i++)
            {
                var ch = info.characterInfo[i];
                if (!ch.isVisible) continue;
                int mi = ch.materialReferenceIndex, vi = ch.vertexIndex;
                var verts = info.meshInfo[mi].vertices;
                var cols  = info.meshInfo[mi].colors32;
                float ct = t - i * stagger;
                float yOff; byte a;
                if (ct <= 0f) { yOff = dropH; a = 0; }
                else { float k = Mathf.Clamp01(ct / fallDur); yOff = Mathf.LerpUnclamped(dropH, 0f, EaseOutBounce(k)); a = 255; }
                for (int v = 0; v < 4; v++)
                {
                    verts[vi + v] += new Vector3(0f, yOff, 0f);
                    var c = cols[vi + v]; c.a = a; cols[vi + v] = c;
                }
            }
            for (int m = 0; m < info.meshInfo.Length; m++)
            {
                var mesh = info.meshInfo[m].mesh;
                mesh.vertices = info.meshInfo[m].vertices;
                mesh.colors32 = info.meshInfo[m].colors32;
                txt.UpdateGeometry(mesh, m);
            }
        }

        private static float EaseOutBounce(float x)
        {
            const float n1 = 7.5625f, d1 = 2.75f;
            if (x < 1f / d1) return n1 * x * x;
            else if (x < 2f / d1) { x -= 1.5f / d1; return n1 * x * x + 0.75f; }
            else if (x < 2.5f / d1) { x -= 2.25f / d1; return n1 * x * x + 0.9375f; }
            else { x -= 2.625f / d1; return n1 * x * x + 0.984375f; }
        }

        // ================= 2. 채팅 입력/응답 =================

        // 메인 화면 말풍선을 모두 제거한다. 새 턴이 시작될 때 호출.
        // _chatLog는 건드리지 않으므로 "대화 기록" 데이터는 그대로 유지된다.
        private void ClearActiveBubbles()
        {
            _pendingNpcBubble = null;
            if (chatContent == null) return;
            foreach (Transform child in chatContent) Destroy(child.gameObject);
        }

        private void OnClickSend()
        {
            if (inputField == null || string.IsNullOrWhiteSpace(inputField.text)) return;

            string text = inputField.text;
            inputField.text = string.Empty;
            SetSendingState(true);

            // 이전 턴 자막 제거
            ClearActiveBubbles();

            string displayMsg = "<b><color=#D99E2E>▶ 수사관 :</color></b>  " + text;
            AppendLog(true, displayMsg);

            // 상대방(피의자) 위치에 타자기 타이핑 인디케이터 전용 라인 세팅
            _pendingNpcBubble = SpawnBubble(npcBubblePrefab, string.Empty, typed: false);
            if (_pendingNpcBubble != null) _pendingNpcBubble.StartTypingIndicator();

            StartApiTimeout();
            manager.SubmitInput(text);

            SmoothScrollToBottom();
        }

        private void StartApiTimeout()
        {
            if (_apiTimeoutRoutine != null) StopCoroutine(_apiTimeoutRoutine);
            _apiTimeoutRoutine = StartCoroutine(ApiTimeoutRoutine());
        }

        private void CancelApiTimeout()
        {
            if (_apiTimeoutRoutine == null) return;
            StopCoroutine(_apiTimeoutRoutine);
            _apiTimeoutRoutine = null;
        }

        private IEnumerator ApiTimeoutRoutine()
        {
            yield return new WaitForSecondsRealtime(ApiTimeoutSec);
            // LLM 요청 코루틴을 강제 중단하고 에러 처리
            manager.AbortCurrentLLMRequest();
            HandleError("응답 시간이 초과되었습니다 (30초). 다시 입력해 주세요.");
        }

        private void SetSendingState(bool sending)
        {
            if (sendButton != null) sendButton.interactable = !sending;
            if (inputField != null) inputField.interactable = !sending;
            if (sending) _korBridge?.HideOverlay();

            var lbl = sendButton != null ? sendButton.GetComponentInChildren<TMP_Text>() : null;
            if (lbl == null) return;
            if (sending)
            {
                if (_sendLabelRoutine != null) StopCoroutine(_sendLabelRoutine);
                _sendLabelRoutine = StartCoroutine(AnimateSendLabel(lbl));
            }
            else
            {
                if (_sendLabelRoutine != null) { StopCoroutine(_sendLabelRoutine); _sendLabelRoutine = null; }
                lbl.text = "ENTER ▶";
            }
        }

        private IEnumerator AnimateSendLabel(TMP_Text lbl)
        {
            string[] frames = { "전송 중  ·", "전송 중  · ·", "전송 중  · · ·" };
            int i = 0;
            while (true)
            {
                lbl.text = frames[i % frames.Length];
                i++;
                yield return new WaitForSecondsRealtime(0.38f);
            }
        }
        private void FocusInput()
        {
            if (inputField == null || !inputField.interactable) return;
            inputField.ActivateInputField();
            inputField.Select();
            _korBridge?.ShowOverlay();
        }

        private void HandleEvidenceUnlocked(Persuasion.EvidenceData ev)
        {
            if (evidenceContainer == null)
            {
                var found = GameObject.Find("EvidenceContainer");
                if (found != null) evidenceContainer = found.transform;
            }

            // 시스템 메시지 팝업 출력 (항상 출력)
            AppendSystemMessage($"<color=#FFE373><b>[증거 획득]</b> {ev.evidenceName} : {ev.evidenceDescription}</color>");
            AudioManager.Instance?.PlayButtonClick();

            // 화면 중앙에 팝업 효과 추가
            ShowEvidencePopup(ev.evidenceName);

            if (evidenceContainer == null) return;
            evidenceContainer.gameObject.SetActive(true);
            
            // 신규 증거물 UI 동적 생성
            GameObject evObj = new GameObject("EvidenceItem");
            evObj.transform.SetParent(evidenceContainer, false);
            
            var rt = evObj.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(180f, 60f);

            var bg = evObj.AddComponent<Image>();
            bg.color = new Color(0.85f, 0.62f, 0.18f, 0.8f); // 획득 시 눈에 띄는 색상

            var txtObj = new GameObject("Text");
            txtObj.transform.SetParent(evObj.transform, false);
            
            var txtRt = txtObj.AddComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero; txtRt.anchorMax = Vector2.one;
            txtRt.sizeDelta = Vector2.zero;
            txtRt.offsetMin = new Vector2(5, 5); txtRt.offsetMax = new Vector2(-5, -5);

            var tmp = txtObj.AddComponent<TextMeshProUGUI>();
            tmp.text = $"<size=12>EVIDENCE</size>\n<b>{ev.evidenceName}</b>";
            tmp.fontSize = 16;
            tmp.color = Color.black;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.lineSpacing = -10f;

            // 효과음 재생
            AudioManager.Instance?.PlayButtonClick();
        }

        private void AppendSystemMessage(string text)
        {
            AppendLog(false, text);
        }

        private void ShowEvidencePopup(string name)
        {
            var popup = new GameObject("EvidencePopup");
            popup.transform.SetParent(this.transform, false);
            var rt = popup.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(400f, 100f);
            rt.anchoredPosition = new Vector2(0, 150f);

            var img = popup.AddComponent<Image>();
            img.color = new Color(0.15f, 0.15f, 0.15f, 0.85f);
            var outline = popup.AddComponent<Outline>();
            outline.effectColor = new Color(0.85f, 0.62f, 0.18f, 1f);
            outline.effectDistance = new Vector2(2, -2);
            
            var txtObj = new GameObject("Text");
            txtObj.transform.SetParent(popup.transform, false);
            var txtRt = txtObj.AddComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero; txtRt.anchorMax = Vector2.one;
            txtRt.sizeDelta = Vector2.zero;
            
            var tmp = txtObj.AddComponent<TextMeshProUGUI>();
            tmp.text = $"<color=#FFE373>[새로운 증거 획득]</color>\n<b>{name}</b>";
            tmp.fontSize = 24;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            
            var cg = popup.AddComponent<CanvasGroup>();
            StartCoroutine(AnimateEvidencePopup(popup, cg, rt));
        }

        private IEnumerator AnimateEvidencePopup(GameObject popup, CanvasGroup cg, RectTransform rt)
        {
            cg.alpha = 0f;
            float t = 0;
            // Fade in and move up slightly
            while(t < 0.3f) { t += Time.unscaledDeltaTime; cg.alpha = t/0.3f; rt.anchoredPosition = new Vector2(0, 130f + 20f*(t/0.3f)); yield return null; }
            yield return new WaitForSecondsRealtime(2.5f);
            t = 0;
            // Fade out
            while(t < 0.4f) { t += Time.unscaledDeltaTime; cg.alpha = 1f - t/0.4f; yield return null; }
            Destroy(popup);
        }

        private static string TranslateEmotion(string e)
        {
            if (string.IsNullOrEmpty(e)) return "무감정";
            string lower = e.ToLower();
            if (lower.Contains("anger") || lower.Contains("angry")) return "분노";
            if (lower.Contains("disgust")) return "혐오";
            if (lower.Contains("fear")) return "공포";
            if (lower.Contains("sad")) return "슬픔";
            if (lower.Contains("joy") || lower.Contains("happy")) return "기쁨";
            if (lower.Contains("surprise")) return "놀람";
            if (lower.Contains("confus")) return "혼란";
            if (lower.Contains("panic") || lower.Contains("embarrass")) return "당황";
            if (lower.Contains("bewilder")) return "황당함";
            if (lower.Contains("calm") || lower.Contains("neutral")) return "평온함";
            if (lower.Contains("anxious") || lower.Contains("nervous")) return "불안함";
            return e; // fallback
        }

        private void HandleNPCReplied(NPCResponse response)
        {
            CancelApiTimeout();
            int maxT = manager.Data?.maxTurns ?? 20;
            bool isAI = (manager.CurrentStage?.stageId == "stage_ai_terminal" || manager.CurrentStage?.stageId == "stage_ai_core");
            
            if (emotionLabel != null) 
            {
                if (isAI)
                    emotionLabel.text = $"[ 사용 턴 : {manager.TurnCount} / {maxT} | 심리 상태 : 불가(AI) ]";
                else
                    emotionLabel.text = $"[ 사용 턴 : {manager.TurnCount} / {maxT} | 심리 상태 : {TranslateEmotion(response.emotion)} ]";
            }
            
            persuasionSlider.value = manager.Persuasion;
            if (persuasionValueText != null) persuasionValueText.text = manager.Persuasion + "%";

            var sliderRt = persuasionSlider != null ? persuasionSlider.GetComponent<RectTransform>() : null;
            if (response.persuasionDelta < 0 && sliderRt != null)
            {
                StartCoroutine(ShakeRectTransform(sliderRt, 0.45f, 7f));
                AudioManager.Instance?.PlayPersuasionDown();
            }
            else if (response.persuasionDelta > 0 && sliderRt != null)
            {
                StartCoroutine(PulseSlider(sliderRt));
                AudioManager.Instance?.PlayPersuasionUp();
            }

            var stage = manager.CurrentStage;
            string npcName = stage != null ? stage.characterName : "용의자";
            int parenIdx = npcName.IndexOf('(');
            if (parenIdx > 0) npcName = npcName.Substring(0, parenIdx).Trim();

            string historyMsg = $"<b><color=#5C9EAD>● {npcName} :</color></b>  " + response.dialogue;
            AppendLog(false, historyMsg);

            string displayDialogue = $"\"{response.dialogue.Trim()}\"";

            // 대기 인디케이터를 실제 대사로 교체 + 타자기 효과
            var bubble = _pendingNpcBubble;
            _pendingNpcBubble = null;
            if (bubble != null)
            {
                var audio = AudioManager.Instance;
                if (audio != null && playPanel != null && playPanel.activeSelf)
                    audio.PlayCharacterVoice(manager.CurrentStageIndex);
                bubble.SetTextTyped(displayDialogue, SpeedFor(response.dialogue), PinScrollToBottom,
                    () => { if (audio != null) audio.StopCharacterVoice(); });
            }
            else
            {
                SpawnBubble(npcBubblePrefab, displayDialogue, typed: false);
            }

            if (!string.IsNullOrEmpty(response.hint))
            {
                hintPanel.SetActive(true);
                hintText.text = "[ 공략 힌트 ]  " + response.hint;
            }
            else
            {
                hintPanel.SetActive(false);
            }

            if (manager.Persuasion < 100)
            {
                SetSendingState(false);
                FocusInput();
            }
        }

        private void HandleError(string error)
        {
            CancelApiTimeout();
            var audio = AudioManager.Instance;
            if (audio != null) audio.StopCharacterVoice();

            string msg = "[오류] " + error;
            AppendLog(false, msg);

            var bubble = _pendingNpcBubble;
            _pendingNpcBubble = null;
            if (bubble != null) bubble.SetText(msg);
            else SpawnBubble(npcBubblePrefab, msg, typed: false);

            SetSendingState(false);
            FocusInput();
        }

        private static string FormatActionDirective(string rawGoal)
        {
            if (string.IsNullOrEmpty(rawGoal)) return "하단 입력창에 대사를 입력해 수사를 진행하세요!";
            string goal = rawGoal.Trim();

            if (goal.EndsWith("받아내기"))
            {
                string target = goal.Substring(0, goal.Length - 4).Trim();
                if (target.EndsWith("자백")) return "하단 입력창에 질문을 던져 피의자의 자백을 받아내세요!";
                if (!string.IsNullOrEmpty(target)) return $"하단 입력창에 질문을 던져 {target} 자백을 받아내세요!";
                return "하단 입력창에 질문을 던져 자백을 받아내세요!";
            }
            if (goal.EndsWith("알아내기"))
            {
                string target = goal.Substring(0, goal.Length - 4).Trim();
                if (!string.IsNullOrEmpty(target)) return $"하단 입력창에 질문을 던져 {target}를 알아내세요!";
                return "하단 입력창에 질문을 던져 정보를 알아내세요!";
            }
            if (goal.EndsWith("파악"))
            {
                return $"하단 입력창에 질문을 던져 {goal}하세요!";
            }
            if (goal.EndsWith("증명"))
            {
                return $"하단 입력창에 대화를 건네 {goal}하세요!";
            }

            return $"하단 입력창에 대사를 입력해 {goal}를 달성하세요!";
        }

        /// <summary>스테이지 시작 시 상대(용의자/NPC)가 먼저 첫 대사를 건넨다 (sampleLineAt0 활용).</summary>
        private void ShowNpcOpeningLine()
        {
            var stage = manager != null ? manager.CurrentStage : null;
            if (stage == null) return;

            // 🎯 자연스러운 수사 작전 지침 안내 메시지 전송
            string directiveText = $"<b><color=#FFE082>[ ◆ 수사 작전 지침 ]</color> <color=#FFFFFF>{FormatActionDirective(stage.goal)}</color></b>";
            AppendLog(false, directiveText);
            SpawnBubble(npcBubblePrefab, directiveText, typed: false);

            // 상황을 드러내는 전용 첫 대사 우선, 없으면 sampleLineAt0 폴백
            string line = !string.IsNullOrEmpty(stage.openingLine) ? stage.openingLine : stage.sampleLineAt0;
            if (string.IsNullOrEmpty(line)) return;

            string npcName = stage.characterName ?? "용의자";
            int parenIdx = npcName.IndexOf('(');
            if (parenIdx > 0) npcName = npcName.Substring(0, parenIdx).Trim();

            AppendLog(false, $"<b><color=#5C9EAD>● {npcName} :</color></b>  " + line);

            var bubble = SpawnBubble(npcBubblePrefab, string.Empty, typed: false);
            if (bubble != null)
            {
                var audio = AudioManager.Instance;
                if (audio != null) audio.PlayCharacterVoice(manager.CurrentStageIndex);
                bubble.SetTextTyped(line, SpeedFor(line), PinScrollToBottom,
                    () => { if (audio != null) audio.StopCharacterVoice(); });
            }
            else
            {
                SpawnBubble(npcBubblePrefab, line, typed: false);
            }
        }

        /// <summary>대사 길이에 비례한 타자기 속도(초/글자). 길수록 조금 빠르게, 범위 제한.</summary>
        private static float SpeedFor(string text)
        {
            int len = string.IsNullOrEmpty(text) ? 1 : text.Length;
            return Mathf.Clamp(1.2f / len, 0.008f, 0.03f);
        }

        private ChatBubble SpawnBubble(GameObject prefab, string text, bool typed)
        {
            if (prefab == null || chatContent == null) return null;

            var go = Instantiate(prefab, chatContent);
            var cb = go.GetComponent<ChatBubble>();
            if (cb != null)
            {
                cb.PlayPopIn();
                if (!typed) cb.SetText(text);
            }
            Canvas.ForceUpdateCanvases();
            SmoothScrollToBottom();
            return cb;
        }

        private void AppendLog(bool isPlayer, string text)
        {
            _chatLog.Add(new ChatEntry { isPlayer = isPlayer, text = text });
        }

        private void PinScrollToBottom()
        {
            if (chatScrollRect != null) chatScrollRect.verticalNormalizedPosition = 0f;
        }

        private void SmoothScrollToBottom()
        {
            if (chatScrollRect == null || !isActiveAndEnabled) return;
            if (_scrollRoutine != null) StopCoroutine(_scrollRoutine);
            _scrollRoutine = StartCoroutine(SmoothScrollRoutine());
        }

        private IEnumerator SmoothScrollRoutine()
        {
            yield return null; // 레이아웃 갱신 대기
            Canvas.ForceUpdateCanvases();
            const float dur = 0.25f;
            float t = 0f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                float cur = chatScrollRect.verticalNormalizedPosition;
                chatScrollRect.verticalNormalizedPosition = Mathf.Lerp(cur, 0f, t / dur);
                yield return null;
            }
            chatScrollRect.verticalNormalizedPosition = 0f;
            _scrollRoutine = null;
        }

        // ================= 3. 대화 기록 (읽기 전용) =================

        private void OpenHistory()
        {
            if (historyPanel == null || historyContent == null) return;

            foreach (Transform c in historyContent) Destroy(c.gameObject);
            foreach (var e in _chatLog)
            {
                var prefab = e.isPlayer ? playerBubblePrefab : npcBubblePrefab;
                if (prefab == null) continue;
                var go = Instantiate(prefab, historyContent);
                var cb = go.GetComponent<ChatBubble>();
                if (cb != null) cb.SetText(e.text);
            }

            historyPanel.SetActive(true);
            Canvas.ForceUpdateCanvases();
            if (historyScrollRect != null) historyScrollRect.verticalNormalizedPosition = 1f; // 위(처음)부터
        }

        private void CloseHistory()
        {
            if (historyPanel == null) return;
            historyPanel.SetActive(false);
            if (historyContent != null)
                foreach (Transform c in historyContent) Destroy(c.gameObject);
        }

        // ================= 4. 스토리 미리보기 =================

        private readonly System.Collections.Generic.List<string> _currentStoryPages = new System.Collections.Generic.List<string>();
        private int _currentStoryPageIndex = 0;
        private int _scenePageCount = 0; // 장면 내레이션 페이지 수 — 이후 인덱스는 캐릭터 초상화 표시

        private void OnClickStoryPeek()
        {
            if (manager == null || storyBeatPanel == null) return;
            manager.PeekStory();            // 게임 상태(설득도/턴/대화) 리셋 없음
            ShowStoryBeat();
            ShowOnly(storyBeatPanel);
        }

        private void OnStoryBeatContinue()
        {
            // 나래이션 타이핑 중이면 먼저 전체를 즉시 표시(스킵)
            if (_storyTyping)
            {
                CompleteStoryTyping();
                return;
            }

            if (_currentStoryPageIndex < _currentStoryPages.Count - 1)
            {
                DisplayStoryPage(_currentStoryPageIndex + 1);
                return;
            }

            if (manager != null && manager.IsPeeking)
            {
                manager.ClosePeekStory();
                SetStoryContinueLabel("계속하기 ▶");
                ShowOnly(playPanel);        // RefreshStageHeader 호출 안 함 → 진행 중 대화/설득도 유지
            }
            else
            {
                manager.ContinueFromStoryBeat();
            }
        }

        private void SetStoryContinueLabel(string label)
        {
            if (storyBeatContinueButton == null) return;
            var lbl = storyBeatContinueButton.GetComponentInChildren<TMP_Text>();
            if (lbl != null) lbl.text = label;
        }

        // ================= 5. 메뉴 (소리/나가기) =================

        private void OpenMenu()
        {
            if (menuPanel == null) return;
            var am = AudioManager.Instance;
            if (am != null)
            {
                if (musicSlider != null) musicSlider.SetValueWithoutNotify(am.MusicVolume01);
                if (sfxSlider   != null) sfxSlider.SetValueWithoutNotify(am.SfxVolume01);
                if (voiceSlider != null) voiceSlider.SetValueWithoutNotify(am.VoiceVolume01);
            }
            menuPanel.SetActive(true);
        }

        private void CloseMenu()
        {
            if (menuPanel != null) menuPanel.SetActive(false);
        }

        private void GoToHome()
        {
            var audio = AudioManager.Instance;
            if (audio != null) audio.StopCharacterVoice();
            CloseMenu();
            if (manager != null) manager.GoToIntro();
        }

        private void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#elif UNITY_WEBGL
            // WebGL에서는 Application.Quit() 호출 시 브라우저 콘솔 에러가 발생하므로 경고창만 띄우거나 무시합니다.
            HandleError("웹 브라우저 탭을 닫아 게임을 종료해 주세요.");
#else
            Application.Quit();
#endif
        }

        private void OpenGuide()
        {
            if (guidePanel != null) guidePanel.SetActive(true);
        }

        private void CloseGuide()
        {
            if (guidePanel != null) guidePanel.SetActive(false);
            // 게임 진행 중에 닫으면 입력창으로 포커스 복귀
            if (playPanel != null && playPanel.activeSelf) FocusInput();
        }

        // ================= 진행 헤더 / 페이즈 =================

        private void RefreshStageHeader()
        {
            var stage = manager.CurrentStage;
            if (stage == null) return;

            chapterTitleText.text = string.Empty;
            int maxT = manager.Data?.maxTurns ?? 20;
            if (stageTitleText != null) stageTitleText.text = $"제한 턴 : {maxT}턴";
            characterNameText.text = stage.characterName;
            backgroundNarrationText.text = stage.characterPersona;          // 4: 특징만 간단히
            if (goalText != null) goalText.text = "▶  수사 목표 : " + stage.goal;
            persuasionSlider.value = manager.Persuasion;
            if (persuasionValueText != null) persuasionValueText.text = manager.Persuasion + "%";
            
            bool isAI = (stage.stageId == "stage_ai_terminal" || stage.stageId == "stage_ai_core");
            if (emotionLabel != null) 
            {
                if (isAI)
                    emotionLabel.text = $"[ 사용 턴 : 0 / {maxT} | 심리 상태 : 불가(AI) ]";
                else
                    emotionLabel.text = $"[ 사용 턴 : 0 / {maxT} | 심리 상태 : 평온함 ]";
            }
            
            hintPanel.SetActive(false);

            // 새 스테이지 시작 → 대화/기록/대기상태 초기화 및 오버레이 닫기
            _chatLog.Clear();
            _pendingNpcBubble = null;
            var audio = AudioManager.Instance;
            if (audio != null) audio.StopCharacterVoice();
            SetSendingState(false);
            if (historyPanel != null) historyPanel.SetActive(false);
            if (menuPanel != null) menuPanel.SetActive(false);

            foreach (Transform child in chatContent) Destroy(child.gameObject);
        }

        private void BuildStageButtons()
        {
            if (stageButtonContainer == null || stageButtonPrefab == null || manager.Data == null) return;

            foreach (Transform child in stageButtonContainer) Destroy(child.gameObject);

            var stages = manager.Data.stages;
            for (int i = 0; i < stages.Length; i++)
            {
                var stage = stages[i];
                var go = Instantiate(stageButtonPrefab, stageButtonContainer);
                var btn = go.GetComponent<StageButton>();
                if (btn == null) continue;

                Sprite thumb = null;
                if (portraitLibrary != null)
                {
                    var set = portraitLibrary.Find(stage.stageId);
                    if (set != null) thumb = set.baseSprite;
                }

                int index = i; // 클로저 캡처 방지
                bool unlocked = manager.IsStageUnlocked(i);
                btn.Setup(stage.chapterTitle, stage.stageTitle, thumb, unlocked, () =>
                {
                    AudioManager.Instance?.PlayButtonClick();
                    manager.StartStage(index);
                });
            }
        }

        private void HandlePhaseChanged(GamePhase phase)
        {
            switch (phase)
            {
                case GamePhase.Intro:
                    ShowOnly(introPanel);
                    break;
                case GamePhase.StageSelect:
                    BuildStageButtons();
                    ShowOnly(stageSelectPanel);
                    break;
                case GamePhase.StoryBeat:
                    if (storyBeatPanel != null)
                    {
                        SetStoryContinueLabel("계속하기 ▶");
                        ShowStoryBeat();
                        ShowOnly(storyBeatPanel);
                    }
                    else
                    {
                        // StoryBeatPanel이 씬에 없으면 스토리 비트를 건너뛰고 바로 플레이로 진행
                        manager.ContinueFromStoryBeat();
                    }
                    break;
                case GamePhase.Playing:
                    RefreshStageHeader();
                    if (evidenceContainer != null)
                    {
                        // 증거 패널 초기화 (이전 스테이지 증거 삭제)
                        foreach (Transform child in evidenceContainer)
                        {
                            Destroy(child.gameObject);
                        }
                        evidenceContainer.gameObject.SetActive(true);
                    }

                    if (stageIntroOverlay != null && stageIntroGroup != null)
                    {
                        StartCoroutine(PlayStageIntroRoutine());
                    }
                    else
                    {
                        ShowOnly(playPanel);
                        ShowNpcOpeningLine();
                        FocusInput();
                    }
                    break;
                case GamePhase.StageCleared:
                    manager.LoadPersonalBest();
                    ShowClearResult();
                    resultText.color = s_Gold;
                    // 클리어 전용 요소 표시
                    if (resultGradeText       != null) resultGradeText.gameObject.SetActive(true);
                    if (resultStatsText       != null) resultStatsText.gameObject.SetActive(true);
                    if (resultNarrationText   != null) resultNarrationText.gameObject.SetActive(false);
                    if (shareButton           != null) shareButton.gameObject.SetActive(true);
                    if (openLeaderboardButton != null) openLeaderboardButton.gameObject.SetActive(true);
                    ShowOnly(resultPanel);
                    break;
                case GamePhase.StageFailed:
                    resultText.text  = "실패했습니다. 다시 시도하세요.";
                    resultText.color = s_Danger;
                    // 클리어 전용 요소 숨김
                    if (resultGradeText       != null) resultGradeText.gameObject.SetActive(false);
                    if (resultStatsText       != null) resultStatsText.gameObject.SetActive(false);
                    if (resultPersonalBestText != null) resultPersonalBestText.gameObject.SetActive(false);
                    if (resultBestMovePanel    != null) resultBestMovePanel.gameObject.SetActive(false);
                    if (shareButton            != null) shareButton.gameObject.SetActive(false);
                    if (openLeaderboardButton  != null) openLeaderboardButton.gameObject.SetActive(false);
                    if (resultNarrationText != null)
                    {
                        resultNarrationText.gameObject.SetActive(true);
                        var failStage = manager.CurrentStage;
                        resultNarrationText.text = (failStage != null && !string.IsNullOrEmpty(failStage.failureDialogue))
                            ? failStage.failureDialogue
                            : string.Empty;
                    }
                    nextButton.gameObject.SetActive(false);
                    retryButton.gameObject.SetActive(true);
                    ShowOnly(resultPanel);
                    if (_clearCoroutine != null) StopCoroutine(_clearCoroutine);
                    _clearCoroutine = StartCoroutine(FailImpactRoutine());
                    break;
                case GamePhase.GameCompleted:
                    resultText.text  = "모든 상황 클리어!";
                    resultText.color = s_Gold;
                    if (resultGradeText        != null) resultGradeText.gameObject.SetActive(true);
                    if (resultStatsText        != null) resultStatsText.gameObject.SetActive(true);
                    if (resultPersonalBestText != null) resultPersonalBestText.gameObject.SetActive(false);
                    if (resultNarrationText    != null) 
                    {
                        resultNarrationText.gameObject.SetActive(true);
                        resultNarrationText.text = "\n<size=120%><b>[ 제작진 ]</b></size>\n\n<b>기획자</b> : 이준\n<b>개발자</b> : 이정한\n\n플레이해주셔서 감사합니다!";
                        resultNarrationText.alignment = TextAlignmentOptions.Center;
                    }
                    if (resultStatsText != null)
                        resultStatsText.text = $"{manager.TurnCount}턴 완료  ·  전 상황 클리어";
                    if (resultGradeText != null) { resultGradeText.text = "★"; resultGradeText.color = s_Gold; }
                    if (resultBestMovePanel != null) resultBestMovePanel.SetActive(false);
                    if (shareButton != null) shareButton.gameObject.SetActive(true);
                    if (openLeaderboardButton != null) openLeaderboardButton.gameObject.SetActive(false);
                    nextButton.gameObject.SetActive(false);
                    retryButton.gameObject.SetActive(false);
                    ShowOnly(resultPanel);
                    break;
            }
        }

        private IEnumerator PlayStageIntroRoutine()
        {
            var stage = manager != null ? manager.CurrentStage : null;
            if (stage != null)
            {
                if (introStageTitleText != null) introStageTitleText.text = stage.stageTitle;
                if (introStageSubTitleText != null) introStageSubTitleText.text = stage.characterName;
                if (introGoalText != null) introGoalText.text = "▶  수사 목표 : " + stage.goal;
            }

            ShowOnly(playPanel);
            if (stageIntroOverlay != null) stageIntroOverlay.SetActive(true);

            var audio = AudioManager.Instance;
            if (audio != null) audio.PlayButtonClick();

            stageIntroGroup.alpha = 0f;
            if (introGoalCardRt != null) introGoalCardRt.localScale = Vector3.one * 0.85f;

            float t = 0f;
            const float fadeInDur = 0.45f;
            while (t < fadeInDur)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / fadeInDur);
                float e = 1f - Mathf.Pow(1f - k, 3f);
                stageIntroGroup.alpha = k;
                if (introGoalCardRt != null) introGoalCardRt.localScale = Vector3.Lerp(Vector3.one * 0.85f, Vector3.one, e);
                yield return null;
            }
            stageIntroGroup.alpha = 1f;
            if (introGoalCardRt != null) introGoalCardRt.localScale = Vector3.one;

            float holdTimer = 0f;
            const float holdDur = 1.8f;
            while (holdTimer < holdDur)
            {
                holdTimer += Time.unscaledDeltaTime;
                bool skip = false;
#if ENABLE_INPUT_SYSTEM
                if (Pointer.current != null && Pointer.current.press.wasPressedThisFrame) skip = true;
                if (Keyboard.current != null && (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame)) skip = true;
#else
                if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)) skip = true;
#endif
                if (skip) break;
                yield return null;
            }

            t = 0f;
            const float fadeOutDur = 0.35f;
            while (t < fadeOutDur)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / fadeOutDur);
                stageIntroGroup.alpha = 1f - k;
                yield return null;
            }

            if (stageIntroOverlay != null) stageIntroOverlay.SetActive(false);

            // 플레이 화면에서 이탈했으면 코루틴 종료 (음성 루프 방지)
            if (playPanel == null || !playPanel.activeSelf) yield break;

            // 상대(용의자)가 먼저 상황을 열며 첫 대사를 건넨다 → 이후 플레이어가 대응
            ShowNpcOpeningLine();

            // 게임 시작 시 수사 지침서 팝업 자동 표시 금지 (입력 포커스로 바로 이동)
            _hasShownGuide = true;
            FocusInput();
        }

        /// <summary>
        /// 인트로컷(스테이지 시작) 또는 전환컷(스테이지 사이) 화면을 구성한다.
        /// 스토리 컷이 여러 개(전환 컷 + 배경 컷 또는 문단 2개 이상)인 경우 페이지 순차 넘김을 지원한다.
        /// </summary>
        private void ShowStoryBeat()
        {
            var stage = manager.CurrentStage;
            if (stage == null) return;

            // 1-1(뺑소니) 도입 시작에 차 브레이크 소리
            if (!manager.IsPeeking && manager.CurrentBeatKind == StoryBeatKind.StageIntro
                && (stage.stageId == "stage_hitandrun" || manager.CurrentStageIndex == 0))
            {
                AudioManager.Instance?.PlayCarBrake();
            }

            _currentStoryPages.Clear();
            _currentStoryPageIndex = 0;
            _scenePageCount = 0;

            // 전환컷(StageTransition): 직전 스테이지의 전환 내레이션만 표시.
            // 배경 내레이션은 이어지는 도입컷(StageIntro)에서 표시하므로 여기서 중복 추가하지 않는다.
            if (manager.CurrentBeatKind == StoryBeatKind.StageTransition)
            {
                // 전환컷이 2장(예: 살해 장면 추가)인 경우, 내레이션을 줄바꿈 기준으로 나눠 컷마다 한 페이지씩.
                int cutCount = (transitionArtLibrary != null)
                    ? transitionArtLibrary.GetTransitionPageCount(manager.PendingTransitionFromStageId, stage.stageId)
                    : 1;
                string narration = manager.PendingTransitionNarration;
                if (cutCount >= 2 && !string.IsNullOrEmpty(narration))
                {
                    string[] sentences = narration.Split(new[] { "\n", "\r\n" }, System.StringSplitOptions.RemoveEmptyEntries);
                    if (sentences.Length >= 2)
                    {
                        // 앞 문장 → 1번째 컷, 나머지 → 2번째 컷
                        _currentStoryPages.Add(sentences[0].Trim());
                        _currentStoryPages.Add(string.Join("\n", sentences, 1, sentences.Length - 1).Trim());
                    }
                    else
                    {
                        _currentStoryPages.Add(narration.Trim());
                        _currentStoryPages.Add(narration.Trim());
                    }
                }
                else if (!string.IsNullOrEmpty(narration))
                {
                    _currentStoryPages.Add(narration.Trim());
                }
            }
            else
            {
                // 도입컷(StageIntro): 배경 내레이션을 문장 단위로 한 장씩 표시
                foreach (var sent in SplitIntoSentences(stage.backgroundNarration))
                    _currentStoryPages.Add(sent);
            }

            // 도입컷: 장면 페이지 이후 캐릭터 초상화 페이지 추가 (문장 단위)
            if (manager.CurrentBeatKind != StoryBeatKind.StageTransition)
            {
                _scenePageCount = _currentStoryPages.Count;
                foreach (var sent in SplitIntoSentences(stage.characterNarration))
                    _currentStoryPages.Add(sent);
            }

            // 표시할 페이지가 하나도 없으면(전환/배경 내레이션 모두 비어있음) 다음 단계로 즉시 진행.
            if (_currentStoryPages.Count == 0)
            {
                manager.ContinueFromStoryBeat();
                return;
            }

            DisplayStoryPage(0);
        }

        private void DisplayStoryPage(int index)
        {
            if (_currentStoryPages.Count == 0) return;
            _currentStoryPageIndex = UnityEngine.Mathf.Clamp(index, 0, _currentStoryPages.Count - 1);

            var stage = manager.CurrentStage;
            Sprite sprite = null;
            if (transitionArtLibrary != null && stage != null)
            {
                bool isTransitionPage = manager.CurrentBeatKind == StoryBeatKind.StageTransition;
                if (isTransitionPage)
                {
                    sprite = transitionArtLibrary.GetTransition(manager.PendingTransitionFromStageId, stage.stageId, _currentStoryPageIndex);
                }
                else if (_scenePageCount > 0 && _currentStoryPageIndex >= _scenePageCount)
                {
                    // 캐릭터 소개 페이지 — 초상화 사용
                    sprite = transitionArtLibrary.GetIntroCharacter(stage.stageId);
                }
                else
                {
                    sprite = transitionArtLibrary.GetIntro(stage.stageId);
                }

                // 도입 일러스트가 없는 스테이지(예: 연쇄살인마)는 캐릭터 초상화로 폴백.
                if (sprite == null && !isTransitionPage && portraitLibrary != null)
                {
                    var set = portraitLibrary.Find(stage.stageId);
                    if (set != null) sprite = set.baseSprite;
                }
            }

            if (storyBeatImage != null)
            {
                storyBeatImage.sprite = sprite;
                storyBeatImage.enabled = (sprite != null);
                // 밝은 일러스트 위에서도 흰 글자가 보이도록 이미지를 어둡게 틴트.
                storyBeatImage.color = new Color(0.62f, 0.62f, 0.64f, 1f);
                if (sprite != null)
                {
                    var fitter = storyBeatImage.GetComponent<AspectRatioFitter>();
                    if (fitter != null && sprite.rect.height > 0)
                    {
                        fitter.aspectRatio = sprite.rect.width / sprite.rect.height;
                        fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                    }
                }
            }

            if (storyBeatText != null)
            {
                StartStoryTyping(_currentStoryPages[_currentStoryPageIndex]);
            }

            // 헤더 레이블 — 페이지 종류에 따라 전환
            if (storyHeaderLabel != null)
            {
                bool isCharPage  = _scenePageCount > 0 && _currentStoryPageIndex >= _scenePageCount;
                bool isTransPage = manager.CurrentBeatKind == StoryBeatKind.StageTransition;
                storyHeaderLabel.text = isTransPage ? "◆  사건 경과"
                                      : isCharPage  ? "◆  용의자 프로파일"
                                                    : "◆  사건 현장";
            }
            // 페이지 인디케이터
            if (storyPageIndicator != null)
                storyPageIndicator.text = $"{_currentStoryPageIndex + 1} / {_currentStoryPages.Count}";
            // 케이스 번호
            if (storyCaseLabel != null && stage != null)
                storyCaseLabel.text = $"CASE #{stage.stageOrder + 1:D3}";

            bool isLastPage = (_currentStoryPageIndex == _currentStoryPages.Count - 1);
            if (manager.IsPeeking)
            {
                SetStoryContinueLabel(isLastPage ? "닫기" : "다음 ▶");
            }
            else if (manager.CurrentBeatKind == StoryBeatKind.StageTransition)
            {
                SetStoryContinueLabel("다음 ▶");
            }
            else
            {
                SetStoryContinueLabel(isLastPage ? "수사 시작 ▶" : "다음 ▶");
            }
        }

        // ================= 스토리 나래이션 타자기 효과 =================

        /// <summary>나래이션을 한 글자씩 출력(타자기). 출력 동안 키보드 타이핑 소리 재생.</summary>
        private void StartStoryTyping(string full)
        {
            if (storyBeatText == null) return;
            if (_storyTypeRoutine != null) StopCoroutine(_storyTypeRoutine);
            _storyTypeRoutine = StartCoroutine(StoryTypeRoutine(full ?? string.Empty));
        }

        private IEnumerator StoryTypeRoutine(string full)
        {
            _storyTyping = true;
            storyBeatText.text = full;
            storyBeatText.ForceMeshUpdate();
            int total = storyBeatText.textInfo.characterCount;
            storyBeatText.maxVisibleCharacters = 0;

            var audio = AudioManager.Instance;
            if (audio != null) audio.StartTypingSound();

            int shown = 0;
            while (shown <= total)
            {
                storyBeatText.maxVisibleCharacters = shown;
                shown++;
                yield return new WaitForSecondsRealtime(0.035f);
            }

            storyBeatText.maxVisibleCharacters = int.MaxValue;
            if (audio != null) audio.StopTypingSound();
            _storyTyping = false;
            _storyTypeRoutine = null;
        }

        /// <summary>긴 문단을 문장 단위로 maxChars 이하 페이지로 분할.</summary>
        private static System.Collections.Generic.List<string> SplitIntoPages(string text, int maxChars)
        {
            var pages = new System.Collections.Generic.List<string>();
            var sentences = System.Text.RegularExpressions.Regex.Split(text.Trim(), @"(?<=[.!?…。])\s+");
            var buf = new System.Text.StringBuilder();
            foreach (var s in sentences)
            {
                if (buf.Length > 0 && buf.Length + s.Length + 1 > maxChars)
                {
                    pages.Add(buf.ToString().Trim());
                    buf.Clear();
                }
                if (buf.Length > 0) buf.Append(' ');
                buf.Append(s);
            }
            if (buf.Length > 0) pages.Add(buf.ToString().Trim());
            return pages;
        }

        /// <summary>
        /// 내레이션 텍스트를 문장 단위로 분리. 마침표/느낌표/물음표 뒤 공백이나 줄바꿈으로 컷.
        /// </summary>
        private static System.Collections.Generic.IEnumerable<string> SplitIntoSentences(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) yield break;
            var sentences = System.Text.RegularExpressions.Regex.Split(
                text.Trim(), @"(?<=[.!?…。])\s+");
            foreach (var s in sentences)
            {
                var t = s.Trim();
                if (!string.IsNullOrWhiteSpace(t)) yield return t;
            }
        }

        /// <summary>타이핑 중 클릭 시 전체 즉시 표시(스킵).</summary>
        private void CompleteStoryTyping()
        {
            if (_storyTypeRoutine != null) { StopCoroutine(_storyTypeRoutine); _storyTypeRoutine = null; }
            if (storyBeatText != null) storyBeatText.maxVisibleCharacters = int.MaxValue;
            var audio = AudioManager.Instance;
            if (audio != null) audio.StopTypingSound();
            _storyTyping = false;
        }

        /// <summary>
        /// 클리어 결과 화면을 상황(챕터) 맥락에 맞게 구성한다.
        /// 같은 상황의 다음 단계면 "다음 단계", 다음 스테이지가 새 상황이면 "다음 상황으로",
        /// 마지막 스테이지면 "결말 보기"로 안내하고 전환 내레이션을 함께 보여준다.
        /// </summary>
        private void ShowClearResult()
        {
            var audio = AudioManager.Instance;
            if (audio != null) audio.StopCharacterVoice();

            var data = manager.Data;
            int idx = manager.CurrentStageIndex;
            var cur = manager.CurrentStage;

            bool hasNext = data != null && data.stages != null && idx + 1 < data.stages.Length;
            bool nextIsNewChapter = hasNext && cur != null && data.stages[idx + 1].chapterId != cur.chapterId;

            resultText.text = nextIsNewChapter ? "상황 클리어!" : "설득 성공!";

            // 등급
            string grade = manager.CalculateGrade();
            if (resultGradeText != null)
            {
                resultGradeText.text  = grade;
                resultGradeText.color = GradeColor(grade);
                resultGradeText.transform.localScale = Vector3.one * 0.5f;
            }

            // 통계
            if (resultStatsText != null)
                resultStatsText.text = $"{manager.TurnCount}턴 완료  ·  설득도 {manager.Persuasion}%";

            // 개인 최고 기록
            if (resultPersonalBestText != null)
            {
                string pb = manager.PersonalBestGrade;
                int    pt = manager.PersonalBestTurns;
                if (!string.IsNullOrEmpty(pb) && pt > 0)
                    resultPersonalBestText.text = $"개인 최고  :  {pb}등급  /  {pt}턴";
                else
                    resultPersonalBestText.text = string.Empty;
                resultPersonalBestText.gameObject.SetActive(true);
            }

            // 베스트 발화
            bool hasBest = !string.IsNullOrEmpty(manager.BestPlayerText) && manager.BestPersuasionDelta > 0;
            if (resultBestMovePanel != null)
            {
                resultBestMovePanel.SetActive(hasBest);
                if (hasBest && resultBestMoveText != null)
                    resultBestMoveText.text = $"\"{manager.BestPlayerText}\"  (+{manager.BestPersuasionDelta})";
            }

            var nextLabel = nextButton.GetComponentInChildren<TMP_Text>();
            if (nextLabel != null)
                nextLabel.text = !hasNext ? "결말 보기 ▶" : (nextIsNewChapter ? "다음 상황으로 ▶" : "다음 단계 ▶");

            nextButton.gameObject.SetActive(true);
            retryButton.gameObject.SetActive(false);

            AudioManager.Instance?.PlayClearStinger();

            if (_clearCoroutine != null) StopCoroutine(_clearCoroutine);
            _clearCoroutine = StartCoroutine(ClearCelebrationRoutine());
        }

        private static Color GradeColor(string g) => g switch
        {
            "S" => new Color(1.00f, 0.88f, 0.25f, 1f),
            "A" => new Color(0.70f, 0.90f, 1.00f, 1f),
            "B" => new Color(0.85f, 0.62f, 0.35f, 1f),
            _   => new Color(0.75f, 0.75f, 0.75f, 1f),
        };

        private IEnumerator ClearCelebrationRoutine()
        {
            // 1. 화면 플래시
            if (clearFlashOverlay != null)
            {
                clearFlashOverlay.gameObject.SetActive(true);
                float t = 0f;
                while (t < 0.12f) { t += Time.unscaledDeltaTime; clearFlashOverlay.color = new Color(1f, 1f, 1f, Mathf.Lerp(0f, 0.80f, t / 0.12f)); yield return null; }
                t = 0f;
                while (t < 0.30f) { t += Time.unscaledDeltaTime; clearFlashOverlay.color = new Color(1f, 1f, 1f, Mathf.Lerp(0.80f, 0f, t / 0.30f)); yield return null; }
                clearFlashOverlay.gameObject.SetActive(false);
            }

            // 2. 등급 팝인 (overshoot)
            if (resultGradeText != null)
            {
                var rt = resultGradeText.rectTransform;
                float t = 0f, dur = 0.45f;
                while (t < dur)
                {
                    t += Time.unscaledDeltaTime;
                    float k = Mathf.Clamp01(t / dur);
                    float overshoot = 1f + Mathf.Sin(k * Mathf.PI) * 0.18f;
                    float scale = Mathf.Lerp(0.5f, 1f, k) * overshoot;
                    rt.localScale = new Vector3(scale, scale, 1f);
                    yield return null;
                }
                rt.localScale = Vector3.one;
            }

            // 3. 베스트 발화 카드 페이드인
            if (resultBestMovePanel != null && resultBestMovePanel.activeSelf)
            {
                var cg = resultBestMovePanel.GetComponent<CanvasGroup>();
                if (cg == null) cg = resultBestMovePanel.AddComponent<CanvasGroup>();
                cg.alpha = 0f;
                float t = 0f;
                while (t < 0.4f) { t += Time.unscaledDeltaTime; cg.alpha = t / 0.4f; yield return null; }
                cg.alpha = 1f;
            }
            _clearCoroutine = null;
        }

        /// <summary>
        /// 심문 실패 순간의 임팩트 연출: 붉은 플래시 + 결과 패널 흔들림 + 실패 스팅어.
        /// 클리어 연출(ClearCelebrationRoutine)과 대칭되는 부정적 버전.
        /// </summary>
        private IEnumerator FailImpactRoutine()
        {
            AudioManager.Instance?.PlayFailStinger();

            // 1. 붉은 화면 플래시 (클리어의 흰색 플래시와 대비되는 톤)
            if (clearFlashOverlay != null)
            {
                clearFlashOverlay.gameObject.SetActive(true);
                var flashColor = new Color(0.55f, 0.06f, 0.06f, 1f);
                float t = 0f;
                while (t < 0.10f) { t += Time.unscaledDeltaTime; clearFlashOverlay.color = new Color(flashColor.r, flashColor.g, flashColor.b, Mathf.Lerp(0f, 0.55f, t / 0.10f)); yield return null; }
                t = 0f;
                while (t < 0.35f) { t += Time.unscaledDeltaTime; clearFlashOverlay.color = new Color(flashColor.r, flashColor.g, flashColor.b, Mathf.Lerp(0.55f, 0f, t / 0.35f)); yield return null; }
                clearFlashOverlay.gameObject.SetActive(false);
            }

            // 2. 결과 패널 전체가 둔탁하게 흔들림 (클리어의 경쾌한 팝인과 대비)
            var panelRt = resultPanel != null ? resultPanel.GetComponent<RectTransform>() : null;
            if (panelRt != null)
                yield return StartCoroutine(ShakeRectTransform(panelRt, 0.35f, 10f));

            _clearCoroutine = null;
        }

        private IEnumerator ShakeRectTransform(RectTransform rt, float dur, float mag)
        {
            if (rt == null) yield break;
            Vector3 orig = rt.localPosition;
            float t = 0f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                float strength = 1f - (t / dur);
                rt.localPosition = orig + new Vector3(UnityEngine.Random.Range(-mag, mag) * strength, 0f, 0f);
                yield return null;
            }
            rt.localPosition = orig;
        }

        private IEnumerator PulseSlider(RectTransform rt)
        {
            if (rt == null) yield break;
            Vector3 orig = rt.localScale;
            float t = 0f;
            while (t < 0.30f)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Sin(t / 0.30f * Mathf.PI);
                float s = 1f + k * 0.06f;
                rt.localScale = new Vector3(s, orig.y * (1f + k * 0.12f), 1f);
                yield return null;
            }
            rt.localScale = orig;
        }

        // ================= 6. 공유 / 개인정보 =================

        private void OnClickShare()
        {
            var stage = manager?.CurrentStage;
            if (stage == null) return;
            string grade = manager.CalculateGrade();
            int    turns = manager.TurnCount;
            string text  = $"[설득의 기술] {stage.stageTitle} {grade}등급 클리어! ({turns}턴 완료)\n#설득게임 #심문게임";
            // 현재 페이지 URL을 우선 사용 (호스팅 도메인 무관하게 동작). 없으면 제목만 공유.
            string url   = Application.absoluteURL;
            if (string.IsNullOrEmpty(url)) url = "";
            ShareManager.Share("설득의 기술", text, url);
            if (shareButton != null) StartCoroutine(ShareFeedback(shareButton.GetComponentInChildren<TMP_Text>()));
        }

        private IEnumerator ShareFeedback(TMP_Text lbl)
        {
            if (lbl == null) yield break;
            string orig = lbl.text;
            lbl.text = "공유 완료!";
            yield return new WaitForSecondsRealtime(2.5f);
            lbl.text = orig;
        }

        private void OnClickPrivacy()
        {
            // 현재 페이지와 같은 디렉터리 기준 상대 경로 (하위 경로 배포에서도 동작)
            // Application.OpenURL("privacy.html");
            if (manager != null && manager.CurrentPhase == GamePhase.Playing)
            {
                AppendSystemMessage("<color=#FFCD5A>개인정보처리방침은 정식 배포 시 활성화될 예정입니다.</color>");
            }
            else
            {
                Debug.Log("개인정보처리방침 링크 클릭됨 (현재 준비 중)");
            }
        }

        // ================= 7. 글로벌 리더보드 =================

        private void OpenLeaderboard()
        {
            if (leaderboardPanel == null) return;
            var stage = manager != null ? manager.CurrentStage : null;

            if (leaderboardTitleText != null)
                leaderboardTitleText.text = "★  " + (stage != null ? stage.stageTitle : "") + "  랭킹";
            if (leaderboardMyRecordText != null)
                leaderboardMyRecordText.text = $"이번 기록 :  <color=#FFCD5A>{manager.CalculateGrade()}</color>등급  ·  {manager.TurnCount}턴";
            if (leaderboardNameInput != null)
                leaderboardNameInput.text = SaveManager.Get("player_name", "");
            if (leaderboardListText != null)
                leaderboardListText.text = "불러오는 중…";

            leaderboardPanel.SetActive(true);
            RefreshLeaderboard();
        }

        private void RefreshLeaderboard()
        {
            var stage = manager != null ? manager.CurrentStage : null;
            if (leaderboard == null || stage == null)
            {
                if (leaderboardListText != null) leaderboardListText.text = "랭킹을 불러올 수 없습니다.";
                return;
            }
            leaderboard.FetchTop(stage.stageId, RenderLeaderboard,
                err => { if (leaderboardListText != null) leaderboardListText.text = "랭킹을 불러오지 못했습니다.\n<size=70%>(" + err + ")</size>"; });
        }

        private void RenderLeaderboard(List<LbEntry> entries)
        {
            if (leaderboardListText == null) return;
            if (entries == null || entries.Count == 0)
            {
                leaderboardListText.text = "아직 등록된 기록이 없습니다.\n첫 번째 주인공이 되어보세요!";
                return;
            }

            var sb = new StringBuilder();
            int rank = 1;
            foreach (var e in entries)
            {
                string rankTag = rank == 1 ? "<color=#FFD54A><b> 1위</b></color>"
                               : rank == 2 ? "<color=#CFD8DC><b> 2위</b></color>"
                               : rank == 3 ? "<color=#D9A066><b> 3위</b></color>"
                               : $"<color=#7A7D88>{rank,2}위</color>";
                string nm = SafeName(e.name);
                sb.AppendLine($"{rankTag}   <b>{nm}</b>    <color=#FFCD5A>{e.grade}</color>등급 · {e.turns}턴");
                rank++;
            }
            leaderboardListText.text = sb.ToString();
        }

        private static string SafeName(string s)
        {
            if (string.IsNullOrEmpty(s)) return "익명";
            return s.Replace("<", "").Replace(">", "");
        }

        private void SubmitScore()
        {
            var stage = manager != null ? manager.CurrentStage : null;
            if (leaderboard == null || stage == null) return;

            string name = leaderboardNameInput != null ? leaderboardNameInput.text.Trim() : "";

            // WebGL: 네이티브 프롬프트로 닉네임 입력 (한글 IME 문제 회피)
            string prompted = LeaderboardManager.PromptName("리더보드에 표시할 닉네임을 입력하세요 (최대 12자)",
                                                            string.IsNullOrEmpty(name) ? SaveManager.Get("player_name", "") : name);
            if (prompted != null)
            {
                if (string.IsNullOrWhiteSpace(prompted)) return; // 취소/공백 → 등록 안 함
                name = prompted.Trim();
                if (leaderboardNameInput != null) leaderboardNameInput.text = name;
            }

            if (string.IsNullOrEmpty(name)) name = "익명";
            if (name.Length > 12) name = name.Substring(0, 12);
            SaveManager.Set("player_name", name);

            var lbl = leaderboardSubmitButton != null ? leaderboardSubmitButton.GetComponentInChildren<TMP_Text>() : null;
            if (leaderboardSubmitButton != null) leaderboardSubmitButton.interactable = false;
            if (lbl != null) lbl.text = "등록 중…";

            leaderboard.Submit(name, stage.stageId, manager.CalculateGrade(), manager.TurnCount,
                entries =>
                {
                    RenderLeaderboard(entries);
                    if (lbl != null) lbl.text = "등록 완료!";
                    StartCoroutine(ReenableSubmit(lbl));
                },
                err =>
                {
                    if (lbl != null) lbl.text = "등록";
                    if (leaderboardSubmitButton != null) leaderboardSubmitButton.interactable = true;
                    if (leaderboardListText != null) leaderboardListText.text = "등록 실패: " + err;
                });
        }

        private IEnumerator ReenableSubmit(TMP_Text lbl)
        {
            yield return new WaitForSecondsRealtime(2f);
            if (lbl != null) lbl.text = "내 기록 등록";
            if (leaderboardSubmitButton != null) leaderboardSubmitButton.interactable = true;
        }

        private void CloseLeaderboard()
        {
            if (leaderboardPanel != null) leaderboardPanel.SetActive(false);
        }

        private void ShowOnly(GameObject panel)
        {
            if (panel == null) return;
            var audio = AudioManager.Instance;
            if (audio != null) audio.StopCharacterVoice();

            if (panel != playPanel) _korBridge?.HideOverlay();

            // 스토리 화면을 벗어나면 나래이션 타이핑/소리 정리
            if (panel != storyBeatPanel && _storyTyping) CompleteStoryTyping();

            introPanel.SetActive(panel == introPanel);
            if (stageSelectPanel != null) stageSelectPanel.SetActive(panel == stageSelectPanel);
            if (storyBeatPanel != null) storyBeatPanel.SetActive(panel == storyBeatPanel);
            playPanel.SetActive(panel == playPanel);
            resultPanel.SetActive(panel == resultPanel);
            if (traitPanel != null) traitPanel.SetActive(false);
            if (guidePanel != null) guidePanel.SetActive(false);
            if (leaderboardPanel != null) leaderboardPanel.SetActive(false);

            if (panel == introPanel) PlayIntroTitleAnim();
        }
    }
}
