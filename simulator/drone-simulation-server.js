import { WebSocketServer } from "ws";
import http from "http";
import { parse } from "url";

const PORT = 8083;

/**
 * @typedef {Object} Drone
 * @property {string} id
 * @property {number} lat
 * @property {number} lng
 * @property {Object} homeLocation
 * @property {number} homeLocation.lat
 * @property {number} homeLocation.lng
 * @property {number} alt
 * @property {number} velX
 * @property {number} velY
 * @property {number} velZ
 * @property {number} batLvl
 * @property {number} batTemperature
 * @property {number} hdg
 * @property {number} satCount
 * @property {number} rft
 * @property {boolean} isTraveling
 * @property {boolean} isFlying
 * @property {string} model
 * @property {boolean} online
 * @property {boolean} isGoingHome
 * @property {boolean} isHomeLocationSet
 * @property {boolean} areMotorsOn
 * @property {boolean} areLightsOn
 * @property {Timer=} timer
 */

/** @type {Map<string, Drone>} */
const drones = new Map();

/* ---------------- Logging ---------------- */
function logEvent(msg, id) {
  const ts = new Date().toISOString();
  console.log(`[${ts}]${id ? ` [${id}]` : ""} ${msg}`);
}

/* ---------------- HTTP CONTROL API ---------------- */

const server = http.createServer((req, res) => {
  const url = parse(req.url, true);
  const [_, resource, id, action] = url.pathname.split("/");

  res.setHeader("Content-Type", "application/json");

  // Create drone
  if (req.method === "POST" && resource === "drones" && !id) {
    let body = "";
    req.on("data", (c) => (body += c));
    req.on("end", () => {
      const { dboidsID } = JSON.parse(body || "{}");
      if (!dboidsID) return end(res, 400, { error: "dboidsID required" });
      if (drones.has(dboidsID)) return end(res, 409, { error: "exists" });

      const drone = {
        id: dboidsID,
        lat: 39.73426,
        lng: -8.82159,
        homeLocation: { lat: 39.73426, lng: -8.82159 },
        alt: 0,
        velX: 0,
        velY: 0,
        velZ: 0,
        batLvl: 100,
        batTemperature: 25,
        hdg: 0,
        satCount: 10,
        rft: 1,
        isTraveling: false,
        isFlying: false,
        model: "Model",
        online: true,
        isGoingHome: false,
        isHomeLocationSet: true,
        areMotorsOn: false,
        areLightsOn: false,
      };

      drones.set(dboidsID, drone);
      logEvent("Drone created", dboidsID);
      end(res, 201, drone);
    });
    return;
  }

  // GET /drones → list all drones
  if (req.method === "GET" && resource === "drones" && !id) {
    const list = Array.from(drones.values());
    return end(res, 200, list);
  }

  // Delete drone
  if (req.method === "DELETE" && resource === "drones" && id) {
    const drone = drones.get(id);
    if (!drone) return end(res, 404, {});
    stop(drone);
    drones.delete(id);
    logEvent("Drone deleted", id);
    return end(res, 204, {});
  }

  // Start flight
  if (req.method === "POST" && resource === "drones" && action === "start") {
    const drone = drones.get(id);
    if (!drone) return end(res, 404, {});

    drone.isFlying = true;
    drone.isTraveling = true;
    drone.velX = 1;
    drone.velY = 1;
    drone.velZ = 0.1;

    simulate(drone, id);
    logEvent("Flight started", id);
    return end(res, 200, { status: "started" });
  }

  // Finish flight
  if (req.method === "POST" && resource === "drones" && action === "finish") {
    const drone = drones.get(id);
    if (!drone) return end(res, 404, {});

    drone.isFlying = false;
    drone.isTraveling = false;
    drone.velX = 0;
    drone.velY = 0;
    drone.velZ = -0.1;
    drone.alt = 0;

    stop(drone);
    logEvent("Flight finished", id);
    return end(res, 200, { status: "finished" });
  }

  end(res, 404, { error: "not found" });
});

/* ---------------- WebSocket ---------------- */

const wss = new WebSocketServer({ server });

wss.on("connection", (ws, req) => {
  const { query } = parse(req.url, true);
  const dboidsID = query.dboidsID;

  if (!dboidsID || !drones.has(dboidsID)) {
    ws.close(1008, "Invalid dboidsID");
    return;
  }

  console.log(`[${dboidsID}] New WS connection`);

  const drone = drones.get(dboidsID);

  ws.send(JSON.stringify(drone));
});

/* ---------------- Simulation ---------------- */

/**
 * @param {Drone} drone
 */
function simulate(drone) {
  if (drone.timer) return;

  drone.timer = setInterval(() => {
    if (!drone.isFlying) return;

    drone.lat += drone.velX * 0.00001;
    drone.lng += drone.velY * 0.00001;
    drone.alt = Math.max(0, drone.alt + drone.velZ);
    drone.batLvl = Math.max(0, drone.batLvl - 0.1);

    broadcast(drone);
  }, 200);
}

/**
 * @param {Drone} drone
 */
function stop(drone) {
  if (drone.timer) {
    clearInterval(drone.timer);
    drone.timer = undefined;
  }
}

/**
 * @param {Drone} data
 */
function broadcast(data) {
  const msg = JSON.stringify(data);
  for (const c of wss.clients) {
    if (c.readyState === 1) c.send(msg);
  }
}

function end(res, code, payload) {
  res.statusCode = code;
  if (code === 204) return res.end();
  res.end(JSON.stringify(payload));
}

/* ---------------- Start ---------------- */
for (let i = 1; i <= 3; i++) {
  const drone = {
    id: String(i),
    lat: 39.73426,
    lng: -8.82159,
    homeLocation: { lat: 39.73426, lng: -8.82159 },
    alt: 0,
    velX: 0,
    velY: 0,
    velZ: 0,
    batLvl: 100,
    batTemperature: 25,
    hdg: 0,
    satCount: 10,
    rft: 1,
    isTraveling: false,
    isFlying: false,
    model: "Model",
    online: true,
    isGoingHome: false,
    isHomeLocationSet: true,
    areMotorsOn: false,
    areLightsOn: false,
  };

  drones.set(String(i), drone);
  logEvent("Drone created", String(i));
}

server.listen(PORT, () => {
  console.log(`Simulator running`);
  console.log(`HTTP → http://localhost:${PORT}`);
  console.log(`WS   → ws://localhost:${PORT}?dboidsID=*`);
});
