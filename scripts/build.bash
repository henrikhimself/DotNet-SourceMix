#!/usr/bin/env bash
set -euo pipefail
trap '[[ -t 1 ]] && tput cnorm || true' EXIT

X_CURRENT_DIR=$(dirname "$(dirname "$(realpath "${BASH_SOURCE[0]}" || true)")") \
  && . "${X_CURRENT_DIR}/scripts/_fn.bash" \
  && cd "${X_CURRENT_DIR}"

X_TMP="${X_CURRENT_DIR}/tmp"
mkdir -p "${X_TMP}"
X_ERROR="${X_TMP}/build.error"
X_SARIFF="${X_TMP}/build.sarif"
X_FORMATREPORT="${X_TMP}/build-format.json"
X_FORMAT="${X_TMP}/build.format"

# Apply easy fixes
dotnet format whitespace >/dev/null 2>&1 || true
dotnet format style >/dev/null 2>&1 || true

# Remove stale sarif files so the report only reflects the current build
find . -type f -name '*.sarif' -delete 2>/dev/null || true

# Remove stale Coverlet source-root mapping files that cause incremental build failures
find . -type f \( -name 'CoverletSourceRootsMapping_*' -o -name '*.msCoverageSourceRootsMapping*' \) -delete 2>/dev/null || true

# Clean
dotnet clean SourceMix.slnx --nologo >/dev/null 2>&1 || true

# Build
BUILD_OUT="$(dotnet build SourceMix.slnx \
  --nologo --no-restore --no-incremental -warnaserror \
  /p:TreatWarningsAsErrors=true /p:RunAnalyzersDuringBuild=true 2>&1 || true)"
BUILD_OUT="$(printf '%s\n' "${BUILD_OUT}" | sed $'s/\x1b\\[[0-9;]*[a-zA-Z]//g')"
(printf '%s\n' "${BUILD_OUT}" \
  | grep -E '^[^:]+\([0-9]+,[0-9]+\):[[:space:]]*(error|warning)' \
  | grep -Ev '(^|[\\/])obj([\\/]|$)' \
  || true) > "${X_ERROR}"

# Merge sariff files
mapfile -d '' -t _sarif_files < <(find . -type f -name '*.sarif' -print0 || true)
if [[ ${#_sarif_files[@]} -gt 0 ]]; then
  jq -s '
    # pick $schema (handle $ in key) and version from first document
    {
      "$schema": (.[0]["$schema"] // null),
      "version": (.[0].version // null),
      "runs": (map(.runs? // []) | add // [])
    }
  ' "${_sarif_files[@]}" \
  | jq -r '
    .runs[]?.results[]? |
    select((.suppressionStates // [] | length) == 0) |
    ( (.locations[0].resultFile.uri // "") ) as $rawUri |
    ( $rawUri | sub("^[A-Za-z][A-Za-z0-9+.-]*://"; "") ) as $uriNoScheme |
    select( ($uriNoScheme | test("(^|/)obj(/|$)") ) | not ) |
    ( .ruleId // (.rule?.id // "unknown") ) as $rule |
    ( (.message // "") | gsub("\n"; " ") ) as $msg |
    ( $uriNoScheme ) as $uri |
    ( (.locations[0].resultFile.region.startLine // 0) ) as $line |
    ( (.locations[0].resultFile.region.startColumn // 0) ) as $col |
    "\($uri)(\($line),\($col)): error \($rule): \($msg)"
  ' > "${X_SARIFF}"
else
  : > "${X_SARIFF}"
fi

# Create static code analysis report
dotnet format analyzers --report "${X_FORMATREPORT}" >/dev/null 2>&1 || true
if [[ -f "${X_FORMATREPORT}" ]] && [[ -s "${X_FORMATREPORT}" ]]; then
  jq -r '
    .[] |
    .FilePath as $path |
    .FileChanges[] |
    "\($path)(\(.LineNumber),\(.CharNumber)): \(.FormatDescription)"
  ' "${X_FORMATREPORT}" > "${X_FORMAT}"
else
  : > "${X_FORMAT}"
fi

# Consolidate into list of errors to be fixed.
hj::heading 1 'Build Result'
X_OUT="$(awk '!seen[$0]++' "${X_ERROR}" "${X_SARIFF}" "${X_FORMAT}")"
if [[ -n "${X_OUT}" ]]; then
  hj::text "${X_OUT}"
fi

if [[ -z "${X_OUT}" ]]; then
  X_COUNT=0
else
  X_COUNT="$(printf '%s\n' "${X_OUT}" | wc -l | tr -d ' ')"
fi
hj::text "Build finished with ${X_COUNT} errors."
