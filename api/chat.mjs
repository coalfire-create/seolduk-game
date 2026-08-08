// 설득의 기술 — OpenAI 프록시 (Vercel Serverless Function)
// Netlify Functions 버전에서 이식 (req/res 패턴으로 변환)

const MODEL         = 'gpt-4.1';
const MAX_TOKENS    = 500;
// 긴 취조 대화(수십 턴)에서도 막히지 않도록 넉넉히. gpt-4.1은 대용량 컨텍스트라 여유.
const MAX_MESSAGES  = 200;
const MAX_MSG_LEN   = 8000;
const MAX_TOTAL_LEN = 80000;

const ALLOWED_ROLES = new Set(['system', 'user', 'assistant']);

export default async function handler(req, res) {
  const origin = req.headers.origin || '*';
  res.setHeader('Access-Control-Allow-Origin', origin);
  res.setHeader('Access-Control-Allow-Methods', 'POST, OPTIONS');
  res.setHeader('Access-Control-Allow-Headers', 'Content-Type');

  if (req.method === 'OPTIONS') return res.status(204).end();
  if (req.method !== 'POST')
    return res.status(405).json({ error: 'method_not_allowed' });

  const apiKey = process.env.OPENAI_API_KEY;
  if (!apiKey)
    return res.status(500).json({ error: 'server_misconfigured' });

  const payload = req.body || {};
  const messages = payload.messages;

  if (!Array.isArray(messages) || messages.length < 1 || messages.length > MAX_MESSAGES)
    return res.status(400).json({ error: 'invalid_messages' });

  let total = 0;
  for (const m of messages) {
    if (!m || typeof m.role !== 'string' || typeof m.content !== 'string')
      return res.status(400).json({ error: 'invalid_message_shape' });
    if (!ALLOWED_ROLES.has(m.role))
      return res.status(400).json({ error: 'invalid_role' });
    if (m.content.length > MAX_MSG_LEN)
      return res.status(400).json({ error: 'message_too_long' });
    total += m.content.length;
  }
  if (total > MAX_TOTAL_LEN)
    return res.status(400).json({ error: 'conversation_too_long' });

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
    const upstream = await fetch('https://api.openai.com/v1/chat/completions', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${apiKey}` },
      body: JSON.stringify(openaiBody),
    });
    const data = await upstream.json();
    return res.status(upstream.status).json(data);
  } catch (e) {
    console.error('[chat] upstream error:', e?.message);
    return res.status(502).json({ error: 'upstream_error' });
  }
}
