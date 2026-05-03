#!/usr/bin/env bash
set -euo pipefail
trap '[[ -t 1 ]] && tput cnorm || true' EXIT

X_CURRENT_DIR=$(dirname "$(dirname "$(realpath "${BASH_SOURCE[0]}" || true)")") \
  && . "${X_CURRENT_DIR}/scripts/_fn.bash" \
  && cd "${X_CURRENT_DIR}"

X_PROJECT="src/SourceMix/SourceMix.csproj"
X_OUTPUT_DIR="${X_CURRENT_DIR}/src/SourceMix/bin/Release"

hj::heading 1 'Pack Result'

if ! PACK_OUT="$(dotnet pack "${X_PROJECT}" -c Release --nologo 2>&1)"; then
  hj::text "Packing \`${X_CURRENT_DIR}/${X_PROJECT}\` FAILED to complete."
  hj::code 'plain' "${PACK_OUT}"
  exit 1
fi

X_PACKAGE="$(
  find "${X_OUTPUT_DIR}" -maxdepth 1 -type f -name '*.nupkg' ! -name '*.snupkg' \
    | while IFS= read -r f; do echo "$(date -r "$f" +%s) $f"; done \
    | sort -nr \
    | head -n 1 \
    | cut -d' ' -f2-
)"

if [[ -z "${X_PACKAGE}" ]]; then
  hj::text "Packing \`${X_CURRENT_DIR}/${X_PROJECT}\` did not produce a publishable package."

  if [[ -n "${PACK_OUT}" ]]; then
    hj::code 'plain' "${PACK_OUT}"
  fi

  exit 1
fi

hj::text "Packing \`${X_CURRENT_DIR}/${X_PROJECT}\` successfully completed."
hj::text "Package ready for publishing: \`${X_PACKAGE}\`"

X_SYMBOLS="$(
  find "${X_OUTPUT_DIR}" -maxdepth 1 -type f -name '*.snupkg' \
    | while IFS= read -r f; do echo "$(date -r "$f" +%s) $f"; done \
    | sort -nr \
    | head -n 1 \
    | cut -d' ' -f2-
)"

if [[ -n "${X_SYMBOLS}" ]]; then
  hj::text "Symbols package ready for publishing: \`${X_SYMBOLS}\`"
fi
