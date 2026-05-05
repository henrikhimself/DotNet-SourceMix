#!/usr/bin/env bash
set -euo pipefail
trap '[[ -t 1 ]] && tput cnorm || true' EXIT

X_CURRENT_DIR=$(dirname "$(dirname "$(realpath "${BASH_SOURCE[0]}" || true)")") \
  && . "${X_CURRENT_DIR}/scripts/_fn.bash" \
  && cd "${X_CURRENT_DIR}"

dotnet test SourceMix.slnx --nologo --logger 'trx;LogFileName=test.trx' --blame-hang-timeout 30s 2>&1

dotnet mdreport trx '**/*.trx'
