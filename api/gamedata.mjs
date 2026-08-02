// 설득의 기술 — Unity .data.br 스트리밍 프록시
// media.githubusercontent.com은 Content-Encoding: br을 보내지 않아서 Unity가 실패함
// 이 함수가 올바른 헤더를 붙여서 브라우저가 brotli 압축을 풀도록 함

const DATA_URL =
  'https://media.githubusercontent.com/media/coalfire-create/seolduk-game/brotli-build/WebGL-Build/Build/WebGL-Build.data.br';

export const config = { maxDuration: 300 };

export default async function handler(req, res) {
  res.setHeader('Access-Control-Allow-Origin', '*');
  res.setHeader('Access-Control-Allow-Methods', 'GET, OPTIONS');
  if (req.method === 'OPTIONS') return res.status(204).end();
  if (req.method !== 'GET') return res.status(405).end();

  const upstream = await fetch(DATA_URL, {
    headers: { 'User-Agent': 'Mozilla/5.0' },
  });

  if (!upstream.ok) {
    return res.status(upstream.status).json({ error: 'upstream_failed' });
  }

  res.setHeader('Content-Type', 'application/octet-stream');
  res.setHeader('Content-Encoding', 'br');
  res.setHeader('Cache-Control', 'public, max-age=86400, immutable');

  const len = upstream.headers.get('content-length');
  if (len) res.setHeader('Content-Length', len);

  const reader = upstream.body.getReader();
  res.status(200);
  while (true) {
    const { done, value } = await reader.read();
    if (done) break;
    res.write(Buffer.from(value));
  }
  res.end();
}
