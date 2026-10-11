#!/usr/bin/env bash
set -euo pipefail

usage() {
    printf 'Usage: %s <branch-or-tag> [additional-refs...]\n' "$0" >&2
}

if [ "$#" -lt 1 ]; then
    usage
    exit 2
fi

command -v gh >/dev/null 2>&1 || {
    printf 'error: GitHub CLI (gh) is required\n' >&2
    exit 1
}

original_account="$(gh api user --jq .login)"
restore_account() {
    gh auth switch --hostname github.com --user "$original_account" >/dev/null 2>&1 || true
}
trap restore_account EXIT

gh auth switch --hostname github.com --user clickbytes >/dev/null
git push clickbytes "$@"
gh auth switch --hostname github.com --user ppotepa >/dev/null
git push origin "$@"
