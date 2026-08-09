using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Persuasion.AI;

namespace Persuasion.Core
{
    public enum GamePhase { Intro, StageSelect, StoryBeat, Playing, StageCleared, StageFailed, GameCompleted }

    /// <summary>StoryBeat 단계에서 어떤 컷을 보여줄지 구분.</summary>
    public enum StoryBeatKind { StageIntro, StageTransition }

    /// <summary>
    /// 게임 전체 진행(설득도, 턴, 현재 스테이지 인덱스)을 관리하는 매니저.
    /// 씬 전환에도 살아남도록 DontDestroyOnLoad로 싱글턴 유지.
    /// UI/캐릭터 컴포넌트는 이 클래스의 이벤트만 구독하면 됨.
    /// </summary>
    public class PersuasionManager : MonoBehaviour
    {
        public static PersuasionManager Instance { get; private set; }

        [SerializeField] private GameData gameData;
        [SerializeField] private LLMClient llmClient;

        public int Persuasion { get; private set; }
        public int TurnCount { get; private set; }
        public int CurrentStageIndex { get; private set; }
        public GamePhase CurrentPhase { get; private set; } = GamePhase.Intro;

        public event Action<NPCResponse> OnNPCReplied;
        public event Action<string> OnError;
        public event Action<GamePhase> OnPhaseChanged;
        public event Action<EvidenceData> OnEvidenceUnlocked;

        private int zeroStreak;
        private int negativeStreak;
        private readonly List<ChatTurn> history = new List<ChatTurn>();
        private readonly HashSet<string> unlockedEvidenceIds = new HashSet<string>();

        // 반복 메시지 감지
        private string _lastPlayerText;
        private int _repeatStreak;

        // 결과 화면용 통계
        private string _pendingPlayerText;
        public string BestPlayerText { get; private set; }
        public int BestPersuasionDelta { get; private set; }

        // ── 개인 최고 기록 (현재 클리어 직후 로딩)
        public string PersonalBestGrade { get; private set; }
        public int    PersonalBestTurns { get; private set; } = -1;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // 인스펙터 배선이 누락된 경우 Resources에서 자동 로드 (안전망)
            if (gameData == null)
            {
                gameData = Resources.Load<GameData>("GameData");
                if (gameData == null)
                    Debug.LogError("[PersuasionManager] GameData가 배선되지 않았고 Resources/GameData 도 없습니다.");
            }

            // localStorage에서 진행도 복원
            HighestUnlockedIndex = SaveManager.GetInt("pm_highest_idx", 0);
        }

        public StageData CurrentStage =>
            (gameData != null && gameData.stages != null && CurrentStageIndex >= 0 && CurrentStageIndex < gameData.stages.Length)
                ? gameData.stages[CurrentStageIndex]
                : null;

        /// <summary>스테이지 선택 화면 등 UI가 콘텐츠를 읽을 수 있도록 노출.</summary>
        public GameData Data => gameData;

        public int StageCount => (gameData != null && gameData.stages != null) ? gameData.stages.Length : 0;
        
        public int CurrentZeroStreak => zeroStreak;
        public int CurrentNegativeStreak => negativeStreak;

        [SerializeField] private bool testModeUnlockAll = true;

        /// <summary>클리어로 해금된 최고 스테이지 인덱스. 시작 시 0(1단계)만 해금.</summary>
        public int HighestUnlockedIndex { get; private set; } = 0;

        public bool IsStageUnlocked(int index) => true;

        public StoryBeatKind CurrentBeatKind { get; private set; } = StoryBeatKind.StageIntro;
        /// <summary>StageTransition일 때 직전 스테이지의 stageId (전환컷/전환 내레이션 조회용).</summary>
        public string PendingTransitionFromStageId { get; private set; }
        /// <summary>StageTransition일 때 직전 스테이지의 transitionNarration.</summary>
        public string PendingTransitionNarration { get; private set; }

        public void StartGame()
        {
            CurrentStageIndex = 0;
            BeginStageIntro();
        }

        // ── 등급 비교 헬퍼 ──────────────────────────────────────────
        static int GradeRank(string g) => g switch { "S" => 4, "A" => 3, "B" => 2, _ => 1 };

        static bool IsBetter(string newG, int newT, string oldG, int oldT)
        {
            int nr = GradeRank(newG), or = GradeRank(oldG);
            if (nr != or) return nr > or;
            return newT < oldT;
        }

        private void SaveStageBest()
        {
            var stage = CurrentStage;
            if (stage == null) return;
            string prefix  = "pm_best_" + stage.stageId;
            string grade   = CalculateGrade();
            int    turns   = TurnCount;
            string prevGrade = SaveManager.Get(prefix + "_grade", "");
            int    prevTurns = SaveManager.GetInt(prefix + "_turns", int.MaxValue);

            if (string.IsNullOrEmpty(prevGrade) || IsBetter(grade, turns, prevGrade, prevTurns))
            {
                SaveManager.Set(prefix + "_grade", grade);
                SaveManager.SetInt(prefix + "_turns", turns);
                prevGrade = grade;
                prevTurns = turns;
            }

            PersonalBestGrade = prevGrade;
            PersonalBestTurns = prevTurns == int.MaxValue ? -1 : prevTurns;
        }

        public void LoadPersonalBest()
        {
            var stage = CurrentStage;
            if (stage == null) { PersonalBestGrade = ""; PersonalBestTurns = -1; return; }
            string prefix = "pm_best_" + stage.stageId;
            PersonalBestGrade = SaveManager.Get(prefix + "_grade", "");
            PersonalBestTurns = SaveManager.GetInt(prefix + "_turns", -1);
        }

        /// <summary>인트로 화면으로 복귀.</summary>
        public void GoToIntro()
        {
            SetPhase(GamePhase.Intro);
        }

        /// <summary>인트로 → 스테이지 선택 화면으로 전환.</summary>
        public void GoToStageSelect()
        {
            SetPhase(GamePhase.StageSelect);
        }

        /// <summary>특정 스테이지를 직접 시작 (스테이지 선택 화면에서 호출).</summary>
        public void StartStage(int index)
        {
            if (gameData == null || gameData.stages == null || index < 0 || index >= gameData.stages.Length)
                return;
            CurrentStageIndex = index;
            BeginStageIntro();
        }

        /// <summary>스테이지 인트로컷(StoryBeat) 화면을 띄운다. 실제 플레이는 ContinueFromStoryBeat()에서 시작.</summary>
        private void BeginStageIntro()
        {
            CurrentBeatKind = StoryBeatKind.StageIntro;
            SetPhase(GamePhase.StoryBeat);
        }

        /// <summary>인트로컷/전환컷 화면에서 "계속하기"를 눌렀을 때 호출.</summary>
        public void ContinueFromStoryBeat()
        {
            if (CurrentBeatKind == StoryBeatKind.StageTransition)
            {
                // 전환컷 다음엔 새 스테이지의 인트로컷을 보여준다 (같은 StoryBeat 단계, 내용만 갱신).
                CurrentBeatKind = StoryBeatKind.StageIntro;
                OnPhaseChanged?.Invoke(GamePhase.StoryBeat);
            }
            else
            {
                ResetStageState();
                SetPhase(GamePhase.Playing);
            }
        }

        /// <summary>진행 중(Playing) 상태를 유지한 채 현재 스테이지의 인트로컷/스토리를 "미리보기"로 볼 때 true.</summary>
        public bool IsPeeking { get; private set; }

        /// <summary>
        /// Play 화면의 "스토리 보기"용. 설득도·턴·대화 기록 등 실제 게임 상태는 절대 건드리지 않고,
        /// 현재 스테이지의 인트로컷/내레이션만 미리보기로 보여주기 위한 플래그만 세운다.
        /// 화면 전환(패널 표시)은 UI가 담당한다.
        /// </summary>
        public void PeekStory()
        {
            IsPeeking = true;
            CurrentBeatKind = StoryBeatKind.StageIntro;
        }

        /// <summary>스토리 미리보기를 닫는다. 진행 중이던 상태를 그대로 유지한다.</summary>
        public void ClosePeekStory()
        {
            IsPeeking = false;
        }

        private void ResetStageState()
        {
            Persuasion = 0;
            TurnCount = 0;
            zeroStreak = 0;
            negativeStreak = 0;
            history.Clear();
            unlockedEvidenceIds.Clear();
            _lastPlayerText = null;
            _repeatStreak = 0;
            _pendingPlayerText = null;
            BestPlayerText = null;
            BestPersuasionDelta = 0;
            GameAnalytics.LogStageStart(CurrentStage?.stageId ?? "");
        }

        /// <summary>사용 턴 비율로 S/A/B/C 등급을 반환한다.</summary>
        public string CalculateGrade()
        {
            int maxT = gameData != null ? gameData.maxTurns : 20;
            float ratio = maxT > 0 ? (float)TurnCount / maxT : 1f;
            if (ratio <= 0.35f) return "S";
            if (ratio <= 0.55f) return "A";
            if (ratio <= 0.75f) return "B";
            return "C";
        }

        // 반복 메시지 패널티용 대사 목록
        static readonly string[] RepeatLines =
        {
            "...방금 한 말 또 하는 거요? 말 다했으면 나가게 해줘요.",
            "같은 말 반복해도 내 대답은 안 바뀌어요. 할 말 있으면 다른 말로 해봐요.",
            "아까 그 말이요? 들었어요. 근데 그게 뭘 달라지게 한다고 생각해요?",
            "계속 같은 소리만 하면 더 이상 대화 안 할 거예요.",
        };

        /// <summary>진행 중인 LLM 요청을 즉시 중단한다. UI 타임아웃 발동 시 호출.</summary>
        public void AbortCurrentLLMRequest()
        {
            if (llmClient != null) llmClient.AbortCurrentRequest();
        }

        public void SubmitInput(string playerText)
        {
            if (llmClient == null || gameData == null || CurrentStage == null)
            {
                OnError?.Invoke("GameData/LLMClient가 설정되지 않았습니다.");
                return;
            }

            // ── 반복 메시지 감지: 직전 플레이어 발언과 동일하면 LLM 호출 없이 패널티 ──
            string normalized = playerText.Trim();
            if (_lastPlayerText != null && normalized == _lastPlayerText)
            {
                _repeatStreak++;
                history.Add(new ChatTurn { role = "player", text = playerText });
                int lineIdx = Mathf.Min(_repeatStreak - 1, RepeatLines.Length - 1);
                HandleResponse(new NPCResponse
                {
                    dialogue         = RepeatLines[lineIdx],
                    emotion          = _repeatStreak >= 3 ? "Anger" : "Disgust",
                    gesture          = "",
                    persuasionDelta  = -8,
                    hint             = "같은 말을 반복하면 오히려 역효과입니다. 다른 접근법을 시도해 보세요.",
                    goalAchieved     = false,
                });
                return;
            }
            _lastPlayerText = normalized;
            _repeatStreak = 0;
            // ────────────────────────────────────────────────────────────────────

            history.Add(new ChatTurn { role = "player", text = playerText });
            _pendingPlayerText = playerText;
            string prompt = BuildSystemPrompt();
            llmClient.RequestNPCResponse(prompt, history, HandleResponse, err => OnError?.Invoke(err));
        }

        private void HandleResponse(NPCResponse response)
        {
            Debug.Log($"[LLM RAW RESPONSE] Dialogue: {response.dialogue} | Evidence: '{response.unlocked_evidence}' | Hint: '{response.hint}'");

            // JSON 필드를 이용한 증거 해금 처리
            if (!string.IsNullOrEmpty(response.unlocked_evidence))
            {
                string evId = response.unlocked_evidence.Trim();
                
                var stage = CurrentStage;
                if (stage != null && stage.evidences != null && !unlockedEvidenceIds.Contains(evId))
                {
                    foreach (var ev in stage.evidences)
                    {
                        if (ev.evidenceId == evId)
                        {
                            unlockedEvidenceIds.Add(evId);
                            OnEvidenceUnlocked?.Invoke(ev);
                            break;
                        }
                    }
                }
            }

            // 설득도 구간별 저항 계수 — GPT가 너무 관대하게 채점해도 코드에서 상한 보정
            // 후반부로 갈수록 캐릭터가 굳어져 같은 발화도 효과가 줄어드는 구조
            int effectiveDelta = response.persuasionDelta;
            if (effectiveDelta > 0)
            {
                float resistance = 1f;
                if      (Persuasion >= 80) resistance = 0.35f;
                else if (Persuasion >= 60) resistance = 0.60f;
                else if (Persuasion >= 40) resistance = 0.80f;
                effectiveDelta = Mathf.Max(1, Mathf.RoundToInt(effectiveDelta * resistance));
            }
            Debug.Log($"[DELTA] raw={response.persuasionDelta} effective={effectiveDelta} persuasion={Persuasion}%");

            // 목표 정보를 실제로 자백/실토하면 즉시 100%로 간주.
            // 단, 설득도 80% 미만에서 GPT가 goalAchieved=true를 조기 반환하는 오작동 방어:
            // 80% 미만이면 goalAchieved를 무시하고 delta만 적용(스테이지가 너무 일찍 끝나는 버그 차단).
            if (response.goalAchieved && Persuasion >= 80)
                Persuasion = 100;
            else
            {
                if (response.goalAchieved) response.goalAchieved = false; // 조기 발동 무효화
                Persuasion = Mathf.Clamp(Persuasion + effectiveDelta, 0, 100);
            }
            TurnCount++;

            if (effectiveDelta > 0 && effectiveDelta > BestPersuasionDelta)
            {
                BestPersuasionDelta = effectiveDelta;
                BestPlayerText = _pendingPlayerText;
            }

            if (response.persuasionDelta < 0)
            {
                negativeStreak++;
                zeroStreak = (Persuasion == 0) ? zeroStreak + 1 : 0;
            }
            else
            {
                negativeStreak = 0;
                zeroStreak = 0;
            }

            history.Add(new ChatTurn { role = "npc", text = response.dialogue });
            OnNPCReplied?.Invoke(response);

            if (Persuasion >= 100)
            {
                HighestUnlockedIndex = Mathf.Max(HighestUnlockedIndex, CurrentStageIndex + 1);
                SaveManager.SetInt("pm_highest_idx", HighestUnlockedIndex);
                SaveStageBest();
                GameAnalytics.LogStageClear(CurrentStage?.stageId ?? "", CalculateGrade(), TurnCount);
                StartCoroutine(DelayedStageClear(response.dialogue));
                return;
            }
            if (zeroStreak >= gameData.zeroStreakFailThreshold)   { GameAnalytics.LogStageFail(CurrentStage?.stageId ?? "", TurnCount); SetPhase(GamePhase.StageFailed); return; }
            if (negativeStreak >= gameData.negativeStreakFailThreshold) { GameAnalytics.LogStageFail(CurrentStage?.stageId ?? "", TurnCount); SetPhase(GamePhase.StageFailed); return; }
            if (TurnCount >= gameData.maxTurns)                        { GameAnalytics.LogStageFail(CurrentStage?.stageId ?? "", TurnCount); SetPhase(GamePhase.StageFailed); return; }
        }

        public void AdvanceToNextStage()
        {
            var prevStage = CurrentStage;
            PendingTransitionFromStageId = prevStage != null ? prevStage.stageId : null;
            PendingTransitionNarration = prevStage != null ? prevStage.transitionNarration : null;

            CurrentStageIndex++;
            if (gameData == null || gameData.stages == null || CurrentStageIndex >= gameData.stages.Length)
            {
                GameAnalytics.LogGameCompleted(TurnCount);
                SetPhase(GamePhase.GameCompleted);
                return;
            }
            CurrentBeatKind = StoryBeatKind.StageTransition;
            SetPhase(GamePhase.StoryBeat);
        }

        public void RetryCurrentStage()
        {
            ResetStageState();
            SetPhase(GamePhase.Playing);
        }

        private void SetPhase(GamePhase phase)
        {
            CurrentPhase = phase;
            OnPhaseChanged?.Invoke(phase);
        }

        private System.Collections.IEnumerator DelayedStageClear(string dialogue)
        {
            float readTime = Mathf.Clamp((dialogue ?? "").Length * 0.05f + 1f, 3f, 8f);
            yield return new WaitForSecondsRealtime(readTime);
            SetPhase(GamePhase.StageCleared);
        }

        private string BuildSystemPrompt()
        {
            var stage = CurrentStage;
            var sb = new StringBuilder();
            sb.AppendLine("당신은 다음 캐릭터를 연기하는 롤플레이 상대입니다: " + stage.characterName);
            sb.AppendLine("배경 상황: " + stage.chapterTitle + " / 플레이어 역할: " + stage.playerRole);
            sb.AppendLine("캐릭터 성격: " + stage.characterPersona);
            sb.AppendLine("현재 목표(플레이어가 알아내야 하는 것): " + stage.goal);
            sb.AppendLine("현재 설득도: " + Persuasion + "%, 턴: " + TurnCount + "/" + gameData.maxTurns);
            sb.AppendLine("설득도 0% 근처 대사 예시: " + stage.sampleLineAt0);
            sb.AppendLine("설득도 50% 근처 대사 예시: " + stage.sampleLineAt50);
            sb.AppendLine("설득도 100% 근처 대사 예시: " + stage.sampleLineAt100);
            sb.AppendLine("플레이어 입력은 자유 텍스트입니다. 괄호()나 대괄호[] 안에 있는 텍스트는 플레이어의 행동, 표정, 제스처 또는 현장의 분위기를 나타냅니다. 이를 바탕으로 상황을 인식하고 실감나게 반응하세요. 예: \"[싸늘한 분위기] (책상을 강하게 내리치며) 다 알고 왔어.\"");
            sb.AppendLine("절대 설정된 성격에서 벗어나지 말고, 플레이어가 어떤 입력을 하든 캐릭터를 유지하세요.");
            sb.AppendLine("★ [중요] 캐릭터 성격 란에 명시된 [시스템 지침](예: 사투리, 특수 말투 등)이 있다면 반드시 100% 준수하여 해당 말투로만 대답하세요!");
            sb.AppendLine("매우 중요: dialogue는 오직 이 캐릭터(" + stage.characterName + ")가 직접 말하는 새로운 대사여야 합니다. 플레이어(" + stage.playerRole + ")의 말을 그대로 따라 하거나 되풀이하거나 인용하지 말고, 반드시 캐릭터 본인의 입장에서 반응하는 서로 다른 대사를 매번 새로 생성하세요. 이전 대사를 똑같이 반복하지 마세요.");
            sb.AppendLine("Confused는 당황/난처함(불안한 반응), Bewildered는 황당함/어이없음(기가 찬 반응)입니다 — 상황에 맞게 구분해서 선택하세요.");

            // 플레이어가 설득에 어려움을 겪을 때 공략 힌트를 제공한다.
            bool struggling = (TurnCount >= 3 && Persuasion < 40) || negativeStreak >= 2;
            if (struggling)
            {
                sb.AppendLine("플레이어가 설득에 어려움을 겪고 있습니다(설득도 정체 또는 하락). dialogue와는 별개로 \"hint\" 필드에, 이 캐릭터를 어떻게 공략해야 하는지(감정 압박/논리적 설득/신뢰 쌓기/자존심 자극 등 이 캐릭터의 성격과 약점에 맞는 방향) 플레이어에게 도움이 될 구체적인 1문장 힌트를 한국어로 제시하세요. 캐릭터의 대사가 아니라, 시스템이 플레이어에게 주는 공략 힌트입니다.");
            }
            else
            {
                sb.AppendLine("\"hint\" 필드는 빈 문자열로 두세요.");
            }

            // 증거 해금 명세: GPT가 임의 ID를 생성하지 못하도록 가능한 ID를 명시
            var stageEvs = stage.evidences;
            if (stageEvs != null && stageEvs.Length > 0)
            {
                sb.AppendLine("이 스테이지에서 해금 가능한 증거 목록 (플레이어 발화가 정확히 해당 조건을 충족할 때만 반환, 그 외 빈 문자열):");
                foreach (var ev in stageEvs)
                    sb.AppendLine("  - evidenceId=\"" + ev.evidenceId + "\": " + ev.evidenceDescription + " | 해금 조건: " + ev.unlockTag);
                sb.AppendLine("unlocked_evidence에는 위 evidenceId 중 하나 또는 빈 문자열만 넣으세요. 다른 값은 절대 안 됩니다.");
            }
            else
            {
                sb.AppendLine("unlocked_evidence는 항상 빈 문자열(\"\")로 두세요.");
            }

            sb.AppendLine("다음 JSON 스키마로만 응답하세요:");
            sb.AppendLine("{\"dialogue\": string (캐릭터 대사, 200자 이내),");
            sb.AppendLine(" \"emotion\": \"Anger|Disgust|Fear|Joy|Sadness|Surprise|Confused|Bewildered\",");
            sb.AppendLine(" \"gesture\": string (캐릭터 짧은 몸짓 묘사, 한국어 15자 이내. 예: '팔짱을 낀다'),");
            sb.AppendLine(" \"persuasionDelta\": int,");
            sb.AppendLine(" \"hint\": string,");
            sb.AppendLine(" \"unlocked_evidence\": string,");
            sb.AppendLine(" \"goalAchieved\": boolean}");
            sb.AppendLine("persuasionDelta 채점 원칙: 오직 플레이어의 발화가 '이 캐릭터에게 실제로 통하는 설득 포인트(위 캐릭터 성격에 적힌 공략 방향·약점·감정 트리거)'를 정확히 짚었을 때만 설득도를 올리세요.");
            sb.AppendLine("- 핵심 급소를 정확하고 강력하게 파고든 크리티컬한 설득: +15~+22");
            sb.AppendLine("- 올바른 방향으로 효과적으로 설득: +7~+13");
            sb.AppendLine("- 방향은 맞지만 약하거나 두루뭉술함: +2~+5");
            sb.AppendLine("- 목표·설득과 무관한 일반적 잡담/영양가 없는 말: 0 (올리지 마세요). 단, 이 캐릭터의 성격에 '잡담·라포·친밀감'이 공략법으로 명시되어 있다면, 자연스럽고 진짜 라포를 쌓는 사담은 소폭 양수(+2~+5) 가능.");
            sb.AppendLine("- 위협·협박성 발언: 일반적으로 반드시 음수(-8~-15). 단, 이 캐릭터의 성격에 '협박·갈구기·윽박·강압이 급소'라고 명시된 경우에 한해 예외적으로 크게 양수(+7~+15) 가능.");
            sb.AppendLine("- 욕설·모욕·인신공격: 일반적으로 반드시 음수(-10~-20). 단, 위와 동일하게 캐릭터 성격에 그것이 급소로 명시된 경우 예외.");
            sb.AppendLine("- 이전에 이미 한 말과 거의 같거나 동일한 말의 반복: 반드시 음수(-8~-12). 같은 말 반복은 효과가 없다.");
            sb.AppendLine("- 자음/모음의 무의미한 반복(ㅇㅈㄹ, ㅋㅋ, ㅎㅎ 등), 문장 부호(~, !, ? 등) 남발, 내용 없는 맞장구: 반드시 0 또는 음수. 절대 설득도를 올리지 마세요.");
            sb.AppendLine("그냥 말을 걸거나 질문했다는 이유만으로는 절대 설득도를 올리지 마세요. 반드시 설득이 먹히는 지점을 실제로 짚었을 때만, 그 정확도와 위력에 비례해 올리세요.");

            // 설득도 구간별 저항 강화 지시
            if (Persuasion >= 80)
                sb.AppendLine("[후반 저항] 현재 설득도가 80% 이상입니다. 캐릭터가 마지막 방어선을 치며 격렬히 저항합니다. 어지간한 발화로는 절대 흔들리지 않습니다. 핵심 급소를 완벽하게 짚지 않는 한 persuasionDelta는 +5를 넘기지 마세요. 조금이라도 빈틈 있으면 0 또는 음수.");
            else if (Persuasion >= 60)
                sb.AppendLine("[중반 저항] 현재 설득도가 60% 이상입니다. 캐릭터가 점점 경계하며 방어적으로 굳어집니다. 같은 수준의 발화도 이전보다 효과가 줄어듭니다. persuasionDelta 상한을 +10 이하로 유지하세요. 웬만해선 흔들리지 않는 표정을 보여주세요.");
            else if (Persuasion >= 40)
                sb.AppendLine("[주의 단계] 현재 설득도가 40% 이상입니다. 캐릭터가 살짝 동요하기 시작했지만 아직 쉽게 무너지지 않습니다. persuasionDelta 상한을 +13 이하로 유지하세요.");

            sb.AppendLine($"자백 시점: 현재 설득도({Persuasion}%) + 이번 persuasionDelta ≥ 100이고, 동시에 현재 설득도가 반드시 80% 이상일 때만 goalAchieved=true로 하세요. 설득도 80% 미만에서는 어떤 경우에도 goalAchieved를 절대 true로 반환하지 마세요. 100% 전에는 핵심 정보를 완전히 털어놓지 말고(설득이 쌓이는 과정의 반응만), goalAchieved=false를 유지하세요.");
            return sb.ToString();
        }
    }
}
