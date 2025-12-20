#!/bin/sh

BASE_URL="http://localhost:8080"
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

# ---------------- Logging ----------------
log() {
  echo "[$(date +'%H:%M:%S')] $*" >> "$LOG_FILE"
}

show_log() {
  tail -n 10 "$LOG_FILE"
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
  printf "New drone ID: "
  read id
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

  printf "Select: "
  read idx

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
    echo "=== Drone Simulator Control (multi-drone) ==="
    echo "---- Recent activity ----"
    show_log
    echo "-------------------------"
    echo
    echo "Active drone: ${ACTIVE_DRONE:-none}"
    echo "All drones:   ${DRONES:-none}"
    echo
    echo "1) Create drone"
    echo "2) Select active drone"
    echo "3) Load drones"
    echo "4) Start flight (active)"
    echo "5) Finish flight (active)"
    echo "6) Delete drone (active)"
    echo "0) Exit"
    echo
    echo -n "Choice: "

    IFS= read -r -n1 choice
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
        echo "Invalid option"
        sleep 1
        ;;
    esac

    echo
    echo "Press any key to continue..."
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
