// 설득의 기술 — 글로벌 리더보드 (Vercel Serverless Function)
// Netlify Blobs → Vercel KV REST API 로 교체
// KV 미설정 시 빈 리더보드 반환 (게임 자체는 정상 동작)
//   GET  /api/leaderboard?stage=<stageId>
//   POST /api/leaderboard  { name, stageId, grade, turns }

const TOP_RETURN       = 20;
const TOP_KEEP         = 100;
const MAX_NAME_LEN     = 12;
const DAILY_IP_SUBMITS = 40;
const GRADE_RANK       = { S: 4, A: 3, B: 2, C: 1 };

const sanitizeName = (s) =>
  String(s ?? '').replace(/[<>]/g, '').trim().slice(0, MAX_NAME_LEN) || '익명';

const rankFn = (a, b) => {
  const ga = GRADE_RANK[a.grade] || 0, gb = GRADE_RANK[b.grade] || 0;
  if (ga !== gb) return gb - ga;
  if (a.turns !== b.turns) return a.turns - b.turns;
  return (a.ts || 0) - (b.ts || 0);
};

const publicEntries = (arr) =>
  arr.slice(0, TOP_RETURN).map(({ name, grade, turns }) => ({ name, grade, turns }));

// ── Vercel KV REST API 헬퍼 (패키지 없이 fetch로 직접 호출) ──
// Vercel 네이티브 KV(KV_REST_API_*)와 Upstash 마켓플레이스(UPSTASH_REDIS_REST_*)
// 두 경로 모두 지원 — 동일한 REST /pipeline API 라 env 이름만 다르다.
const KV_URL   = () => process.env.KV_REST_API_URL   || process.env.UPSTASH_REDIS_REST_URL;
const KV_TOKEN = () => process.env.KV_REST_API_TOKEN || process.env.UPSTASH_REDIS_REST_TOKEN;
const kvAvailable = () => !!(KV_URL() && KV_TOKEN());

async function kvGet(key) {
  if (!kvAvailable()) return null;
  try {
    const r = await fetch(
      `${KV_URL()}/pipeline`,
      { method: 'POST',
        headers: { Authorization: `Bearer ${KV_TOKEN()}`, 'Content-Type': 'application/json' },
        body: JSON.stringify([['GET', key]]) }
    );
    const [{ result }] = await r.json();
    return result ? JSON.parse(result) : null;
  } catch (e) { console.error('[lb] kvGet', e?.message); return null; }
}

async function kvSet(key, value, exSeconds = 0) {
  if (!kvAvailable()) return;
  const cmd = exSeconds
    ? ['SET', key, JSON.stringify(value), 'EX', exSeconds]
    : ['SET', key, JSON.stringify(value)];
  try {
    await fetch(
      `${KV_URL()}/pipeline`,
      { method: 'POST',
        headers: { Authorization: `Bearer ${KV_TOKEN()}`, 'Content-Type': 'application/json' },
        body: JSON.stringify([cmd]) }
    );
  } catch (e) { console.error('[lb] kvSet', e?.message); }
}

// ── 핸들러 ──────────────────────────────────────────────────
export default async function handler(req, res) {
  const origin = req.headers.origin || '*';
  res.setHeader('Access-Control-Allow-Origin', origin);
  res.setHeader('Access-Control-Allow-Methods', 'GET, POST, OPTIONS');
  res.setHeader('Access-Control-Allow-Headers', 'Content-Type');

  if (req.method === 'OPTIONS') return res.status(204).end();

  // KV 미설정: 빈 응답으로 graceful 처리
  if (!kvAvailable())
    return res.status(200).json({ entries: [], note: 'leaderboard_not_configured' });

  // ── GET ───────────────────────────────────────────────────
  if (req.method === 'GET') {
    const stageId = String(req.query?.stage || '').trim();
    if (!stageId) return res.status(400).json({ error: 'missing_stage' });
    const arr = (await kvGet(`lb:${stageId}`)) || [];
    return res.status(200).json({ entries: publicEntries(arr) });
  }

  // ── POST ──────────────────────────────────────────────────
  if (req.method === 'POST') {
    const body    = req.body || {};
    const stageId = String(body.stageId || '').trim().slice(0, 64);
    const grade   = String(body.grade   || '').trim().toUpperCase();
    const turns   = Number(body.turns);
    const name    = sanitizeName(body.name);

    if (!stageId)              return res.status(400).json({ error: 'missing_stage' });
    if (!GRADE_RANK[grade])    return res.status(400).json({ error: 'invalid_grade' });
    if (!Number.isInteger(turns) || turns < 1 || turns > 999)
                               return res.status(400).json({ error: 'invalid_turns' });

    // IP 일일 등록 상한 (best-effort)
    const ip = (req.headers['x-forwarded-for'] || req.socket?.remoteAddress || 'unknown')
      .split(',')[0].trim();
    const today = new Date().toISOString().slice(0, 10);
    const cntKey = `cnt:${today}:${ip}`;
    const cur = (await kvGet(cntKey)) || 0;
    if (cur >= DAILY_IP_SUBMITS)
      return res.status(429).json({ error: 'rate_limited' });
    await kvSet(cntKey, cur + 1, 86400 * 2);

    const lbKey = `lb:${stageId}`;
    const arr = (await kvGet(lbKey)) || [];
    arr.push({ name, grade, turns, ts: Date.now() });
    arr.sort(rankFn);
    const trimmed = arr.slice(0, TOP_KEEP);
    await kvSet(lbKey, trimmed);

    return res.status(200).json({ entries: publicEntries(trimmed) });
  }

  return res.status(405).json({ error: 'method_not_allowed' });
}
