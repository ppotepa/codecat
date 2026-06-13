#!/usr/bin/env bash
set -euo pipefail

usage() {
    cat <<'USAGE'
Usage: scripts/install-linux.sh [--install|--uninstall] [--user|--global] [options]

Build and install codecat on Linux.
Run without arguments to use the interactive menu.

Actions:
  --install          Build and install codecat
  --uninstall        Remove an installed codecat binary

Modes:
  --user             Use ~/.local/bin/codecat
  --global           Use /usr/local/bin/codecat

Options:
  --install-dir DIR  Override the target directory
  --runtime RID      .NET runtime identifier (default: linux-x64)
  --version VERSION  Version to publish (default: value from Codecat.csproj)
  --no-modify-profile
                     Do not add the user install directory to a shell profile
  --help             Show this help

Examples:
  scripts/install-linux.sh
  scripts/install-linux.sh --user
  scripts/install-linux.sh --global
  scripts/install-linux.sh --uninstall --user
  scripts/install-linux.sh --uninstall --global
  scripts/install-linux.sh --user --install-dir "$HOME/bin"
USAGE
}

fail() {
    printf 'error: %s\n' "$*" >&2
    exit 1
}

shell_quote() {
    printf "'%s'" "$(printf '%s' "$1" | sed "s/'/'\\\\''/g")"
}

profile_path_for_shell() {
    shell_name="$(basename -- "${SHELL:-}")"
    case "$shell_name" in
        zsh)
            printf '%s/.zshrc\n' "$HOME"
            ;;
        bash)
            printf '%s/.bashrc\n' "$HOME"
            ;;
        *)
            if [ -f "$HOME/.profile" ]; then
                printf '%s/.profile\n' "$HOME"
            else
                printf '%s/.bashrc\n' "$HOME"
            fi
            ;;
    esac
}

add_install_dir_to_profile() {
    [ -n "${HOME:-}" ] || return 0

    profile_path="$(profile_path_for_shell)"
    marker="codecat installer: $install_dir"

    if [ -f "$profile_path" ] && grep -Fq "$marker" "$profile_path"; then
        printf 'PATH entry already present in %s\n' "$profile_path"
        return 0
    fi

    install_dir_literal="$(shell_quote "$install_dir")"
    path_literal="\$PATH"
    mkdir -p -- "$(dirname -- "$profile_path")"
    {
        printf '\n# %s\n' "$marker"
        printf 'case ":%s:" in\n' "$path_literal"
        printf '    *:%s:*) ;;\n' "$install_dir_literal"
        printf '    *) export PATH=%s:"%s" ;;\n' "$install_dir_literal" "$path_literal"
        printf 'esac\n'
    } >> "$profile_path"

    printf 'Added %s to PATH in %s\n' "$install_dir" "$profile_path"
    printf 'Open a new terminal or run: . %s\n' "$profile_path"
}

remove_install_dir_from_profile() {
    [ -n "${HOME:-}" ] || return 0

    profile_path="$(profile_path_for_shell)"
    marker="# codecat installer: $install_dir"

    if [ ! -f "$profile_path" ] || ! grep -Fq "$marker" "$profile_path"; then
        return 0
    fi

    tmp_path="$(mktemp)"
    awk -v marker="$marker" '
        $0 == marker { skip = 1; next }
        skip && $0 == "esac" { skip = 0; next }
        skip { next }
        { print }
    ' "$profile_path" > "$tmp_path"
    cat "$tmp_path" > "$profile_path"
    rm -f -- "$tmp_path"

    printf 'Removed %s PATH entry from %s\n' "$install_dir" "$profile_path"
}

default_install_dir() {
    case "$1" in
        user)
            [ -n "${HOME:-}" ] || fail "HOME is not set; use --install-dir"
            printf '%s/.local/bin\n' "$HOME"
            ;;
        global)
            printf '/usr/local/bin\n'
            ;;
        *)
            fail "invalid install mode: $1"
            ;;
    esac
}

target_for_mode() {
    mode_for_target="$1"
    install_dir_for_target="$2"

    if [ -z "$install_dir_for_target" ]; then
        install_dir_for_target="$(default_install_dir "$mode_for_target")"
    fi

    printf '%s/codecat\n' "$install_dir_for_target"
}

print_install_status() {
    user_target="$(target_for_mode user "")"
    global_target="$(target_for_mode global "")"

    printf 'Detected installations:\n'
    if [ -x "$user_target" ]; then
        printf '  user:   %s\n' "$user_target"
    else
        printf '  user:   not installed (%s)\n' "$user_target"
    fi

    if [ -x "$global_target" ]; then
        printf '  global: %s\n' "$global_target"
    else
        printf '  global: not installed (%s)\n' "$global_target"
    fi

    if command -v codecat >/dev/null 2>&1; then
        printf '  PATH:   %s\n' "$(command -v codecat)"
    else
        printf '  PATH:   codecat not found\n'
    fi
}

choose_from_menu() {
    print_install_status
    printf '\nWhat do you want to do?\n'
    printf '  1) Install for current user\n'
    printf '  2) Install globally\n'
    printf '  3) Uninstall for current user\n'
    printf '  4) Uninstall globally\n'
    printf '  5) Exit\n'
    printf 'Choose [1-5]: '
    read -r choice

    case "$choice" in
        1)
            action="install"
            mode="user"
            ;;
        2)
            action="install"
            mode="global"
            ;;
        3)
            action="uninstall"
            mode="user"
            ;;
        4)
            action="uninstall"
            mode="global"
            ;;
        5|"")
            exit 0
            ;;
        *)
            fail "invalid menu choice: $choice"
            ;;
    esac
}

install_codecat() {
    command -v dotnet >/dev/null 2>&1 || fail "dotnet SDK is required to build codecat"
    command -v install >/dev/null 2>&1 || fail "GNU install is required"

    publish_dir="$repo_root/artifacts/install/codecat-$version-$runtime"
    binary_path="$publish_dir/Codecat"
    target_path="$install_dir/codecat"

    printf 'Publishing codecat %s for %s...\n' "$version" "$runtime"
    rm -rf -- "$publish_dir"
    dotnet restore "$repo_root/Codecat.slnx"
    dotnet publish "$project_path" \
        --configuration Release \
        --runtime "$runtime" \
        --self-contained true \
        -p:Version="$version" \
        -p:PublishAot=true \
        -p:DebugType=none \
        -p:DebugSymbols=false \
        -p:PublishDir="$publish_dir/"

    [ -x "$binary_path" ] || fail "published executable not found: $binary_path"

    printf 'Installing to %s...\n' "$target_path"
    if [ "$mode" = "global" ] && [ "${EUID:-$(id -u)}" -ne 0 ]; then
        command -v sudo >/dev/null 2>&1 || fail "sudo is required for --global when not running as root"
        sudo install -Dm755 "$binary_path" "$target_path"
    else
        install -Dm755 "$binary_path" "$target_path"
    fi

    printf 'Installed: %s\n' "$target_path"

    case ":${PATH:-}:" in
        *":$install_dir:"*) ;;
        *)
            if [ "$mode" = "user" ] && [ "$modify_profile" = "true" ]; then
                add_install_dir_to_profile
            elif [ "$mode" = "user" ]; then
                printf 'Note: %s is not on PATH. Add it to your shell profile to run codecat from any directory.\n' "$install_dir"
            fi
            ;;
    esac

    "$target_path" --version
}

uninstall_codecat() {
    target_path="$install_dir/codecat"

    if [ ! -e "$target_path" ]; then
        printf 'Not installed: %s\n' "$target_path"
    elif [ "$mode" = "global" ] && [ "${EUID:-$(id -u)}" -ne 0 ]; then
        command -v sudo >/dev/null 2>&1 || fail "sudo is required for --global when not running as root"
        sudo rm -f -- "$target_path"
        printf 'Removed: %s\n' "$target_path"
    else
        rm -f -- "$target_path"
        printf 'Removed: %s\n' "$target_path"
    fi

    if [ "$mode" = "user" ] && [ "$modify_profile" = "true" ]; then
        remove_install_dir_from_profile
    fi
}

script_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd -- "$script_dir/.." && pwd)"
project_path="$repo_root/src/Codecat/Codecat.csproj"

mode="user"
install_dir=""
runtime="linux-x64"
version=""
modify_profile="true"
action=""
menu_requested="true"

while [ "$#" -gt 0 ]; do
    case "$1" in
        --install)
            action="install"
            menu_requested="false"
            ;;
        --uninstall)
            action="uninstall"
            menu_requested="false"
            ;;
        --user)
            mode="user"
            menu_requested="false"
            ;;
        --global)
            mode="global"
            menu_requested="false"
            ;;
        --install-dir)
            [ "$#" -ge 2 ] || fail "--install-dir requires a value"
            install_dir="$2"
            menu_requested="false"
            shift
            ;;
        --runtime)
            [ "$#" -ge 2 ] || fail "--runtime requires a value"
            runtime="$2"
            shift
            ;;
        --version)
            [ "$#" -ge 2 ] || fail "--version requires a value"
            version="$2"
            shift
            ;;
        --no-modify-profile)
            modify_profile="false"
            ;;
        --help|-h)
            usage
            exit 0
            ;;
        *)
            fail "unknown argument: $1"
            ;;
    esac
    shift
done

[ -f "$project_path" ] || fail "project file not found: $project_path"

if [ -z "$version" ]; then
    version="$(sed -n 's:.*<Version>\(.*\)</Version>.*:\1:p' "$project_path" | head -n 1)"
    [ -n "$version" ] || fail "could not read <Version> from $project_path"
fi

if [ "$menu_requested" = "true" ]; then
    choose_from_menu
fi

if [ -z "$action" ]; then
    action="install"
fi

if [ -z "$install_dir" ]; then
    install_dir="$(default_install_dir "$mode")"
fi

case "$action" in
    install)
        install_codecat
        ;;
    uninstall)
        uninstall_codecat
        ;;
    *)
        fail "invalid action: $action"
        ;;
esac
