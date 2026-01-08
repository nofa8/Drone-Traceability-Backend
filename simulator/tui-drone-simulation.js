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
const presets = JSON.parse(fs.readFileSync("./presets.json", "utf-8"));
const PRESET_FIELDS = [
  "model",
  "lat",
  "lng",
  "alt",
  "maxSpeed",
  "maxAscendRate",
  "targetAlt",
  "targetLat",
  "targetLng",
  "hdg",
  "batLvl",
  "satCount",
  "windSpeed",
  "gpsNoiseBase",
  "areMotorsOn",
  "areLightsOn",
];

const DRONE_STEP_INTERVAL = 200; // ms
const METERS_PER_DEG_LAT = 111_320;

// ---------------- Timer ----------------
function startDroneTimer() {
  if (globalThis._droneTimer) return;
  globalThis._droneTimer = setInterval(() => {
    let changed = false;
    for (const d of drones.values()) {
      if (d.isFlying) changed = d.step() || changed;
    }
    if (changed) requestRender();
  }, DRONE_STEP_INTERVAL);
}

// ---------------- Drone ----------------
class Drone {
  constructor(id) {
    this.id = id;
    this.model = "";

    this.homeLocation = { lat: 39.73426, lng: -8.82159 };
    this.lat = this.homeLocation.lat;
    this.lng = this.homeLocation.lng;
    this.alt = 0;

    this.velX = 0;
    this.velY = 0;
    this.velZ = 0;

    this.velBiasX = 0;
    this.velBiasY = 0;
    this.altBias = 0;

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

    // Environmental models
    this.windSpeed = 0.3; // m/s
    this.gpsNoiseBase = 0.8; // meters
    this.gpsBiasX = 0;
    this.gpsBiasY = 0;

    this.ws = new WebSocket(`${WS_URL}?dboidsID=${id}`);

    this.ws.on("open", () => {
      pushServerMessage(`WS open ${this.id}`);
      // send initial state once connected
      this.send();
    });

    this.ws.on("message", (data) => {
      let text;
      try {
        text = typeof data === "string" ? data : data.toString();
      } catch (e) {
        text = String(data);
      }
      pushServerMessage(`${this.id}: ${text}`);
    });

    this.ws.on("close", () => {
      pushServerMessage(`WS closed ${this.id}`);
      this.online = false;
    });

    this.ws.on("error", (err) => {
      pushServerMessage(`WS error ${this.id}: ${err?.message ?? err}`);
    });
  }

  start() {
    this.isFlying = true;
    this.areMotorsOn = true;
    startDroneTimer();
  }

  stop() {
    this.isFlying = false;
    this.areMotorsOn = false;
    this.velX = this.velY = this.velZ = 0;
    this.send();
  }

  goHome() {
    this.isGoingHome = true;
    this.targetLat = this.homeLocation.lat;
    this.targetLng = this.homeLocation.lng;
    this.targetAlt = Math.max(this.alt, 10); // cruise height
  }

  step() {
    const dt = DRONE_STEP_INTERVAL / 1000;
    let changed = false;

    // -------- IMU bias drift (slow)
    this.velBiasX += (Math.random() - 0.5) * 0.002;
    this.velBiasY += (Math.random() - 0.5) * 0.002;
    this.altBias += (Math.random() - 0.5) * 0.01;

    // -------- Heading control
    if (this.targetLat !== null && this.targetLng !== null) {
      const dx = this.targetLng - this.lng;
      const dy = this.targetLat - this.lat;
      const desired = (Math.atan2(dy, dx) * 180) / Math.PI;
      const diff = ((desired - this.hdg + 540) % 360) - 180;
      this.hdg += Math.max(Math.min(diff, 4), -4);
    }

    // -------- Velocity integration
    const rad = (this.hdg * Math.PI) / 180;
    this.velX += Math.cos(rad) * 0.2;
    this.velY += Math.sin(rad) * 0.2;

    const speed = Math.hypot(this.velX, this.velY);
    if (speed > this.maxSpeed) {
      this.velX *= this.maxSpeed / speed;
      this.velY *= this.maxSpeed / speed;
    }

    // -------- Altitude logic (RTH descent)
    if (this.isGoingHome && this.targetLat && this.targetLng) {
      const distHome =
        Math.hypot(this.targetLat - this.lat, this.targetLng - this.lng) *
        METERS_PER_DEG_LAT;

      if (distHome < 10) {
        this.targetAlt = 0;
      }
    }

    if (this.targetAlt !== null) {
      const diff = this.targetAlt - this.alt;
      this.velZ = Math.max(
        Math.min(diff * 0.5, this.maxAscendRate),
        -this.maxAscendRate
      );
    }

    // -------- Wind (meters)
    const windX = (Math.random() - 0.5) * this.windSpeed;
    const windY = (Math.random() - 0.5) * this.windSpeed;

    // -------- Position integration (meters)
    let dx = (this.velX + this.velBiasX + windX) * dt;
    let dy = (this.velY + this.velBiasY + windY) * dt;

    const maxStep = this.maxSpeed * dt * 1.2;
    const stepDist = Math.hypot(dx, dy);
    if (stepDist > maxStep) {
      const scale = maxStep / stepDist;
      dx *= scale;
      dy *= scale;
    }

    const metersPerDegLng =
      METERS_PER_DEG_LAT * Math.cos((this.lat * Math.PI) / 180);

    this.lat += dy / METERS_PER_DEG_LAT;
    this.lng += dx / metersPerDegLng;
    let oldAlt = this.alt;
    this.alt = Math.max(0, this.alt + this.velZ * dt);

    // -------- GPS noise model
    const gpsAccuracy = this.gpsNoiseBase * (12 / Math.max(this.satCount, 6));

    this.gpsBiasX += (Math.random() - 0.5) * 0.02;
    this.gpsBiasY += (Math.random() - 0.5) * 0.02;

    this.lat +=
      ((Math.random() - 0.5) * gpsAccuracy + this.gpsBiasY) /
      METERS_PER_DEG_LAT;
    this.lng +=
      ((Math.random() - 0.5) * gpsAccuracy + this.gpsBiasX) / metersPerDegLng;

    // -------- Battery
    const motion = Math.hypot(this.velX, this.velY, this.velZ);

    let oldBatLvl = this.batLvl;
    this.batLvl = Math.max(0, this.batLvl - 0.02 - motion * 0.01);

   // check if values changes on the display 
   changed = oldAlt !== this.alt || oldBatLvl !== this.batLvl;

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
          alt: this.alt + this.altBias,
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
  for (const preset of Object.values(presets)) {
    const id = preset.id ?? crypto.randomUUID().slice(0, 6);
    if (drones.has(id)) continue;

    const d = new Drone(id);
    for (const key of PRESET_FIELDS) {
      if (preset[key] !== undefined) {
        d[key] = preset[key];
      }
    }

    // If preset defines initial position, update homeLocation too
    if (preset.lat !== undefined && preset.lng !== undefined) {
      d.homeLocation = { lat: preset.lat, lng: preset.lng };
    }

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
