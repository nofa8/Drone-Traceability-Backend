#!/usr/bin/env bun

import WebSocket from "ws";
import {
  state,
  pushServerMessage,
  requestRender,
  readLine,
  setupKeyboard,
  formatServerText,
  setRender
} from "./tuiBase.js";

const WS_URL = "ws://localhost:5102";
let vehicleId = "RD001";
let ws = null;

// ---------------- WebSocket ----------------
function connect() {
  if (ws && ws.readyState === WebSocket.OPEN) return;
  if (ws) try { ws.close(); } catch {}

  ws = new WebSocket(WS_URL);

  ws.on("open", requestRender);
  ws.on("close", requestRender);
  ws.on("error", (err) => console.log("WS error:", err?.message ?? err));
  ws.on("message", (msg) => {
    pushServerMessage(msg.toString());
  });
}

connect();

// ---------------- Render ----------------
function render() {
  if (state.lockRender) return;

  console.clear();
  console.log("Client Command TUI\n");

  if (state.serverMessages.length > 0) {
    const visible = 3;
    const maxStart = Math.max(0, state.serverMessages.length - visible);
    if (state.messageScroll > maxStart) state.messageScroll = maxStart;

    const start = Math.max(0, Math.min(state.messageScroll, maxStart));
    const slice = state.serverMessages.slice(start, start + visible);

    console.log(
      `Server messages (${start + 1}-${start + slice.length} of ${state.serverMessages.length}) [up/down,x=clear]:`
    );

    const cols = process.stdout?.columns ?? 80;
    for (const m of slice) {
      const avail = cols - m.time.length - 1;
      console.log(`${m.time} ${formatServerText(m.text, avail)}`);
    }
    console.log();
  }

  console.log(
    "Connected:",
    ws && ws.readyState === WebSocket.OPEN ? "YES" : "NO reconnect with [c]"
  );
  console.log("Vehicle ID:", vehicleId);
  console.log("\nCommands:");
  console.log("[i] Set vehicle id");
  console.log("[1] takeoff");
  console.log("[2] land");
  console.log("[3] startGoHome");
  console.log("[4] motors (arm/disarm)");
  console.log("[5] identify");
  console.log("[6] startMission");
  console.log("[7] pauseMission");
  console.log("[8] stopMission");
  console.log("[9] resumeMission");
  console.log("[m] virtualSticks (enable/disable)");
  console.log("[v] virtualSticksInput (send axes payload)");
  console.log("[r] send raw JSON");
  console.log("[q] Quit");
}

// assign render to tuiBase
setRender(render);

// ---------------- Commands ----------------
async function sendCommand(eventType, payload) {
  if (!ws || ws.readyState !== WebSocket.OPEN) {
    console.log("WebSocket not open");
    return;
  }
  ws.send(JSON.stringify({ userId: vehicleId, role: eventType, message: payload }));
}

function normalizeChoice(input, allowed, def) {
  if (!input) return def;
  const s = input.trim();
  if (s === "\u001b") return def;
  const lower = s.toLowerCase();
  for (const a of allowed) if (a.toLowerCase() === lower) return a;
  return def;
}

// ---------------- Keyboard ----------------
setupKeyboard(async (key) => {
  switch (key) {
    case "q": ws?.close(); process.exit(0); break;
    case "i": vehicleId = (await readLine("Enter vehicle id: ")) || vehicleId; break;
    case "1": await sendCommand("FlightCommand", { command: "takeoff" }); break;
    case "2": await sendCommand("FlightCommand", { command: "land" }); break;
    case "3": await sendCommand("FlightCommand", { command: "startGoHome" }); break;

    case "4": {
      const s = await readLine("Motors state (1/0): ");
      if (s !== null) await sendCommand("UtilityCommand", { command: "motors", state: s === "1" });
      break;
    }

    case "5": {
      const s = await readLine("Identify state (1/0): ");
      if (s !== null) await sendCommand("UtilityCommand", { command: "identify", state: s === "1" });
      break;
    }

    case "6": {
      const startAction = normalizeChoice(await readLine("Start action (takeoff/none) [takeoff]: "), ["takeoff","none"], "takeoff");
      const endAction = normalizeChoice(await readLine("End action (land/goHome/none) [land]: "), ["land","goHome","none"], "land");
      const repeat = parseInt(await readLine("Repeat count [0]: ")) || 0;
      const altitude = parseFloat(await readLine("Altitude [50]: ")) || 50;
      await sendCommand("StartMissionCommand", { command: "startMission", startAction, endAction, repeat, altitude });
      break;
    }

    case "7": await sendCommand("FlightCommand", { command: "pauseMission" }); break;
    case "8": await sendCommand("FlightCommand", { command: "stopMission" }); break;
    case "9": await sendCommand("FlightCommand", { command: "resumeMission" }); break;

    case "m": {
      const s = await readLine("Virtual sticks enabled (1/0): ");
      if (s !== null) await sendCommand("UtilityCommand", { command: "virtualSticks", state: s === "1" });
      break;
    }

    case "v": await sendCommand("VirtualSticksInputCommand", { command: "virtualSticksInput", yaw:0.3, throttle:0.1, pitch:-0.4, roll:0 }); break;
    case "r": {
      const s = await readLine("Enter raw JSON: ");
      if (s) try { ws.send(s); } catch { console.log("Invalid JSON"); }
      break;
    }
    case "c": connect(); break;

    // scrolling / clear
    case "\u001b[A": case "k": state.messageScroll = Math.max(0, state.messageScroll - 1); break;
    case "\u001b[B": case "j": state.messageScroll = Math.min(Math.max(0, state.serverMessages.length - 3), state.messageScroll + 1); break;
    case "x": state.serverMessages.length = 0; state.messageScroll = 0; break;
  }
});

requestRender();
