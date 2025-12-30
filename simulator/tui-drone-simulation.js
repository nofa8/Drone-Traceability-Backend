#!/usr/bin/env bun

import WebSocket from "ws";
import crypto from "crypto";
import fs from "fs";
import {
  state,
  pushServerMessage,
  requestRender,
  readLine,
  setupKeyboard,
  formatServerText,
  setRender,
} from "./tuiBase.js";

const WS_URL = "ws://localhost:8083";
const drones = new Map();

// Load presets
const presets = JSON.parse(fs.readFileSync("./presets.json", "utf-8"));
const DRONE_STEP_INTERVAL = 200;

function startDroneTimer() {
  if (globalThis._droneTimer) return; // already running
  globalThis._droneTimer = setInterval(() => {
    let changed = false;
    for (const d of drones.values()) {
      if (d.isFlying) {
        changed = d.step() || changed; // step returns true if state changed
      }
    }
    if (changed) requestRender(); // single render per tick
  }, DRONE_STEP_INTERVAL);
}

function stopDroneTimer() {
  if (globalThis._droneTimer) clearInterval(globalThis._droneTimer);
  globalThis._droneTimer = null;
}

// ---------------- Drone ----------------
class Drone {
  constructor(id) {
    this.id = id;
    this.model = "";
    this.homeLocation = { lat: 39.73426, lng: -8.82159 };
    this.lat = 39.73426;
    this.lng = -8.82159;
    this.alt = 0;
    this.velX = 0;
    this.velY = 0;
    this.velZ = 0;
    this.batLvl = 100;
    this.batTemperature = 25;
    this.hdg = 0;
    this.satCount = 10;
    this.rft = 0;
    this.isTraveling = false;
    this.isFlying = false;
    this.online = true;
    this.isGoingHome = false;
    this.isHomeLocationSet = true;
    this.areMotorsOn = false;
    this.areLightsOn = false;

    this.targetAlt = null;
    this.targetLat = null;
    this.targetLng = null;
    this.maxSpeed = 5;
    this.maxAscendRate = 1;
    this.windFactor = 0.05;

    this.ws = new WebSocket(`${WS_URL}?dboidsID=${id}`);

    this.ws.on("open", () => pushServerMessage(`[ws ${this.id}] connected`));
    this.ws.on("message", (data) => {
      const str = data.toString();
      try {
        const parsed = JSON.parse(str);
        pushServerMessage(`[${this.id}] ${JSON.stringify(parsed)}`);
      } catch {
        pushServerMessage(`[${this.id}] ${str}`);
      }
    });
    this.ws.on("close", (code, reason) =>
      pushServerMessage(`[ws ${this.id}] closed ${code} ${reason || ""}`)
    );
    this.ws.on("error", (err) =>
      pushServerMessage(`[ws ${this.id}] error: ${err?.message ?? err}`)
    );
  }

  start() {
    if (this.isFlying) return;
    this.isFlying = true;
    this.areMotorsOn = true;
    startDroneTimer(); // ensure global timer is running
  }

  stop() {
    if (!this.isFlying) return;
    this.isFlying = false;
    this.areMotorsOn = false;
    clearInterval(this.timer);
    this.velX = this.velY = this.velZ = 0;
    this.send();
    requestRender();
  }

  setHeading(hdg) {
    this.hdg = hdg % 360;
    this.isGoingHome = false;
    this.targetLat = null;
    this.targetLng = null;
  }

  setAltitude(targetAlt) {
    this.targetAlt = targetAlt;
  }

  setSpeed(speed) {
    this.maxSpeed = speed;
  }

  goHome() {
    this.isGoingHome = true;
    this.targetLat = this.homeLocation.lat;
    this.targetLng = this.homeLocation.lng;
  }

  setWaypoint(lat, lng, alt = null) {
    this.isGoingHome = false;
    this.targetLat = lat;
    this.targetLng = lng;
    if (alt !== null) this.targetAlt = alt;
  }

  step() {
    // Track changes for reactive rendering
    let changed = false;
    const oldLat = this.lat;
    const oldLng = this.lng;
    const oldAlt = this.alt;
    const oldHdg = this.hdg;

    // Update heading
    if (this.targetLat !== null && this.targetLng !== null) {
      const dx = this.targetLng - this.lng;
      const dy = this.targetLat - this.lat;
      const desiredHdg = (Math.atan2(dy, dx) * 180) / Math.PI;
      const diff = ((desiredHdg - this.hdg + 540) % 360) - 180;
      this.hdg += Math.max(Math.min(diff, 5), -5);
    } else {
      this.hdg += (Math.random() - 0.5) * 5;
      if (this.hdg < 0) this.hdg += 360;
      if (this.hdg >= 360) this.hdg -= 360;
    }

    if (this.hdg !== oldHdg) changed = true;

    const rad = (this.hdg * Math.PI) / 180;

    this.velX += 0.1 * Math.cos(rad);
    this.velY += 0.1 * Math.sin(rad);
    const speed = Math.sqrt(this.velX ** 2 + this.velY ** 2);
    if (speed > this.maxSpeed) {
      this.velX = (this.velX / speed) * this.maxSpeed;
      this.velY = (this.velY / speed) * this.maxSpeed;
    }

    if (this.targetAlt !== null) {
      const altDiff = this.targetAlt - this.alt;
      this.velZ = Math.max(
        Math.min(altDiff * 0.2, this.maxAscendRate),
        -this.maxAscendRate
      );
    } else {
      this.velZ += (Math.random() - 0.5) * 0.1;
      this.velZ = Math.max(
        Math.min(this.velZ, this.maxAscendRate),
        -this.maxAscendRate
      );
    }

    this.lat += this.velY * 0.00001 + (Math.random() - 0.5) * this.windFactor;
    this.lng += this.velX * 0.00001 + (Math.random() - 0.5) * this.windFactor;
    this.alt += this.velZ;
    if (this.alt < 0) this.alt = 0;

    if (this.targetLat !== null && this.targetLng !== null) {
      const dist = Math.hypot(
        this.targetLat - this.lat,
        this.targetLng - this.lng
      );
      if (dist < 0.0001) {
        this.velX = this.velY = 0;
        this.targetLat = null;
        this.targetLng = null;
      }
    }

    const motionFactor = Math.sqrt(
      this.velX ** 2 + this.velY ** 2 + this.velZ ** 2
    );
    this.batLvl = Math.max(0, this.batLvl - 0.02 - motionFactor * 0.01);

    if (
      this.lat !== oldLat ||
      this.lng !== oldLng ||
      this.alt !== oldAlt ||
      this.hdg !== oldHdg
    )
      changed = true;

    this.send();
    return changed;
  }

  send() {
    if (this.ws.readyState === WebSocket.OPEN) {
      this.ws.send(
        JSON.stringify({
          id: this.id,
          model: this.model,
          homeLocation: this.homeLocation,
          lat: this.lat,
          lng: this.lng,
          alt: this.alt,
          velX: this.velX,
          velY: this.velY,
          velZ: this.velZ,
          batLvl: this.batLvl,
          batTemperature: this.batTemperature,
          hdg: this.hdg,
          satCount: this.satCount,
          rft: this.rft,
          isTraveling: this.isTraveling,
          isFlying: this.isFlying,
          online: this.online,
          isGoingHome: this.isGoingHome,
          isHomeLocationSet: this.isHomeLocationSet,
          areMotorsOn: this.areMotorsOn,
          areLightsOn: this.areLightsOn,
        })
      );
    }
  }

  close() {
    this.stop();
    try {
      this.ws.close();
    } catch {}
  }
}

// ---------------- Drone helpers ----------------
function resolveTargets(input) {
  if (input === "all") return [...drones.values()];
  const idxs = input
    .split(",")
    .map((n) => parseInt(n.trim(), 10) - 1)
    .filter((n) => !isNaN(n));
  return [...drones.values()].filter((_, i) => idxs.includes(i));
}

async function createNewDrone() {
  const id = crypto.randomUUID().slice(0, 6);
  const d = new Drone(id);
  d.model = (await readLine("Enter model name: ")) || "";
  d.maxSpeed = parseFloat(await readLine("Enter max speed: ")) || 5;
  d.targetAlt = parseFloat(await readLine("Enter target altitude: ")) || 0;
  d.hdg = parseFloat(await readLine("Enter heading (0-360): ")) || 0;
  drones.set(id, d);
  pushServerMessage(`Drone ${id} created.`);
}

async function loadAllPresets() {
  for (const presetKey of Object.keys(presets)) {
    const preset = presets[presetKey];
    const id = preset.id ?? crypto.randomUUID().slice(0, 6);
    if (drones.has(id)) continue;
    const d = new Drone(id);
    d.model = preset.model || "";
    d.maxSpeed = preset.maxSpeed ?? d.maxSpeed;
    d.targetAlt = preset.targetAlt ?? d.targetAlt;
    d.hdg = preset.hdg ?? d.hdg;
    drones.set(id, d);
    pushServerMessage(`Drone ${id} (${d.model}) loaded.`);
  }
}

// ---------------- Render ----------------
function render() {
  if (state.lockRender) return;

  console.clear();
  console.log("Drone Simulator\n");

  const visible = 3;
  const maxStart = Math.max(0, state.serverMessages.length - visible);
  const start = Math.max(0, Math.min(state.messageScroll, maxStart));
  const slice = state.serverMessages.slice(start, start + visible);

  if (slice.length > 0) {
    console.log(
      `Server messages (${start + 1}-${start + slice.length} of ${
        state.serverMessages.length
      }) [up/down,x=clear]:`
    );
    const cols = process.stdout?.columns ?? 80;
    for (const m of slice) {
      const avail = Math.max(10, cols - m.time.length - 1);
      console.log(`${m.time} ${formatServerText(m.text, avail)}`);
    }
    console.log();
  }

  console.log("#  ID       STATE     ALT     BAT");
  console.log("--------------------------------");
  let i = 1;
  for (const d of drones.values()) {
    console.log(
      `${String(i).padEnd(3)}${d.id.padEnd(9)}${(d.isFlying
        ? "FLYING"
        : "IDLE"
      ).padEnd(10)}${d.alt.toFixed(1).padEnd(8)}${Math.round(d.batLvl)}%`
    );
    i++;
  }
  if (drones.size === 0) console.log("(no drones)");

  console.log(`
[a] Load all drones from presets
[n] Create new drone
[s] Start drone(s)
[f] Finish drone(s)
[d] Delete drone(s)
[h] Set heading
[t] Set target altitude
[v] Set max speed
[w] Set waypoint
[g] Go home
[q] Quit
`);
}

// Assign render to tuiBase
setRender(render);

// ---------------- Keyboard ----------------
setupKeyboard(async (key) => {
  let targets;

  switch (key) {
    case "q":
      for (const d of drones.values()) d.close();
      process.exit(0);

    case "a":
      await loadAllPresets();
      break;

    case "n":
      await createNewDrone();
      break;

    case "s":
    case "f":
    case "d":
    case "h":
    case "t":
    case "v":
    case "w":
    case "g":
      if (drones.size === 1) targets = [...drones.values()];
      else {
        const sel = await readLine("Select drones (e.g., 1,3 or all): ");
        targets = resolveTargets(sel);
      }
      break;
  }

  switch (key) {
    case "s":
      targets?.forEach((d) => d.start());
      break;
    case "f":
      targets?.forEach((d) => d.stop());
      break;
    case "d":
      targets?.forEach((d) => {
        d.close();
        drones.delete(d.id);
      });
      break;
    case "h": {
      const hdg = parseFloat(await readLine("Enter heading (0-360): "));
      targets?.forEach((d) => d.setHeading(hdg));
      break;
    }
    case "t": {
      const alt = parseFloat(await readLine("Enter target altitude: "));
      targets?.forEach((d) => d.setAltitude(alt));
      break;
    }
    case "v": {
      const spd = parseFloat(await readLine("Enter max speed: "));
      targets?.forEach((d) => d.setSpeed(spd));
      break;
    }
    case "w": {
      const wp = await readLine("Enter waypoint (lat,lng[,alt]): ");
      const parts = wp.split(",").map((p) => parseFloat(p.trim()));
      if (parts.length >= 2)
        targets?.forEach((d) =>
          d.setWaypoint(parts[0], parts[1], parts[2] ?? null)
        );
      break;
    }
    case "g":
      targets?.forEach((d) => d.goHome());
      break;

    // scroll / clear
    case "\u001b[A":
    case "k":
      state.messageScroll = Math.max(0, state.messageScroll - 1);
      break;
    case "\u001b[B":
    case "j":
      state.messageScroll = Math.min(
        Math.max(0, state.serverMessages.length - 3),
        state.messageScroll + 1
      );
      break;
    case "x":
      state.serverMessages.length = 0;
      state.messageScroll = 0;
      break;
  }

  requestRender();
});

// ---------------- Graceful shutdown ----------------
process.on("SIGINT", () => {
  for (const d of drones.values()) d.close();
  process.exit(0);
});

// Initial render
requestRender();
