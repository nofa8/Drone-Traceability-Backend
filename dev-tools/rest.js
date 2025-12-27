#!/usr/bin/env bun
import fetch from "node-fetch";

const droneId = process.argv[2];
if (!droneId) {
  console.error(`Usage: ./rest.js <droneId>`);
  process.exit(1);
}
const BASE_URL = `http://localhost:5101`;
// const BASE_URL = `https://localhost:7008`;

console.log("droneId:", droneId);
console.log("url:", BASE_URL);

/**
 * Calls the telemetry endpoint with cursor-based pagination.
 *
 * @param {Object} options
 * @param {string|null} options.cursor — Cursor timestamp for pagination
 * @param {boolean} options.forward — True for newer page
 * @param {string|null} options.from — ISO timestamp for "from" filter
 * @param {string|null} options.to — ISO timestamp for "to" filter
 * @param {number} options.limit — Page size
 */
async function fetchTelemetry({
  cursor = null,
  forward = false,
  from = null,
  to = null,
  limit = 100,
}) {
  const params = new URLSearchParams();

  params.set("limit", limit.toString());
  if (cursor) params.set("cursor", cursor);
  if (forward) params.set("forward", "true");
  if (from) params.set("from", from);
  if (to) params.set("to", to);

  const url = `${BASE_URL}/api/drones/${droneId}/telemetry?${params.toString()}`;

  try {
    const res = await fetch(url);

    if (!res.ok) {
      console.error("Error status:", res.status, await res.text());
      return null;
    }

    return await res.json();
  } catch (err) {
    console.error("Fetch error:", err);
    return null;
  }
}

async function main() {
  console.log("Fetching first page…");
  const page1 = await fetchTelemetry({ limit: 50 });
  if (!page1) return;

  console.log("Page 1 results:", page1.items.length);
  console.log("NextCursor:", page1.nextCursor);
  console.log("PrevCursor:", page1.prevCursor);

  // Fetch previous page (page2) if prevCursor exists
  let page2 = null;
  if (page1.prevCursor) {
    console.log("\nFetching previous page (older) …");
    page2 = await fetchTelemetry({
      cursor: page1.prevCursor,
      limit: 50,
    });

    if (!page2) return;
    console.log("Page 2 results:", page2.items.length);
    console.log("NextCursor:", page2.nextCursor);
    console.log("PrevCursor:", page2.prevCursor);
  }

  // Fetch previous page (page3) if page2 exists and has prevCursor
  if (page2?.prevCursor) {
    console.log("\nFetching previous page (older) …");
    const page3 = await fetchTelemetry({
      cursor: page2.prevCursor,
      limit: 50,
    });

    if (!page3) return;
    console.log("Page 3 results:", page3.items.length);
    console.log("NextCursor:", page3.nextCursor);
    console.log("PrevCursor:", page3.prevCursor);
  }
}


main();
