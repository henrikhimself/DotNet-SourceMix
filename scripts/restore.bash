#!/usr/bin/env bash
set -euo pipefail
trap '[[ -t 1 ]] && tput cnorm || true' EXIT

X_CURRENT_DIR=$(dirname "$(dirname "$(realpath "${BASH_SOURCE[0]}" || true)")") \
  && . "${X_CURRENT_DIR}/scripts/_fn.bash" \
  && cd "${X_CURRENT_DIR}"

X_BINLOG=restore.binlog

hj::heading 1 'Restore Result'

if (dotnet restore SourceMix.slnx \
  --verbosity detailed -bl:"${X_BINLOG}" 2>&1) >/dev/null; then
  hj::text "Restoring \`${X_CURRENT_DIR}/SourceMix.slnx\` successfully completed."
  exit 0
fi

hj::text "Restoring \`${X_CURRENT_DIR}/SourceMix.slnx\` FAILED to complete."

hj::heading 2 'Binlog Errors'
# shellcheck disable=SC2016
X_ERRORS="$(dotnet binlogtool search "${X_BINLOG}" '$error')" || true
hj::code 'plain' "${X_ERRORS}"

hj::heading 2 'Binlog Warnings'
# shellcheck disable=SC2016
X_WARNINGS="$(dotnet binlogtool search "${X_BINLOG}" '$warning')" || true
hj::code 'plain' "${X_WARNINGS}"
