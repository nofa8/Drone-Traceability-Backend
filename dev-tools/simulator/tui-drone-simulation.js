#!/usr/bin/env bun

import WebSocket from "ws";
import crypto from "crypto";
import fs from "fs";

const WS_URL = "ws://localhost:8083";
const drones = new Map();
let isReadingInput = false;

// Load presets
const presets = JSON.parse(fs.readFileSync("./presets.json", "utf-8"));

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
  }

  start() {
    if (this.isFlying) return;
    this.isFlying = true;
    this.areMotorsOn = true;
    this.timer = setInterval(() => this.step(), 200);
  }

  stop() {
    if (!this.isFlying) return;
    this.isFlying = false;
    this.areMotorsOn = false;
    clearInterval(this.timer);
    this.velX = this.velY = this.velZ = 0;
    this.send();
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

    this.send();
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
    this.ws.close();
  }
}

// Graceful shutdown on Ctrl+C
process.on("SIGINT", () => {
  console.log("\nCaught Ctrl+C, closing drones...");
  for (const d of drones.values()) d.close();
  process.exit(0);
});

// ---------------- Input ----------------
function readLine(prompt) {
  isReadingInput = true;
  return new Promise((resolve) => {
    let buf = "";
    process.stdout.write(prompt);
    const onData = (data) => {
      const ch = data.toString();
      if (ch === "\r") {
        process.stdin.off("data", onData);
        process.stdout.write("\n");
        isReadingInput = false;
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

function resolveTargets(input) {
  if (input === "all") return [...drones.values()];
  const idxs = input
    .split(",")
    .map((n) => parseInt(n.trim(), 10) - 1)
    .filter((n) => !isNaN(n));
  return [...drones.values()].filter((_, i) => idxs.includes(i));
}

// ---------------- Add Drone From Preset ----------------
async function createNewDrone() {
  const id = crypto.randomUUID().slice(0, 6);
  const d = new Drone(id);

  d.model = (await readLine("Enter model name: ")) || "";
  d.maxSpeed = parseFloat(await readLine("Enter max speed: ")) || 5;
  d.targetAlt = parseFloat(await readLine("Enter target altitude: ")) || 0;
  d.hdg = parseFloat(await readLine("Enter heading (0-360): ")) || 0;

  drones.set(id, d);
  console.log(`Drone ${id} created.`);
}

async function loadAllPresets() {
  console.log("Loading all presets...");

  for (const presetKey of Object.keys(presets)) {
    const preset = presets[presetKey];
    const id = preset.id ?? crypto.randomUUID().slice(0, 6);

    if (drones.has(id)) {
      console.log(`Drone with ID "${id}" already exists, skipping.`);
      continue;
    }

    const d = new Drone(id);
    d.model = preset.model || "";
    d.maxSpeed = preset.maxSpeed ?? d.maxSpeed;
    d.targetAlt = preset.targetAlt ?? d.targetAlt;
    d.hdg = preset.hdg ?? d.hdg;

    drones.set(id, d);
    console.log(`Drone ${id} (${d.model}) loaded.`);
  }
}


// ---------------- Render ----------------
function render() {
  if (isReadingInput) return;
  console.clear();
  console.log("Drone Simulator\n");
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

// ---------------- Key Handling ----------------
process.stdin.setRawMode(true);
process.stdin.resume();
process.stdin.on("data", async (data) => {
  if (isReadingInput) return;
  const key = data.toString();
  let targets;

  if (key === "\u0003") {  // \u0003 is Ctrl+C
    console.log("\nCaught Ctrl+C, closing drones...");
    for (const d of drones.values()) d.close();
    process.exit(0);
  }

  if (key === "q") {
    for (const d of drones.values()) d.close();
    process.exit(0);
  }
  if (key === "a") await loadAllPresets();
  if (key === "n") await createNewDrone();

  if (["s", "f", "d", "h", "t", "v", "w", "g"].includes(key)) {
    if (drones.size === 1) targets = [...drones.values()];
    else {
      const sel = await readLine("Select drones (e.g., 1,3 or all): ");
      targets = resolveTargets(sel);
    }
  }

  switch (key) {
    case "s":
      targets.forEach((d) => d.start());
      break;
    case "f":
      targets.forEach((d) => d.stop());
      break;
    case "d":
      targets.forEach((d) => {
        d.close();
        drones.delete(d.id);
      });
      break;
    case "h": {
      const hdg = parseFloat(await readLine("Enter heading (0-360): "));
      targets.forEach((d) => d.setHeading(hdg));
      break;
    }
    case "t": {
      const alt = parseFloat(await readLine("Enter target altitude: "));
      targets.forEach((d) => d.setAltitude(alt));
      break;
    }
    case "v": {
      const spd = parseFloat(await readLine("Enter max speed: "));
      targets.forEach((d) => d.setSpeed(spd));
      break;
    }
    case "w": {
      const wp = await readLine("Enter waypoint (lat,lng[,alt]): ");
      const parts = wp.split(",").map((p) => parseFloat(p.trim()));
      if (parts.length >= 2)
        targets.forEach((d) =>
          d.setWaypoint(parts[0], parts[1], parts[2] ?? null)
        );
      break;
    }
    case "g":
      targets.forEach((d) => d.goHome());
      break;
  }
});

// ---------------- Main Loop ----------------
setInterval(render, 200);
render();
