#!/bin/sh

BASE_URL="http://localhost:8083"
SERVER_CMD="bun run drone-simulation-server.js"
SESSION="drone-sim"
LOG_FILE="/tmp/drone_sim_$(date +'%Y%m%d_%H%M%S').log"

ACTIVE_DRONE=""
DRONES=""   # space-separated list

# ---------------- Dependency check ----------------
require() {
  command -v "$1" >/dev/null 2>&1 || {
    echo "Missing dependency: $1"
    exit 1
  }
}

require bun
require jq
require tmux
require curl

# ---------------- Colors ----------------
RED="\033[0;31m"
GREEN="\033[0;32m"
YELLOW="\033[0;33m"
BLUE="\033[0;34m"
MAGENTA="\033[0;35m"
CYAN="\033[0;36m"
BOLD="\033[1m"
RESET="\033[0m"

# ---------------- Logging ----------------
log() {
  echo -e "${CYAN}[$(date +'%H:%M:%S')]${RESET} $*" >> "$LOG_FILE"
}

show_log() {
  tail -n 10 "$LOG_FILE" | while read line; do
    echo -e "$line"
  done
}

# ---------------- Loading animation ----------------
loading() {
  printf "Loading"
  for i in 1 2 3; do
    sleep 0.3
    printf "."
  done
  echo
}

# ---------------- Drone commands ----------------
create_drone() {
  printf "New drone ID (ESC to cancel): "
  IFS= read -rsn1 id
  if [[ $id == $'\e' ]]; then
    echo
    return
  fi

  read -r rest
  id+="$rest"
  [ -z "$id" ] && return

  curl -s -X POST "$BASE_URL/drones" \
    -H "Content-Type: application/json" \
    -d "{\"dboidsID\":\"$id\"}" | jq .

  DRONES="$DRONES $id"
  ACTIVE_DRONE="$id"
  log "Created drone $id"

  if command -v websocat >/dev/null 2>&1; then
    printf "Open WS monitor pane for %s? [y/N] " "$id"
    read ans
    [ "$ans" = "y" ] && add_ws_pane "$id"
  fi
}


load_drones() {
  loading
  json=$(curl -s "$BASE_URL/drones")
  DRONES=$(echo "$json" | jq -r '.[].id' | tr '\n' ' ')

  if [ -z "$DRONES" ]; then
    ACTIVE_DRONE=""
    DRONES=""
    echo "No drones found on server."
    log "No drones found on server."
  else
    ACTIVE_DRONE=$(echo $DRONES | awk '{print $1}')
    echo "Drones on server:"
    echo "$json" | jq .
    log "Loaded drones from server: $DRONES"
  fi
}

select_drone() {
  if [ -z "$DRONES" ]; then
    echo "No drones available to select."
    return
  fi

  echo
  echo "Available drones:"
  i=1
  for d in $DRONES; do
    echo "  $i) $d"
    i=$((i + 1))
  done

  printf "Select (ESC to cancel): "
  IFS= read -rsn1 idx
  if [[ $idx == $'\e' ]]; then
    echo
    return
  fi

  read -r rest
  idx+="$rest"

  i=1
  for d in $DRONES; do
    if [ "$i" = "$idx" ]; then
      ACTIVE_DRONE="$d"
      log "Selected active drone $d"
      return
    fi
    i=$((i + 1))
  done

  echo "Invalid selection"
}


start_flight() {
  [ -z "$ACTIVE_DRONE" ] && { echo "No active drone"; return; }
  curl -s -X POST "$BASE_URL/drones/$ACTIVE_DRONE/start" | jq .
  log "Started flight for $ACTIVE_DRONE"
}

finish_flight() {
  [ -z "$ACTIVE_DRONE" ] && { echo "No active drone"; return; }
  curl -s -X POST "$BASE_URL/drones/$ACTIVE_DRONE/finish" | jq .
  log "Finished flight for $ACTIVE_DRONE"
}

delete_drone() {
  [ -z "$ACTIVE_DRONE" ] && { echo "No active drone"; return; }
  curl -s -X DELETE "$BASE_URL/drones/$ACTIVE_DRONE" -w "\nHTTP %{http_code}\n"
  log "Deleted drone $ACTIVE_DRONE"
  DRONES=$(echo "$DRONES" | sed "s/\b$ACTIVE_DRONE\b//g")
  ACTIVE_DRONE=""
}

# ---------------- Menu loop ----------------
menu_loop() {
  load_drones
  while true; do
    clear
    echo -e "${BOLD}${YELLOW}Drone Simulator Control${RESET}"
    echo
    echo -e "${YELLOW}Recent activity${RESET}"
    show_log
    echo
    echo -e "${YELLOW}Drone IDs${RESET}"
    echo -e "Active: ${GREEN}${ACTIVE_DRONE:-none}${RESET}"
    echo -e "All:    ${BLUE}${DRONES:-none}${RESET}"
    echo
    echo -e "${BOLD}1)${RESET} Create drone"
    echo -e "${BOLD}2)${RESET} Select active drone"
    echo -e "${BOLD}3)${RESET} Load drones"
    echo -e "${BOLD}4)${RESET} Start flight (active)"
    echo -e "${BOLD}5)${RESET} Finish flight (active)"
    echo -e "${BOLD}6)${RESET} Delete drone (active)"
    echo -e "${BOLD}0)${RESET} Exit"
    echo
    echo -ne "Choice: "

    IFS= read -rsn1 choice   # -r raw, -s silent (no echo), -n1 one char
    if [[ $choice == $'\e' ]]; then
      continue  # Escape pressed, go back to menu
    fi
    echo

    case "$choice" in
      1) create_drone ;;
      2) select_drone ;;
      3) load_drones ;;
      4) start_flight ;;
      5) finish_flight ;;
      6) delete_drone ;;
      0) exit 0 ;;
      *) 
        echo -e "${RED}Invalid option${RESET}"
        sleep 1
        ;;
    esac

    echo
    echo -e "Press any key to continue..."
    IFS= read -r -n1
  done
}


# ---------------- WS monitor pane ----------------
add_ws_pane() {
  id="$1"
  left_pane=$(tmux list-panes -t "$SESSION" -F "#{pane_id}" | head -n1)
  new_pane=$(tmux split-window -v -P -F "#{pane_id}" -t "$left_pane")
  tmux send-keys -t "$new_pane" "websocat ws://localhost:8080?dboidsID=$id | jq ." C-m
}

# ---------------- tmux setup ----------------
start_tmux() {
  if tmux has-session -t "$SESSION" 2>/dev/null; then
    tmux attach -t "$SESSION"
    exit 0
  fi

  tmux new-session -d -s "$SESSION"
  sleep 0.2
  left_pane=$(tmux list-panes -t "$SESSION" -F "#{pane_id}" | head -n1)
  tmux send-keys -t "$left_pane" "sh $0 --inside-tmux" C-m

  right_pane=$(tmux split-window -h -P -t "$SESSION")
  tmux send-keys -t "$right_pane" "$SERVER_CMD" C-m

  tmux select-pane -t "$left_pane"
  tmux attach -t "$SESSION"
}

# ---------------- Entry point ----------------
if [ "$1" = "--inside-tmux" ]; then
  menu_loop
else
  start_tmux
fi
