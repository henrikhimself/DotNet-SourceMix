#!/usr/bin/env bash

hj::heading() {
    local level="$1"
    local content="$2"
    local prefix

    if ! [[ "${level}" =~ ^[1-9][0-9]*$ ]]; then
        level=1
    fi

    printf -v prefix '%*s' "${level}" ''
    prefix="${prefix// /#}"

    printf '%s %s\n' "${prefix}" "${content}"
}

hj::text() {
    local content="$1"

    printf '%s\n\n' "${content}"
}

hj::code() {
    local syntax_id="$1"
    local content="$2"

    printf '```%s\n' "${syntax_id}"
    printf '%s\n' "${content}"
    printf '```\n\n'
}
