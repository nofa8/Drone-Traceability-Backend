#!/bin/sh
# Launch a tmux session with three windows:
#  - backend: runs the dTITAN backend
#  - simulator: left pane runs the simulator menu, right pane runs the simulator server
#  - lazydocker: runs lazydocker in repo root

SESSION="dTITAN"
BACKEND_CMD="cd dTITAN.Backend && dotnet run"
WEBSOCKETS_CMD="cd simulator && bun run drone-simulation-server.js"
SIMULATOR_CMD="cd simulator && ./run-sim.sh"

# ---------------- Check dependencies ----------------
for cmd in tmux lazydocker; do
    command -v $cmd >/dev/null 2>&1 || { echo "$cmd not found, install it first."; exit 1; }
done

# ---------------- Create tmux session ----------------
if tmux has-session -t "$SESSION" 2>/dev/null; then
    echo "Session '$SESSION' already exists. Attaching..."
    tmux attach -t "$SESSION"
    exit 0
fi

# ---------------- Window 1: Horizontal split first ----------------
tmux new-session -d -s "$SESSION" -n "Workspace"

# Split horizontally: left = left pane (backend + websocket), right = simulator
tmux split-window -h -l 20 -t "$SESSION:1.1"
tmux send-keys -t "$SESSION:1.2" "$SIMULATOR_CMD" C-m

# Split left pane vertically: top = backend, bottom = websocket
tmux split-window -v -l 4 -t "$SESSION:1.1"
tmux send-keys -t "$SESSION:1.1" "$BACKEND_CMD" C-m
tmux send-keys -t "$SESSION:1.2" "$WEBSOCKETS_CMD" C-m  # new bottom-left pane

# Optional: select left-top pane (backend) by default
tmux select-pane -t "$SESSION:1.1"

# ---------------- Window 2: LazyDocker ----------------
tmux new-window -t "$SESSION:2" -n "LazyDocker"
tmux send-keys -t "$SESSION:2.1" "docker compose up -d && lazydocker" C-m

# ---------------- Window 3: Terminal ----------------
tmux new-window -t "$SESSION:3" -n "Terminal"
tmux send-keys -t "$SESSION:3.1" "bash" C-m

# ---------------- Attach session ----------------
tmux select-window -t "$SESSION:1"
tmux attach -t "$SESSION"
