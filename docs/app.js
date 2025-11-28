function getApiBaseUrl() {
  return document.getElementById("apiBaseUrl").value.trim();
}

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
      document.getElementById("trackCode").value = data.trackingCode;
    }
  } catch (err) {
    resultEl.textContent = "Error: " + err;
  }
}

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

    const eventsHtml = data.events && data.events.length
      ? data.events.map(ev => `
          <li class="mb-1">
            <span class="font-semibold">${ev.status}</span> –
            ${ev.description}
            <span class="text-xs text-gray-500">(${ev.createdAt})</span>
          </li>
        `).join("")
      : "<li>No events</li>";

    container.innerHTML = `
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
  } catch (err) {
    container.textContent = "Error: " + err;
  }
}
