#!/usr/bin/env python3
"""Agrega os resultados brutos do benchmark em tabelas (markdown + CSV).

Le a arvore results/<stamp>/<scenario>/rep<N>/{ghz.json,k6.log} e produz:
  - summary.csv    (linha por cenario/ferramenta/metrica, media e desvio)
  - summary.md     (tabelas prontas para colar no artigo)

Nao inventa dados: apenas resume o que os geradores de carga mediram.
Uso: python3 aggregate.py results/<stamp>
"""
import json
import re
import sys
import statistics
from pathlib import Path

SCENARIO_LABEL = {
    "A": "A - sem mesh",
    "B": "B - mesh, mTLS OFF",
    "C": "C - mesh, mTLS ON",
}


def ns_to_ms(v):
    return None if v is None else v / 1_000_000.0


def parse_ghz(path):
    """ghz --format json: latencias em nanosegundos."""
    try:
        data = json.loads(path.read_text(encoding="utf-8", errors="ignore"))
    except Exception:
        return None
    dist = {int(d["percentage"]): d["latency"] for d in data.get("latencyDistribution", [])}
    return {
        "rps": data.get("rps"),
        "avg_ms": ns_to_ms(data.get("average")),
        "p50_ms": ns_to_ms(dist.get(50)),
        "p90_ms": ns_to_ms(dist.get(90)),
        "p95_ms": ns_to_ms(dist.get(95)),
        "p99_ms": ns_to_ms(dist.get(99)),
        "count": data.get("count"),
    }


def parse_k6(path):
    """k6 handleSummary: bloco JSON entre marcadores; latencias em ms."""
    text = path.read_text(encoding="utf-8", errors="ignore")
    m = re.search(r"===BENCH_JSON===\s*(\{.*?\})\s*===END_BENCH_JSON===", text, re.S)
    if not m:
        return None
    try:
        d = json.loads(m.group(1))
    except Exception:
        return None
    lat = d.get("latency_ms", {})
    return {
        "rps": d.get("rps"),
        "avg_ms": lat.get("avg"),
        "p50_ms": lat.get("p50"),
        "p90_ms": lat.get("p90"),
        "p95_ms": lat.get("p95"),
        "p99_ms": lat.get("p99"),
        "fail_rate": d.get("fail_rate"),
        "count": d.get("count"),
    }


def agg(values):
    vals = [v for v in values if isinstance(v, (int, float))]
    if not vals:
        return (None, None)
    mean = statistics.mean(vals)
    sd = statistics.stdev(vals) if len(vals) > 1 else 0.0
    return (mean, sd)


def fmt(mean, sd):
    if mean is None:
        return "n/a"
    return f"{mean:.2f} ± {sd:.2f}"


def collect(root, tool, parser, filename):
    """Retorna {scenario: {metric: [valores por rep]}}"""
    out = {}
    for scen_dir in sorted(p for p in root.iterdir() if p.is_dir() and p.name in SCENARIO_LABEL):
        per_metric = {}
        for rep_dir in sorted(scen_dir.glob("rep*")):
            f = rep_dir / filename
            if not f.exists():
                continue
            parsed = parser(f)
            if not parsed:
                continue
            for k, v in parsed.items():
                per_metric.setdefault(k, []).append(v)
        if per_metric:
            out[scen_dir.name] = per_metric
    return out


METRICS = [("rps", "Throughput (req/s)"), ("avg_ms", "Latencia media (ms)"),
           ("p50_ms", "p50 (ms)"), ("p95_ms", "p95 (ms)"), ("p99_ms", "p99 (ms)")]


def build_tables(root):
    lines = ["# Resultados do benchmark", "",
             "Gerado por `bench/aggregate.py`. Valores: media ± desvio padrao entre repeticoes.",
             ""]
    csv_rows = ["tool,scenario,metric,mean,stdev,n"]

    for tool, parser, filename, title in [
        ("ghz", parse_ghz, "ghz.json", "gRPC: battle-service -> inventory-service (GetBattleStats)"),
        ("k6", parse_k6, "k6.log", "REST: cliente -> matchmaking-service (/join)"),
    ]:
        data = collect(root, tool, parser, filename)
        if not data:
            continue
        lines += [f"## {title}", "", "| Metrica | " +
                  " | ".join(SCENARIO_LABEL[s] for s in sorted(data)) + " |",
                  "|---|" + "---|" * len(data)]
        for mkey, mlabel in METRICS:
            row = [mlabel]
            for s in sorted(data):
                mean, sd = agg(data[s].get(mkey, []))
                row.append(fmt(mean, sd))
                n = len([v for v in data[s].get(mkey, []) if isinstance(v, (int, float))])
                csv_rows.append(f"{tool},{s},{mkey},{'' if mean is None else f'{mean:.4f}'},"
                                f"{'' if mean is None else f'{sd:.4f}'},{n}")
            lines.append("| " + " | ".join(row) + " |")
        lines.append("")

        # Overhead relativo B->C (custo da criptografia) e A->C (custo total)
        if "C" in data:
            lines.append("### Overhead relativo (latencia media)")
            base_c, _ = agg(data["C"].get("avg_ms", []))
            for base in ("A", "B"):
                if base in data and base_c is not None:
                    base_v, _ = agg(data[base].get("avg_ms", []))
                    if base_v:
                        pct = (base_c - base_v) / base_v * 100.0
                        lines.append(f"- {base} -> C: {pct:+.1f}%")
            lines.append("")

    return "\n".join(lines), "\n".join(csv_rows)


def main():
    if len(sys.argv) < 2:
        print("uso: python3 aggregate.py results/<stamp>", file=sys.stderr)
        sys.exit(2)
    root = Path(sys.argv[1])
    md, csv = build_tables(root)
    (root / "summary.md").write_text(md, encoding="utf-8")
    (root / "summary.csv").write_text(csv, encoding="utf-8")
    print(f"Escrito: {root/'summary.md'}")
    print(f"Escrito: {root/'summary.csv'}")
    print()
    print(md)


if __name__ == "__main__":
    main()
