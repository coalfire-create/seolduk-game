# 설득의 기술 (The Art of Persuasion)

> AI NPC를 상대로 **대화만으로** 자백·협조를 이끌어내는 실시간 설득 어드벤처
> NAN 2026 NHN Game×AI 해커톤 예선 출품작 · 2인 팀(개발/기획)

**▶ 웹에서 바로 플레이:** https://lee-jeong-han.itch.io/seolduk

---

## 게임 개요

플레이어는 심문관이 되어, GPT-4.1로 구동되는 NPC와 자유 대화를 나눕니다. 정해진
선택지가 아니라 **직접 타이핑한 문장**이 매 턴 LLM에 전달되고, AI가 인물의 성격·심리
상태에 맞춰 실시간으로 반응하며 **설득도**를 판정합니다. 증거를 제시하고 심리를
파고들어 목표 설득도에 도달하면 스테이지 클리어.

- **9개 스테이지** (3챕터 × 3) — 경찰 / 정보총국 / 저항군 시나리오
- 턴 수·등급(S·A·B·C) 기반 **글로벌 리더보드**
- 한글/영어 자유 입력, 실시간 NPC 롤플레이 및 설득도 게이지

## 기술 스택

| 구분 | 내용 |
|---|---|
| 엔진 | Unity 6 (6000.5.4f1) — WebGL · Windows · macOS |
| 런타임 AI | OpenAI **GPT-4.1** (매 턴 NPC 응답 + 설득도 JSON 판정) |
| 백엔드 | Vercel Serverless (`/api/chat`, `/api/leaderboard`, `/api/gamedata`) |
| 리더보드 | Upstash for Redis (REST) |
| AI 제작 에셋 | 삽화·초상화 89+ · SUNO BGM 19 · ElevenLabs 음성 9 |

## 아키텍처

```
Unity WebGL (itch.io CDN)
        │  fetch (CORS)
        ▼
Vercel Serverless
  ├─ /api/chat        → OpenAI GPT-4.1  (API 키는 서버 env, 클라이언트 미노출)
  └─ /api/leaderboard → Upstash Redis   (스테이지별 상위 랭킹)
```

- **API 키는 전적으로 서버측**(`process.env.OPENAI_API_KEY`)에서만 사용되며 클라이언트
  빌드·저장소 어디에도 포함되지 않습니다.
- itch.io 등 외부 도메인에서 실행돼도 `LLMClient`/`LeaderboardManager`가 페이지
  오리진을 감지해 Vercel 절대 URL로 자동 호출합니다.

## WebGL 플랫폼 대응

Unity 6 WebGL에서 직접 해결한 주요 이슈:

- **한글/영어 입력** — Unity 6에서 제거된 `WebGLInput.captureAllKeyboardInput` 대신
  캔버스 포커스 제어 + IME 전용 textarea 오버레이(`KoreanInput.jslib`)로 조합 입력 처리
- **오디오 자동재생 차단** — 첫 사용자 입력에서 `AudioContext.resume()`
- **itch.io iframe 임베드** — 1280×720 반응형 캔버스, 크로스도메인 API 호출

## 로컬 실행 (에디터)

1. Unity 6 (6000.5.4f1)로 프로젝트 열기
2. `Assets/StreamingAssets/apikey.txt.example` → `apikey.txt`로 복사 후 본인 OpenAI
   키 입력 (이 파일은 `.gitignore`로 커밋되지 않습니다)
3. 에디터에서 Play — WebGL/스탠드얼론 빌드는 서버 프록시를 사용하므로 로컬 키 불필요

---

© 2026 설득의 기술 팀 · NAN 2026 NHN Game×AI 해커톤
