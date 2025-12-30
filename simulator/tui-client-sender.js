#!/usr/bin/env bun

import WebSocket from "ws";

const WS_URL = "ws://localhost:5101/ws";

let lockRender = false;
let vehicleId = "RD001";

let ws = null;
let lastConnect = null;
let lastDisconnect = null;

function connect() {
  if (ws && ws.readyState === WebSocket.OPEN) {
    console.log("Already connected");
    return;
  }
  if (ws) {
    try {
      ws.close();
    } catch {}
  }
  ws = new WebSocket(WS_URL);

  ws.on("open", () => {
    lastConnect = new Date().toISOString();
    render();
  });
  ws.on("close", () => {
    lastDisconnect = new Date().toISOString();
    render();
  });
  ws.on("error", (err) => {
    try {
      console.log("WS error:", err && err.message ? err.message : err);
    } catch {}
  });
  ws.on("message", (msg) => {
    // show server messages briefly
    try {
      const text = msg.toString();
      console.log("\n< SERVER: " + text);
      lockRender = true;
      setTimeout(() => {
        lockRender = false;
        render();
      }, 2000);
    } catch {}
  });
}

// initialize connection
connect();

function readLine(prompt) {
  lockRender = true;
  return new Promise((resolve) => {
    let buf = "";
    process.stdout.write(prompt);
    const onData = (data) => {
      const ch = data.toString();
      if (ch === "\r") {
        process.stdin.off("data", onData);
        process.stdout.write("\n");
        lockRender = false;
        resolve(buf.trim());
      } else if (ch === "\u0003") process.exit(0);
      else if (ch === "\u007f") {
        if (buf.length > 0) {
          buf = buf.slice(0, -1);
          process.stdout.write("\b \b");
        }
      } else {
        buf += ch;
        process.stdout.write(ch);
      }
    };
    process.stdin.on("data", onData);
  });
}

function normalizeChoice(input, allowed, def) {
  if (!input) return def;
  const s = input.trim();
  if (s === "\u001b") return def; // ignore ESC key
  const lower = s.toLowerCase();
  for (const a of allowed) if (a.toLowerCase() === lower) return a;
  return def;
}

function render() {
  if (lockRender) return;
  console.clear();
  console.log("Client Command TUI\n");
  console.log(
    "Connected:",
    ws && ws.readyState === WebSocket.OPEN ? "YES" : "NO reconnect with [c]"
  );
  console.log("Vehicle ID:", vehicleId);
  console.log(`\nCommands:`);
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

async function sendCommand(eventType, payload) {
  if (!ws || ws.readyState !== WebSocket.OPEN) {
    console.log("WebSocket not open, cannot send");
    return;
  }
  const envelope = {
    userId: vehicleId,
    role: eventType,
    message: payload,
  };
  const text = JSON.stringify(envelope);
  ws.send(text);
  console.log("Sent: " + text);
}

process.stdin.setRawMode(true);
process.stdin.resume();
process.stdin.on("data", async (data) => {
  if (lockRender) return;
  const key = data.toString();
  switch (key) {
    case "q":
      ws.close();
      process.exit(0);
      break;
    case "i": {
      const id = await readLine("Enter vehicle id: ");
      if (id) vehicleId = id;
      break;
    }
    case "1":
      await sendCommand("FlightCommand", { command: "takeoff" });
      break;
    case "2":
      await sendCommand("FlightCommand", { command: "land" });
      break;
    case "3":
      await sendCommand("FlightCommand", { command: "startGoHome" });
      break;
    case "c":
      // reconnect
      lockRender = true;
      console.log("Reconnecting...");
      connect();
      setTimeout(() => {
        lockRender = false;
        render();
      }, 500);
      break;
    case "4": {
      const s = await readLine("Motors state (1/0): ");
      const state = s.trim() === "1";
      await sendCommand("UtilityCommand", { command: "motors", state });
      break;
    }
    case "5":
      const s = await readLine("Identify state (1/0): ");
      const state = s.trim() === "1";
      await sendCommand("UtilityCommand", { command: "identify", state });
      break;
    case "6": {
      // Prompt for mission parameters matching StartMissionCommand
      const startRaw = await readLine(
        "Start action (takeoff/none) [takeoff]: "
      );
      const endRaw = await readLine("End action (land/goHome/none) [land]: ");
      const startAction = normalizeChoice(
        startRaw,
        ["takeoff", "none"],
        "takeoff"
      );
      const endAction = normalizeChoice(
        endRaw,
        ["land", "goHome", "none"],
        "land"
      );
      const repeatRaw =
        (await readLine("Repeat count (0 = once) [0]: ")) || "0";
      const altitudeRaw = (await readLine("Altitude (meters) [50]: ")) || "50";
      const pathRaw = await readLine("Waypoints (lat,lng;lat,lng or empty): ");
      const statusRaw = await readLine(
        "Initial status (optional, RUNNING/PAUSED/COMPLETED/STOPPED) or empty: "
      );
      const status =
        statusRaw && statusRaw.trim() !== "\u001b" ? statusRaw.trim() : null;

      const repeat = parseInt(repeatRaw, 10) || 0;
      const altitude = parseFloat(altitudeRaw) || 50;

      const path = [];
      if (pathRaw && pathRaw.trim().length > 0) {
        // format: lat,lng;lat,lng
        const points = pathRaw
          .split(";")
          .map((p) => p.trim())
          .filter(Boolean);
        for (const pt of points) {
          const parts = pt.split(",").map((s) => parseFloat(s.trim()));
          if (parts.length >= 2 && !isNaN(parts[0]) && !isNaN(parts[1])) {
            path.push({ lat: parts[0], lng: parts[1] });
          }
        }
      }

      const msg = {
        command: "startMission",
        startAction: startAction,
        endAction: endAction,
        repeat: repeat,
        altitude: altitude,
        path: path,
      };
      if (status) msg.status = status;

      await sendCommand("StartMissionCommand", msg);
      break;
    }
    case "7":
      await sendCommand("FlightCommand", { command: "pauseMission" });
      break;
    case "8":
      await sendCommand("FlightCommand", { command: "stopMission" });
      break;
    case "9":
      await sendCommand("FlightCommand", { command: "startMission" });
      break;
    case "m": {
      const s = await readLine("Virtual sticks enabled (1/0): ");
      const state = s.trim() === "1";
      await sendCommand("UtilityCommand", { command: "virtualSticks", state });
      break;
    }
    case "v": {
      // send a default virtual sticks input payload
      await sendCommand("VirtualSticksInputCommand", {
        command: "virtualSticksInput",
        yaw: 0.3,
        throttle: 0.1,
        pitch: -0.4,
        roll: 0,
      });
      break;
    }
    case "r": {
      const s = await readLine("Enter raw JSON to send as envelope: ");
      try {
        const obj = JSON.parse(s);
        if (obj && typeof obj === "object") {
          // full EventEnvelope provided
          if (obj.TimeStamp && obj.EventType && obj.Payload) {
            if (!ws || ws.readyState !== WebSocket.OPEN) {
              console.log("WebSocket not open, cannot send raw envelope");
            } else {
              ws.send(JSON.stringify(obj));
              console.log("Sent raw envelope: " + JSON.stringify(obj));
            }
          } else if (obj.userId && obj.message) {
            sendEventEnvelope("CommandReceived", obj);
          } else {
            sendEventEnvelope("CommandReceived", {
              userId: vehicleId,
              message: obj,
            });
          }
        }
      } catch {
        console.log("Invalid JSON");
      }
      break;
    }
  }
  render();
});

// graceful shutdown
process.on("SIGINT", () => {
  console.log("\nClosing...");
  try {
    ws.close();
  } catch {}
  process.exit(0);
});

// cap render to max 60 FPS
setInterval(render, Math.round(1000 / 60));
render();
