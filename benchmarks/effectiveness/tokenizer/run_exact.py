#!/usr/bin/env python3
import argparse
import hashlib
import json
from datetime import datetime, timezone

import tiktoken

parser = argparse.ArgumentParser()
parser.add_argument("--input", required=True)
parser.add_argument("--output", required=True)
parser.add_argument("--tokenizer", default="o200k_base")
args = parser.parse_args()

raw = open(args.input, "rb").read()
source_hash = hashlib.sha256(raw).hexdigest()
source = json.loads(raw)
encoding = tiktoken.get_encoding(args.tokenizer)
rows = []

for item in source["Workloads"]:
    counts = {
        "json": len(encoding.encode(item["Json"])),
        "aer_text": len(encoding.encode(item["AerText"])),
        "aer_ai": len(encoding.encode(item["AerAi"])),
    }
    json_tokens = counts["json"]
    rows.append({
        "workload": item["Workload"],
        **counts,
        "aer_text_saved": json_tokens - counts["aer_text"],
        "aer_ai_saved": json_tokens - counts["aer_ai"],
        "aer_text_saved_pct": round((json_tokens - counts["aer_text"]) * 100 / json_tokens, 4) if json_tokens else 0,
        "aer_ai_saved_pct": round((json_tokens - counts["aer_ai"]) * 100 / json_tokens, 4) if json_tokens else 0,
    })

report = {
    "benchmark_version": source["BenchmarkVersion"],
    "corpus_version": source["CorpusVersion"],
    "aer_runtime_version": source["AerRuntimeVersion"],
    "tokenizer": args.tokenizer,
    "tokenizer_library": "tiktoken",
    "timestamp_utc": datetime.now(timezone.utc).isoformat(),
    "input_sha256": source_hash,
    "results": rows,
}

json.dump(report, open(args.output, "w", encoding="utf-8"), indent=2, sort_keys=True)
print(json.dumps(report, indent=2, sort_keys=True))
