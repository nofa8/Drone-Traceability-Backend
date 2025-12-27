#!/bin/sh
# Launch a tmux session with three windows:
#  - backend: runs the dTITAN backend
#  - simulator: left pane runs the simulator menu, right pane runs the simulator server
#  - lazydocker: runs lazydocker in repo root

SESSION="dTITAN"
BACKEND_CMD="cd dTITAN.Backend && dotnet run"
SIMULATOR_CMD="cd dev-tools/simulator && ./tui-drone-simulation.js"
DEVTOOLS_CMD="cd dev-tools"

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

# Split horizontally: left = backend, right = simulator + devtools
tmux split-window -h -l 20 -t "$SESSION:1.1"

# Send backend command to left pane (1.1)
tmux send-keys -t "$SESSION:1.1" "$BACKEND_CMD" C-m

# Split right pane vertically: top = simulator, bottom = devtools
tmux split-window -v -l 10 -t "$SESSION:1.2"
tmux send-keys -t "$SESSION:1.2" "$SIMULATOR_CMD" C-m
tmux send-keys -t "$SESSION:1.3" "$DEVTOOLS_CMD" C-m

# Optional: select right-top pane (simulator) by default
tmux select-pane -t "$SESSION:1.2"


# ---------------- Window 2: LazyDocker ----------------
tmux new-window -t "$SESSION:2" -n "LazyDocker"
tmux send-keys -t "$SESSION:2.1" "docker compose up -d && lazydocker" C-m

# ---------------- Window 3: Terminal ----------------
tmux new-window -t "$SESSION:3" -n "Terminal"

# ---------------- Attach session ----------------
tmux select-window -t "$SESSION:1"
tmux attach -t "$SESSION"
