#!/usr/bin/env bash
set -euo pipefail
trap '[[ -t 1 ]] && tput cnorm || true' EXIT

X_CURRENT_DIR=$(dirname "$(dirname "$(realpath "${BASH_SOURCE[0]}" || true)")") \
  && . "${X_CURRENT_DIR}/scripts/_fn.bash" \
  && cd "${X_CURRENT_DIR}"

if [[ -f tmp/benchmark-latest.md ]]; then
  cp tmp/benchmark-latest.md tmp/benchmark-previous.md
fi

rm -rf tmp/BenchmarkDotNet.Artifacts
mkdir -p tmp
log_file="tmp/benchmark-run.log"
if ! dotnet run -c Release --project test/SourceMix.Benchmarks \
    -- --filter '*' --artifacts tmp/BenchmarkDotNet.Artifacts \
    > "$log_file" 2>&1; then
  cat "$log_file"
  exit 1
fi

{
  hj::heading 1 "Benchmark Results"

  while IFS= read -r md_file; do
    class_name=$(basename "$md_file" -report-github.md | sed 's/.*\.//')
    hj::heading 2 "$class_name"
    cat "$md_file"
  done < <(find tmp/BenchmarkDotNet.Artifacts/results -name '*-report-github.md' | sort)
} | tee tmp/benchmark-latest.md
