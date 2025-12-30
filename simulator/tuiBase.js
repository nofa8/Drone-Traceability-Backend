import readline from "readline";

// ---------------- State ----------------
export const state = {
  lockRender: false,
  serverMessages: [],
  messageScroll: 0,
  renderFn: () => {}, // assigned by client
};

// ---------------- Render scheduling ----------------
let needsRender = true;
let renderScheduled = false;

export function requestRender() {
  needsRender = true;
  if (renderScheduled) return;

  renderScheduled = true;
  setImmediate(() => {
    renderScheduled = false;
    if (needsRender) {
      needsRender = false;
      if (typeof state.renderFn === "function") state.renderFn();
    }
  });
}

// ---------------- Set render ----------------
export function setRender(fn) {
  state.renderFn = fn;
}

// ---------------- Message helpers ----------------
export function getMessageVisibleCount(count = 3) {
  return count; // default: show last 3 messages
}

export function pushServerMessage(msg) {
  const text = typeof msg === "string" ? msg : JSON.stringify(msg);
  state.serverMessages.push({ time: new Date().toISOString(), text });
  if (state.serverMessages.length > 200) state.serverMessages.shift();

  const visible = getMessageVisibleCount();
  state.messageScroll = Math.max(0, state.serverMessages.length - visible);

  requestRender();
}

export function formatServerText(text, maxWidth) {
  let out = text;
  try {
    const obj = JSON.parse(text);
    if (obj.eventType) {
      const et = obj.eventType;
      const payload = obj.payload || obj.Payload || {};
      const id = payload.droneId || payload.userId;

      if (payload.telemetry) {
        const t = payload.telemetry;
        const alt = t.altitude ?? t.alt ?? t.altitudeMeters ?? null;
        const bat = t.batteryLevel ?? t.batLvl ?? null;
        out = `${et}${id ? " " + id : ""}${alt != null ? " alt:" + alt : ""}${bat != null ? " bat:" + Math.round(bat) : ""}`;
      } else if (payload.command) {
        out = `${et} ${payload.command}${id ? " " + id : ""}`;
      } else {
        out = `${et}${id ? " " + id : ""}`;
      }
    } else if (obj.command) {
      out = `cmd ${obj.command}`;
    } else {
      out = JSON.stringify(obj);
    }
  } catch {
    out = text.replace(/\s+/g, " ");
  }

  if (!maxWidth || out.length <= maxWidth) return out;
  return out.slice(0, maxWidth - 1) + "…";
}

// ---------------- Safe readline ----------------
export async function readLine(prompt) {
  state.lockRender = true;

  process.stdin.pause();
  try { process.stdin.setRawMode(false); } catch {}
  await new Promise(r => setImmediate(r));

  return new Promise(resolve => {
    const rl = readline.createInterface({
      input: process.stdin,
      output: process.stdout,
    });

    rl.question(prompt, answer => {
      rl.close();

      try { process.stdin.setRawMode(true); } catch {}
      process.stdin.resume();
      state.lockRender = false;

      const trimmed = (answer || "").trim();
      resolve(trimmed.length === 0 ? null : trimmed);

      requestRender();
    });
  });
}

// ---------------- Keyboard setup helper ----------------
export function setupKeyboard(keyHandler) {
  process.stdin.setRawMode(true);
  process.stdin.resume();

  process.stdin.on("data", async (data) => {
    if (state.lockRender) return;
    await keyHandler(data.toString());
    requestRender();
  });

  process.on("SIGINT", () => {
    process.exit(0);
  });
}
