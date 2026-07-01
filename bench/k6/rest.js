import http from 'k6/http';
import { check } from 'k6';

// Carga REST contra o matchmaking (POST /api/matchmaking/join).
// Exercita o salto east-west matchmaking -> inventory (consulta de poder),
// alem de Redis/DB. Rode de DENTRO do mesh para o mTLS ser aplicado.
//
// Parametrizado por env:
//   BASE_URL   ex: http://matchmaking-service:8080
//   PLAYER_ID  GUID usado no corpo do join
//   K6_VUS / K6_DURATION sao passados via flags do k6 no Job.

// Inclui p99 no resumo (o k6 so calcula os percentis listados aqui).
export const options = {
  summaryTrendStats: ['avg', 'med', 'p(90)', 'p(95)', 'p(99)', 'max'],
};

const BASE_URL = __ENV.BASE_URL || 'http://matchmaking-service:8080';
const PLAYER_ID = __ENV.PLAYER_ID || '11111111-1111-1111-1111-111111111111';

export default function () {
  const res = http.post(
    `${BASE_URL}/api/matchmaking/join`,
    JSON.stringify({ playerId: PLAYER_ID }),
    { headers: { 'Content-Type': 'application/json' } },
  );
  check(res, { 'status is 2xx': (r) => r.status >= 200 && r.status < 300 });
}

// Emite um JSON compacto no stdout para o run.sh coletar.
export function handleSummary(data) {
  const d = data.metrics.http_req_duration ? data.metrics.http_req_duration.values : {};
  const reqs = data.metrics.http_reqs ? data.metrics.http_reqs.values : {};
  const failed = data.metrics.http_req_failed ? data.metrics.http_req_failed.values : {};
  const summary = {
    tool: 'k6',
    target: 'matchmaking:/api/matchmaking/join',
    count: reqs.count,
    rps: reqs.rate,
    fail_rate: failed.rate,
    latency_ms: {
      avg: d.avg,
      p50: d.med,
      p90: d['p(90)'],
      p95: d['p(95)'],
      p99: d['p(99)'],
      max: d.max,
    },
  };
  return { stdout: '\n===BENCH_JSON===\n' + JSON.stringify(summary) + '\n===END_BENCH_JSON===\n' };
}
