function getApiBaseUrl() {
  return document.getElementById("apiBaseUrl").value.trim();
}

// ======================
// Admin: Create shipment
// ======================
async function createShipment() {
  const baseUrl = getApiBaseUrl();
  const body = {
    senderName: document.getElementById("senderName").value,
    senderPhone: document.getElementById("senderPhone").value,
    receiverName: document.getElementById("receiverName").value,
    receiverPhone: document.getElementById("receiverPhone").value,
    originBranchId: Number(document.getElementById("originBranchId").value),
    destinationBranchId: Number(document.getElementById("destinationBranchId").value),
    assignedDriverId: null
  };

  const resultEl = document.getElementById("createResult");
  resultEl.textContent = "Loading...";

  try {
    const res = await fetch(`${baseUrl}/api/Shipments`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(body)
    });

    const data = await res.json();
    resultEl.textContent = JSON.stringify(data, null, 2);

    if (data.trackingCode) {
      // auto-fill track + driver fields
      document.getElementById("trackCode").value = data.trackingCode;
      const eventCodeInput = document.getElementById("eventTrackingCode");
      if (eventCodeInput && !eventCodeInput.value) {
        eventCodeInput.value = data.trackingCode;
      }
    }
  } catch (err) {
    resultEl.textContent = "Error: " + err;
  }
}

// ======================
// Customer: Track shipment
// ======================
async function trackShipment() {
  const baseUrl = getApiBaseUrl();
  const code = document.getElementById("trackCode").value.trim();
  const container = document.getElementById("trackResult");
  container.textContent = "Loading...";

  try {
    const res = await fetch(`${baseUrl}/api/Shipments/${encodeURIComponent(code)}`);

    if (!res.ok) {
      const error = await res.json().catch(() => ({}));
      container.textContent = `Error ${res.status}: ${error.message || "Failed to track shipment"}`;
      return;
    }

    const data = await res.json();
    container.innerHTML = buildDetailsHtml(data);
  } catch (err) {
    container.textContent = "Error: " + err;
  }
}

function buildDetailsHtml(data) {
  const eventsHtml = data.events && data.events.length
    ? data.events.map(ev => `
        <li class="mb-1">
          <span class="font-semibold">${ev.status}</span> –
          ${ev.description}
          <span class="text-xs text-gray-500">(${ev.createdAt})</span>
          ${ev.locationText ? `<span class="text-xs text-gray-400"> @ ${ev.locationText}</span>` : ""}
        </li>
      `).join("")
    : "<li>No events</li>";

  return `
    <div class="border rounded p-3 bg-slate-50">
      <p><span class="font-semibold">Tracking:</span> ${data.trackingCode}</p>
      <p><span class="font-semibold">Status:</span> ${data.status}</p>
      <p><span class="font-semibold">From:</span> ${data.originBranchName}</p>
      <p><span class="font-semibold">To:</span> ${data.destinationBranchName}</p>
      <p class="mt-2 font-semibold">Events:</p>
      <ul class="list-disc list-inside text-sm">
        ${eventsHtml}
      </ul>
    </div>
  `;
}

// ======================
// Admin: List shipments
// ======================
async function loadShipments() {
  const baseUrl = getApiBaseUrl();
  const statusValue = document.getElementById("statusFilter").value;
  const tableEl = document.getElementById("shipmentsTable");
  tableEl.textContent = "Loading...";

  let url = `${baseUrl}/api/Shipments`;
  if (statusValue !== "") {
    url += `?status=${encodeURIComponent(statusValue)}`;
  }

  try {
    const res = await fetch(url);
    if (!res.ok) {
      const error = await res.json().catch(() => ({}));
      tableEl.textContent = `Error ${res.status}: ${error.message || "Failed to load shipments"}`;
      return;
    }

    const data = await res.json();
    if (!Array.isArray(data) || data.length === 0) {
      tableEl.textContent = "No shipments found.";
      return;
    }

    const rows = data.map(s => `
      <tr class="border-b">
        <td class="px-2 py-1">${s.id}</td>
        <td class="px-2 py-1">${s.trackingCode}</td>
        <td class="px-2 py-1">${statusText(s.status)}</td>
        <td class="px-2 py-1">${s.originBranchName}</td>
        <td class="px-2 py-1">${s.destinationBranchName}</td>
        <td class="px-2 py-1">${s.assignedDriverId ?? ""}</td>
        <td class="px-2 py-1 text-[10px]">${s.createdAt}</td>
      </tr>
    `).join("");

    tableEl.innerHTML = `
      <table class="min-w-full text-left border text-[11px]">
        <thead class="bg-slate-200">
          <tr>
            <th class="px-2 py-1">Id</th>
            <th class="px-2 py-1">Tracking</th>
            <th class="px-2 py-1">Status</th>
            <th class="px-2 py-1">From</th>
            <th class="px-2 py-1">To</th>
            <th class="px-2 py-1">DriverId</th>
            <th class="px-2 py-1">CreatedAt</th>
          </tr>
        </thead>
        <tbody>
          ${rows}
        </tbody>
      </table>
    `;
  } catch (err) {
    tableEl.textContent = "Error: " + err;
  }
}

function statusText(value) {
  switch (value) {
    case 0: return "Pending";
    case 1: return "InTransit";
    case 2: return "Delivered";
    case 3: return "Cancelled";
    default: return String(value);
  }
}

// ======================
// Driver: Add event / update status
// ======================
async function addEvent() {
  const baseUrl = getApiBaseUrl();

  let trackingCode = document.getElementById("eventTrackingCode").value.trim();
  if (!trackingCode) {
    // fallback to main tracking input
    trackingCode = document.getElementById("trackCode").value.trim();
  }

  const status = Number(document.getElementById("eventStatus").value);
  const description = document.getElementById("eventDescription").value;
  const locationText = document.getElementById("eventLocationText").value;

  const latRaw = document.getElementById("eventLat").value.trim();
  const lngRaw = document.getElementById("eventLng").value.trim();

  const body = {
    status,
    description,
    locationText: locationText || null,
    lat: latRaw ? Number(latRaw) : null,
    lng: lngRaw ? Number(lngRaw) : null
  };

  const resultEl = document.getElementById("eventResult");
  resultEl.textContent = "Loading...";

  try {
    const res = await fetch(`${baseUrl}/api/Shipments/${encodeURIComponent(trackingCode)}/events`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(body)
    });

    if (!res.ok) {
      const error = await res.json().catch(() => ({}));
      resultEl.textContent = `Error ${res.status}: ${error.message || "Failed to add event"}`;
      return;
    }

    const data = await res.json();
    resultEl.textContent = "Event added. Current status: " + data.status;

    // refresh customer view if same tracking code is there
    if (document.getElementById("trackCode").value.trim() === trackingCode) {
      document.getElementById("trackResult").innerHTML = buildDetailsHtml(data);
    }
  } catch (err) {
    resultEl.textContent = "Error: " + err;
  }
}
