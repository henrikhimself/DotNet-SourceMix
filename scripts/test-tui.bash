#!/usr/bin/env bash
set -euo pipefail

trap '[[ -t 1 ]] && tput cnorm || true' EXIT

X_CURRENT_DIR=$(dirname "$(dirname "$(realpath "${BASH_SOURCE[0]}" || true)")") \
  && . "${X_CURRENT_DIR}/scripts/_fn.bash" \
  && cd "${X_CURRENT_DIR}"

APP_DLL="${X_CURRENT_DIR}/src/SourceMix/bin/Debug/net10.0/SourceMix.dll"
declare -a TMUX_SESSIONS=()
declare -a TMUX_WORKSPACES=()

hj::cleanup() {
    local session
    local workspace

    for session in ${TMUX_SESSIONS+"${TMUX_SESSIONS[@]}"}; do
        tmux kill-session -t "${session}" 2>/dev/null || true
    done

    for workspace in ${TMUX_WORKSPACES+"${TMUX_WORKSPACES[@]}"}; do
        rm -rf "${workspace}"
    done
}

trap hj::cleanup EXIT

hj::fail() {
    local message="$1"
    local capture="${2:-}"

    printf 'tmux TUI smoke test failed: %s\n' "${message}" >&2

    if [[ -n "${capture}" ]]; then
        printf '%s\n' '--- recent terminal frame ---' >&2
        printf '%s\n' "${capture}" >&2
        printf '%s\n' '--- end terminal frame ---' >&2
    fi

    exit 1
}

hj::ensure_prerequisites() {
    if ! command -v tmux >/dev/null 2>&1; then
        hj::fail "tmux is required for real-terminal TUI validation."
    fi

    if [[ ! -f "${APP_DLL}" ]]; then
        dotnet build "${X_CURRENT_DIR}/src/SourceMix/SourceMix.csproj" --nologo >/dev/null
    fi
}

hj::new_workspace() {
    local file_count="$1"
    local workspace
    local home
    local config
    local i
    local name

    workspace=$(mktemp -d)
    home="${workspace}/home-wrap-for-terminal-tests"
    config="${workspace}/config"

    mkdir -p "${home}" "${config}"
    TMUX_WORKSPACES+=("${workspace}")

    printf '<Solution/>\n' > "${workspace}/Test.slnx"

    for ((i = 0; i < file_count; i++)); do
        printf -v name 'File%02d' "${i}"
        printf 'public class %s { }\n' "${name}" > "${workspace}/${name}.cs"
    done

    printf '%s\n' "${workspace}"
}

hj::start_tui() {
    local workspace="$1"
    local width="$2"
    local height="$3"
    local session
    local command

    printf -v session 'sourcemix-tui-%s-%s' "$$" "${RANDOM}"
    printf -v command "cd '%s' && HOME='%s/home-wrap-for-terminal-tests' XDG_CONFIG_HOME='%s/config' DOTNET_CLI_HOME='%s/.dotnet' dotnet '%s' tui" \
        "${workspace}" "${workspace}" "${workspace}" "${workspace}" "${APP_DLL}"

    tmux new-session -d -s "${session}" -x "${width}" -y "${height}" "${command}"
    TMUX_SESSIONS+=("${session}")

    printf '%s\n' "${session}"
}

hj::visible_frame() {
    local target="$1"

    tmux capture-pane -p -t "${target}"
}

hj::wait_for_frame_contains() {
    local target="$1"
    local needle="$2"
    local attempts="${3:-80}"
    local frame
    local i

    for ((i = 0; i < attempts; i++)); do
        frame=$(hj::visible_frame "${target}")

        if grep -Fq "${needle}" <<<"${frame}"; then
            printf '%s\n' "${frame}"
            return 0
        fi

        sleep 0.1
    done

    frame=$(hj::visible_frame "${target}")
    hj::fail "expected terminal frame to contain '${needle}'." "${frame}"
}

hj::send_keys() {
    local target="$1"
    shift

    tmux send-keys -t "${target}" "$@"
    sleep 0.2
}

hj::send_text() {
    local target="$1"
    local text="$2"

    tmux send-keys -t "${target}" -l -- "${text}"
    sleep 0.2
}

hj::resize_window() {
    local session="$1"
    local width="$2"
    local height="$3"

    tmux resize-window -t "${session}:0" -x "${width}" -y "${height}"
    sleep 0.8
}

hj::assert_contains() {
    local haystack="$1"
    local needle="$2"

    if ! grep -Fq "${needle}" <<<"${haystack}"; then
        hj::fail "expected terminal frame to contain '${needle}'." "${haystack}"
    fi
}

hj::assert_occurrences() {
    local haystack="$1"
    local needle="$2"
    local expected="$3"
    local actual

    actual=$(grep -Foc "${needle}" <<<"${haystack}" || true)

    if [[ "${actual}" -ne "${expected}" ]]; then
        hj::fail "expected '${needle}' to appear ${expected} time(s), found ${actual}." "${haystack}"
    fi
}

hj::assert_order() {
    local haystack="$1"
    local first="$2"
    local second="$3"
    local first_line
    local second_line

    first_line=$(grep -Fn "${first}" <<<"${haystack}" | head -n 1 | cut -d: -f1 || true)
    second_line=$(grep -Fn "${second}" <<<"${haystack}" | head -n 1 | cut -d: -f1 || true)

    if [[ -z "${first_line}" || -z "${second_line}" || "${first_line}" -ge "${second_line}" ]]; then
        hj::fail "expected '${first}' to appear before '${second}'." "${haystack}"
    fi
}

hj::wait_for_file() {
    local path="$1"
    local attempts="${2:-80}"
    local i

    for ((i = 0; i < attempts; i++)); do
        if [[ -f "${path}" ]]; then
            return 0
        fi

        sleep 0.1
    done

    hj::fail "expected output file '${path}' to be created."
}

hj::run_validation_header_scenario() {
    local workspace
    local session
    local target
    local frame
    local cleared_frame

    workspace=$(hj::new_workspace 1)
    session=$(hj::start_tui "${workspace}" 80 24)
    target="${session}:0.0"

    hj::wait_for_frame_contains "${target}" "Filter:" >/dev/null
    hj::send_keys "${target}" Enter
    frame=$(hj::wait_for_frame_contains "${target}" "Select at least one file")

    hj::assert_occurrences "${frame}" "SourceMix." 1
    hj::assert_contains "${frame}" "Filter:"
    hj::assert_contains "${frame}" "Select at least one file"
    hj::assert_order "${frame}" "SourceMix." "Select at least one file"

    hj::send_keys "${target}" Space
    cleared_frame=$(hj::wait_for_frame_contains "${target}" "+ File00.cs")

    hj::assert_occurrences "${cleared_frame}" "SourceMix." 1
    if grep -Fq "Select at least one file" <<<"${cleared_frame}"; then
        hj::fail "expected the validation message to disappear once a file is selected." "${cleared_frame}"
    fi

    printf 'ok  %s\n' "files-step validation keeps the app header visible"
}

hj::run_tab_transition_scenario() {
    local workspace
    local session
    local target
    local frame

    workspace=$(hj::new_workspace 2)
    session=$(hj::start_tui "${workspace}" 80 24)
    target="${session}:0.0"

    hj::wait_for_frame_contains "${target}" "Filter:" >/dev/null
    hj::send_keys "${target}" Space
    hj::send_keys "${target}" Tab
    hj::send_keys "${target}" Tab
    frame=$(hj::wait_for_frame_contains "${target}" "File00.cs")

    hj::assert_occurrences "${frame}" "SourceMix." 1
    hj::assert_contains "${frame}" "Search  Pinned  Selected"
    hj::assert_contains "${frame}" "File00.cs"

    printf 'ok  %s\n' "tab view changes redraw the file picker in a real terminal"
}

hj::run_backtrack_selection_scenario() {
    local workspace
    local session
    local target
    local frame

    workspace=$(hj::new_workspace 3)
    session=$(hj::start_tui "${workspace}" 80 24)
    target="${session}:0.0"

    hj::wait_for_frame_contains "${target}" "Filter:" >/dev/null
    hj::send_keys "${target}" Down
    hj::send_keys "${target}" Space
    hj::send_keys "${target}" Enter
    hj::send_keys "${target}" C-q
    frame=$(hj::wait_for_frame_contains "${target}" "+ File01.cs")

    hj::assert_occurrences "${frame}" "SourceMix." 1
    hj::assert_contains "${frame}" "Search  Pinned  Selected"
    hj::assert_contains "${frame}" "+ File01.cs"

    printf 'ok  %s\n' "backtracking to Files keeps the prior file selection visible"
}

hj::run_resize_paging_scenario() {
    local workspace
    local session
    local target
    local frame

    workspace=$(hj::new_workspace 30)
    session=$(hj::start_tui "${workspace}" 80 24)
    target="${session}:0.0"

    hj::wait_for_frame_contains "${target}" "more below" >/dev/null
    hj::resize_window "${session}" 80 12
    frame=$(hj::wait_for_frame_contains "${target}" "more below")

    hj::assert_occurrences "${frame}" "SourceMix." 1
    hj::assert_contains "${frame}" "Filter:"
    hj::assert_contains "${frame}" "more below"
    hj::assert_order "${frame}" "SourceMix." "more below"

    printf 'ok  %s\n' "height resize keeps the header and paging hints stable"
}

hj::run_output_prompt_resize_scenario() {
    local workspace
    local session
    local target
    local frame
    local output_path

    workspace=$(hj::new_workspace 1)
    session=$(hj::start_tui "${workspace}" 40 24)
    target="${session}:0.0"
    output_path="${workspace}/home-wrap-for-terminal-tests/sourcemix.md2"

    hj::wait_for_frame_contains "${target}" "Filter:" >/dev/null
    hj::send_keys "${target}" Space
    hj::send_keys "${target}" Enter
    hj::send_keys "${target}" Enter
    hj::wait_for_frame_contains "${target}" "Output file:" >/dev/null
    hj::send_text "${target}" "2"
    hj::resize_window "${session}" 40 12
    frame=$(hj::wait_for_frame_contains "${target}" "sourcemix.md2")

    hj::assert_occurrences "${frame}" "SourceMix." 1
    hj::assert_occurrences "${frame}" "Output file:" 1
    hj::assert_contains "${frame}" "sourcemix.md2"

    hj::send_keys "${target}" Enter
    hj::send_keys "${target}" Enter
    hj::wait_for_file "${output_path}"

    if ! grep -Fq "public class File00" "${output_path}"; then
        hj::fail "expected generated markdown output to contain the selected source file."
    fi

    printf 'ok  %s\n' "wrapped output-path editing survives a real terminal redraw"
}

hj::main() {
    hj::ensure_prerequisites

    hj::heading 2 "tmux TUI smoke tests"

    hj::run_validation_header_scenario
    hj::run_tab_transition_scenario
    hj::run_backtrack_selection_scenario
    hj::run_resize_paging_scenario
    hj::run_output_prompt_resize_scenario
}

hj::main "$@"
