import { serve } from "bun";

const PORT = 8083;

const baseDrone = {
  id: "Os Homens não têm ids",
  lat: 39.93326067596959,
  lng: -8.893057107925472,
  homeLocation: { lat: 39.93321900000001, lng: -8.892509 },
  alt: 10,
  velX: 1,
  velY: 0,
  velZ: 0,
  batLvl: 95,
  batTemperature: 30.0,
  hdg: 0,
  satCount: 10,
  rft: 0,
  isTraveling: true,
  isFlying: true,
  model: "Mavic 2 Enterprise Advanced",
  online: true,
  isGoingHome: false,
  isHomeLocationSet: true,
  areMotorsOn: true,
  areLightsOn: false
};

serve({
  port: PORT,
  fetch(req, server) {
    if (server.upgrade(req)) return;
    return new Response("Mock Drone WebSocket Server\n");
  },
  websocket: {
    open(ws) {
      console.log("Backend connected.");
      ws.data = { counter: 0 };
      ws.data.timer = setInterval(() => {
        ws.data.counter++;
        const msg = {
          ...baseDrone,
          lat: baseDrone.lat + Math.random() * 0.0001,
          lng: baseDrone.lng + Math.random() * 0.0001,
          batLvl: Math.max(0, baseDrone.batLvl - ws.data.counter * 0.01),
          timestamp: Date.now(),
          counter: ws.data.counter
        };
        ws.send(JSON.stringify(msg));
        console.log(`Times sent: ${ws.data.counter}`)
        console.log(`Drone at ${msg.lat},${msg.lng} with ${msg.batLvl}% battery`);
      }, 1000);
    },
    close(ws) {
      clearInterval(ws.data.timer);
      console.log("Backend disconnected.");
    }
  }
});

console.log(`Mock drone server ready at ws://localhost:${PORT}`);
