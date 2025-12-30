#!/bin/sh
# Launch a tmux session with three windows:
#  - backend: runs the dTITAN backend
#  - simulator: left pane runs the simulator menu, right pane runs the simulator server
#  - lazydocker: runs lazydocker in repo root

SESSION="dTITAN"
BACKEND_CMD="cd dTITAN.Backend && dotnet run"
SIMULATOR_CMD="cd simulator && ./tui-drone-simulation.js"
CLIENT_CMD="cd simulator && ./tui-client-sender.js"
DOCKER_CMD="docker compose -f docker-compose.dev.yml up -d && lazydocker"

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

# Split vertically: top = sims, bottom = backend
tmux split-window -v -l 5 -t "$SESSION:1.1"

tmux send-keys -t "$SESSION:1.1" "$SIMULATOR_CMD" C-m
tmux send-keys -t "$SESSION:1.2" "$BACKEND_CMD" C-m

# Split horizontally: left = drone sim, right = client sim
tmux split-window -h -l 30 -t "$SESSION:1.1"
tmux send-keys -t "$SESSION:1.2" "$CLIENT_CMD" C-m

tmux select-pane -t "$SESSION:1.2"

# ---------------- Window 2: LazyDocker ----------------
tmux new-window -t "$SESSION:2" -n "LazyDocker"
tmux send-keys -t "$SESSION:2.1" "$DOCKER_CMD" C-m

# ---------------- Window 3: Terminal ----------------
tmux new-window -t "$SESSION:3" -n "Terminal"

# ---------------- Attach session ----------------
tmux select-window -t "$SESSION:1"
tmux attach -t "$SESSION"
