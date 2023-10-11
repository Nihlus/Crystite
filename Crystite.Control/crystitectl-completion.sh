function _crystitectl_completions()
{
  local -ra _CRYSTITECTL_VERBS=(
    "accept-friend"
    "ban"
    "ban-from"
    "block-user"
    "close-world"
    "focus-world"
    "ignore-user"
    "kick-from"
    "modify-world"
    "reject-friend"
    "respawn"
    "restart-world"
    "save-world"
    "set-role"
    "show-banned"
    "show-blocked"
    "show-contacts"
    "show-friends"
    "show-ignored"
    "show-requested"
    "show-users"
    "show-world"
    "show-worlds"
    "start-world"
    "unban"
  )

  local -rA _CRYSTITECTL_VERB_TYPES=(
    ["accept-friend"]="GlobalUser"
    ["ban"]="GlobalUser"
    ["ban-from"]="WorldUser"
    ["block-user"]="GlobalUser"
    ["close-world"]="World"
    ["focus-world"]="World"
    ["ignore-user"]="GlobalUser"
    ["kick-from"]="WorldUser"
    ["modify-world"]="World"
    ["reject-friend"]="GlobalUser"
    ["respawn"]="WorldUser"
    ["restart-world"]="World"
    ["save-world"]="World"
    ["set-role"]="WorldUser"
    ["show-banned"]="General"
    ["show-blocked"]="General"
    ["show-contacts"]="General"
    ["show-friends"]="General"
    ["show-ignored"]="General"
    ["show-requested"]="General"
    ["show-users"]="World"
    ["show-world"]="World"
    ["show-worlds"]="General"
    ["start-world"]="General"
    ["unban"]="GlobalUser"
  )

  local -ra _CRYSTITECTL_GLOBALUSER_OPTS=("--name" "--id")
  local -ra _CRYSTITECTL_WORLD_OPTS=("--name" "--id")
  local -ra _CRYSTITECTL_WORLDUSER_OPTS=("--name" "--id" "--user-id" "--user-name")

  local -rA _CRYSTITECTL_VERB_OPTS=(
    ["General"]=""
    ["GlobalUser"]=${_CRYSTITECTL_GLOBALUSER_OPTS[@]}
    ["World"]=${_CRYSTITECTL_WORLD_OPTS[@]}
    ["WorldUser"]=${_CRYSTITECTL_WORLDUSER_OPTS[@]}
  )

  local -ra _CRYSTITECTL_COMMON_OPTS=(
    "--port"
    "--server"
    "--output-format"
  )

  local -ra _CRYSTITECTL_SET_ROLE_FLAGS=("--role")
  local -ra _CRYSTITECTL_MODIFY_WORLD_FLAGS=("--new-name" "--description" "--access-level" "--away-kick-interval" "--hide-from-listing" "--max-users")
  local -ra _CRYSTITECTL_START_WORLD_FLAGS=("--template" "--url")
  local -rA _CRYSTITECTL_VERB_FLAGS=(
    ["set-role"]=${_CRYSTITECTL_SET_ROLE_FLAGS[@]}
    ["modify-world"]=${_CRYSTITECTL_MODIFY_WORLD_FLAGS[@]}
    ["start-world"]=${_CRYSTITECTL_START_WORLD_FLAGS[@]}
  )

  local -ra _CRYSTITECTL_OUTPUT_FORMAT_VALUES=("Simple" "Verbose" "Json")
  local -ra _CRYSTITECTL_ROLE_VALUES=("Admin" "Builder" "Moderator" "Guest")
  local -ra _CRYSTITECTL_ACCESS_LEVEL_VALUES=("Private" "LAN" "Contacts" "ContactsPlus" "RegisteredUsers" "Anyone")
  local -rA _CRYSTITECTL_CRYSTITECTL_FLAG_VALUES=(
    ["--output-format"]=${_CRYSTITECTL_OUTPUT_FORMAT_VALUES[@]}
    ["--role"]=${_CRYSTITECTL_ROLE_VALUES[@]}
    ["--access-level"]=${_CRYSTITECTL_ACCESS_LEVEL_VALUES[@]}
  )

  if ((${#COMP_WORDS[@]} - 1 < 2 )); then
    mapfile -t COMPREPLY < <(compgen -W "${_CRYSTITECTL_VERBS[*]}" "'${COMP_WORDS[1]}'")
    return
  fi

  local current_word="${COMP_WORDS[${COMP_CWORD}]}"

  local active_flag=${COMP_WORDS[$((COMP_CWORD - 1))]}
  if [[ -n "${_CRYSTITECTL_CRYSTITECTL_FLAG_VALUES[${active_flag}]}" ]]; then
    # suggest flag values
    mapfile -t COMPREPLY < <(compgen -W "${_CRYSTITECTL_CRYSTITECTL_FLAG_VALUES[${active_flag}]}" "'${current_word}'")
    return
  else
    # not a completable flag; suggest other flags
    local -r filter_patterns="$(printf '%s|' "${COMP_WORDS[@]}")"

    # common options
    mapfile -t -O ${#COMPREPLY[@]} COMPREPLY \
      < <(compgen -X "@(${filter_patterns})" -W "${_CRYSTITECTL_COMMON_OPTS[*]}" "'${current_word}'")

    local -r verb="${COMP_WORDS[1]}"

    # verb type options
    local verb_type=${_CRYSTITECTL_VERB_TYPES[${verb}]}
    local verb_type_opts=${_CRYSTITECTL_VERB_OPTS[${verb_type}]}
    mapfile -t -O ${#COMPREPLY[@]} COMPREPLY \
      < <(compgen -X "@(${filter_patterns})" -W "${verb_type_opts[*]}" "'${current_word}'")

    # verb-specific options
    if [[ -n "${_CRYSTITECTL_VERB_FLAGS[${verb}]}" ]]; then
      mapfile -t -O ${#COMPREPLY[@]} COMPREPLY \
        < <(compgen -X "@(${filter_patterns})" -W "${_CRYSTITECTL_VERB_FLAGS[${verb}]}" "'${current_word}'")
    fi
  fi
}

complete -F _crystitectl_completions crystitectl
