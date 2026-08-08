# 배포 가이드 — 설득의 기술 (Netlify + API 프록시)

게임 클라이언트에는 더 이상 OpenAI 키가 포함되지 않습니다.
게임 → **Netlify Function 프록시** → OpenAI 구조로, 키는 서버 환경변수에만 존재합니다.

## 폴더 구조 (배포 루트 = 이 "My project" 폴더)

```
My project/
├─ netlify.toml              # publish=WebGL-Build, functions=netlify/functions, /api/chat 리다이렉트
├─ netlify/functions/
│  ├─ chat.mjs               # OpenAI 프록시 (키 주입 + 입력검증 + IP 일일제한)
│  ├─ leaderboard.mjs        # 글로벌 리더보드 (스테이지별 등급/턴 랭킹, Blobs 저장)
│  └─ package.json           # @netlify/blobs 의존성
├─ WebGL-Build/              # Unity WebGL 산출물 (매 빌드 재생성 — 키 없음)
│  ├─ index.html
│  ├─ _headers               # Brotli 헤더 (자동 생성)
│  └─ privacy.html           # 개인정보처리방침 (자동 복사)
└─ apikey.local.txt          # 에디터 개발용 키 (배포 안 됨, 절대 커밋 금지)
```

## 최초 1회 설정

1. **Netlify CLI 설치 & 로그인** (터미널에서):
   ```
   ! npm install -g netlify-cli
   ! netlify login
   ```

2. **사이트 연결** ("My project" 폴더에서):
   ```
   ! cd "$(pwd)" && netlify init      # 새 사이트 생성, 또는
   ! netlify link                     # 기존 사이트에 연결
   ```

3. **OpenAI 키를 서버 환경변수로 등록** (키가 서버에만 존재하게 됨):
   ```
   ! netlify env:set OPENAI_API_KEY "sk-..."
   ```
   또는 Netlify 대시보드 → Site settings → Environment variables 에서 `OPENAI_API_KEY` 추가.

4. **OpenAI 예산 한도(hard cap) 설정** — 최후의 비용 방어선:
   platform.openai.com → Settings → Limits → Monthly budget 에서 상한 설정.

## 배포 (매번)

Unity에서 WebGL 빌드 후, **"My project" 폴더 루트에서** 배포하세요
(WebGL-Build 폴더만 드래그하면 함수/리다이렉트가 빠집니다):

```
! netlify deploy --build --prod
```
또는 미리보기:
```
! netlify deploy --build
```

## 동작 확인

- 게임에서 대화 전송 → 정상 응답이면 프록시 연결 성공.
- `https://<사이트>/StreamingAssets/apikey.txt` → **404여야 정상** (키 미포함 확인).
- `https://<사이트>/privacy.html` → 개인정보처리방침 노출 확인.

## 비용 제어 요약

| 계층 | 위치 | 내용 |
|---|---|---|
| 입력 캡 | chat.mjs | 메시지 40개·개당 4천자·전체 2.4만자, max_tokens 500, 모델 강제 |
| IP 일일제한 | chat.mjs (Blobs) | IP당 60회/일 (초과 시 429) |
| 클라이언트 소프트제한 | LLMClient.cs | 브라우저당 50회/일 (UX 안내용) |
| 하드 예산 | OpenAI 대시보드 | 월 예산 상한 — 최종 방어선 |

## 글로벌 리더보드

- 스테이지 클리어 화면 → **🏆 랭킹 보기** → 닉네임 등록 / 상위 20위 조회.
- 저장은 **Netlify Blobs**(무료, 기본 활성). 별도 설정 불필요. 배포하면 바로 동작.
- 랭킹 기준: 등급(S>A>B>C) → 턴 적은 순 → 먼저 등록한 순.
- 에디터에서 테스트하려면 씬의 `Systems > LeaderboardManager` 컴포넌트의
  `Editor Base Url` 에 배포된 사이트 주소(예: `https://site.netlify.app`)를 넣으세요.
  (WebGL 빌드에서는 자동으로 현재 도메인 사용)

## GA4 (선택)

`Assets/Editor/PostBuildProcessor.cs` 의 `GA4_MEASUREMENT_ID` 를 실제 측정 ID(G-XXXX)로
교체하면, 다음 빌드부터 index.html 에 추적 스크립트가 자동 삽입됩니다.
