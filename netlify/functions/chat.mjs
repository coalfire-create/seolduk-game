// 설득의 기술 — OpenAI 프록시 (Netlify Function)
// 목적: API 키를 서버(환경변수)에만 두고, 클라이언트는 이 엔드포인트만 호출한다.
// 비용 제어: (1) 입력 캡으로 호출당 비용 상한, (2) IP별 일일 호출 제한(Netlify Blobs),
//            (3) 모델/토큰을 서버에서 강제(클라이언트가 바꿀 수 없음).
// 최후의 보루로 OpenAI 대시보드에서 월 예산 한도(hard cap)를 반드시 설정하세요.

import { getStore } from '@netlify/blobs';

const MODEL          = 'gpt-4o-mini';   // 클라이언트가 무엇을 보내든 이 모델로 강제
const MAX_TOKENS     = 500;
const MAX_MESSAGES   = 40;
const MAX_MSG_LEN    = 4000;            // 메시지 1개 최대 문자수
const MAX_TOTAL_LEN  = 24000;           // 전체 대화 최대 문자수
const DAILY_IP_LIMIT = 60;             // IP당 하루 최대 호출 수

const ALLOWED_ROLES = new Set(['system', 'user', 'assistant']);

export const handler = async (event) => {
  const origin = event.headers?.origin || '*';
  const cors = {
    'Access-Control-Allow-Origin': origin,
    'Access-Control-Allow-Methods': 'POST, OPTIONS',
    'Access-Control-Allow-Headers': 'Content-Type',
    'Content-Type': 'application/json',
  };

  if (event.httpMethod === 'OPTIONS') return { statusCode: 204, headers: cors, body: '' };
  if (event.httpMethod !== 'POST')
    return { statusCode: 405, headers: cors, body: JSON.stringify({ error: 'method_not_allowed' }) };

  const apiKey = process.env.OPENAI_API_KEY;
  if (!apiKey)
    return { statusCode: 500, headers: cors, body: JSON.stringify({ error: 'server_misconfigured' }) };

  // ── 입력 파싱 & 검증 ────────────────────────────────────────
  let payload;
  try { payload = JSON.parse(event.body || '{}'); }
  catch { return { statusCode: 400, headers: cors, body: JSON.stringify({ error: 'invalid_json' }) }; }

  const messages = payload.messages;
  if (!Array.isArray(messages) || messages.length < 1 || messages.length > MAX_MESSAGES)
    return { statusCode: 400, headers: cors, body: JSON.stringify({ error: 'invalid_messages' }) };

  let total = 0;
  for (const m of messages) {
    if (!m || typeof m.role !== 'string' || typeof m.content !== 'string')
      return { statusCode: 400, headers: cors, body: JSON.stringify({ error: 'invalid_message_shape' }) };
    if (!ALLOWED_ROLES.has(m.role))
      return { statusCode: 400, headers: cors, body: JSON.stringify({ error: 'invalid_role' }) };
    if (m.content.length > MAX_MSG_LEN)
      return { statusCode: 400, headers: cors, body: JSON.stringify({ error: 'message_too_long' }) };
    total += m.content.length;
  }
  if (total > MAX_TOTAL_LEN)
    return { statusCode: 400, headers: cors, body: JSON.stringify({ error: 'conversation_too_long' }) };

  // ── IP별 일일 호출 제한 (best-effort) ───────────────────────
  const ip = (event.headers['x-nf-client-connection-ip']
           || event.headers['x-forwarded-for']
           || 'unknown').split(',')[0].trim();
  const today = new Date().toISOString().slice(0, 10);
  try {
    const store = getStore('llm-ratelimit');
    const key = `${today}:${ip}`;
    const cur = parseInt((await store.get(key)) || '0', 10) || 0;
    if (cur >= DAILY_IP_LIMIT)
      return { statusCode: 429, headers: cors, body: JSON.stringify({ error: 'rate_limited', message: '오늘의 사용 한도에 도달했습니다. 내일 다시 시도해 주세요.' }) };
    await store.set(key, String(cur + 1));
  } catch (e) {
    // Blobs 미구성 시: 입력 캡이 호출당 비용을 제한하므로 계속 진행(fail-open)
    console.error('[chat] rate-limit store unavailable:', e?.message);
  }

  // ── 서버 강제 요청 구성 (모델/토큰/포맷은 클라이언트가 변경 불가) ──
  const openaiBody = {
    model: MODEL,
    messages,
    response_format: { type: 'json_object' },
    max_tokens: MAX_TOKENS,
    temperature: (typeof payload.temperature === 'number')
      ? Math.min(Math.max(payload.temperature, 0), 1.2)
      : 0.9,
  };

  try {
    const r = await fetch('https://api.openai.com/v1/chat/completions', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${apiKey}` },
      body: JSON.stringify(openaiBody),
    });
    const text = await r.text();  // OpenAI 응답을 그대로 전달 (클라이언트가 choices[0].message.content 파싱)
    return { statusCode: r.status, headers: cors, body: text };
  } catch (e) {
    console.error('[chat] upstream error:', e?.message);
    return { statusCode: 502, headers: cors, body: JSON.stringify({ error: 'upstream_error' }) };
  }
};
