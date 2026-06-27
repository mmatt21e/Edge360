// Leaflet-backed map module. Keyed by element id so multiple maps can coexist.
const maps = {};

function ctx(elementId) {
    return maps[elementId];
}

export function init(elementId, lat, lon, zoom) {
    if (maps[elementId]) {
        dispose(elementId);
    }
    const map = L.map(elementId).setView([lat, lon], zoom);
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        maxZoom: 19,
        attribution: '© OpenStreetMap contributors'
    }).addTo(map);

    maps[elementId] = { map, members: {}, places: [], route: null };
    // Leaflet needs a size recalculation once the container is laid out.
    setTimeout(() => map.invalidateSize(), 200);
}

export function setMembers(elementId, markers) {
    const c = ctx(elementId);
    if (!c) return;

    Object.values(c.members).forEach(m => c.map.removeLayer(m));
    c.members = {};

    const bounds = [];
    markers.forEach(m => {
        const marker = L.marker([m.lat, m.lon]).addTo(c.map).bindTooltip(m.label, { permanent: false });
        c.members[m.id] = marker;
        bounds.push([m.lat, m.lon]);
    });
    if (bounds.length > 0) {
        c.map.fitBounds(bounds, { padding: [40, 40], maxZoom: 15 });
    }
}

export function upsertMember(elementId, id, label, lat, lon) {
    const c = ctx(elementId);
    if (!c) return;
    if (c.members[id]) {
        c.members[id].setLatLng([lat, lon]);
    } else {
        c.members[id] = L.marker([lat, lon]).addTo(c.map).bindTooltip(label, { permanent: false });
    }
}

export function setPlaces(elementId, circles) {
    const c = ctx(elementId);
    if (!c) return;
    c.places.forEach(p => c.map.removeLayer(p));
    c.places = [];
    circles.forEach(p => {
        const circle = L.circle([p.lat, p.lon], { radius: p.radius, color: '#2563eb', fillOpacity: 0.1 })
            .addTo(c.map).bindTooltip(p.label, { permanent: false });
        c.places.push(circle);
    });
}

export function drawRoute(elementId, route) {
    const c = ctx(elementId);
    if (!c) return;
    if (c.route) c.map.removeLayer(c.route);
    const latlngs = route.map(p => [p[0], p[1]]);
    c.route = L.polyline(latlngs, { color: '#dc2626', weight: 4 }).addTo(c.map);
    if (latlngs.length > 0) c.map.fitBounds(latlngs, { padding: [40, 40] });
}

export function onClick(elementId, dotNetRef) {
    const c = ctx(elementId);
    if (!c) return;
    c.map.on('click', (e) => dotNetRef.invokeMethodAsync('OnMapClick', e.latlng.lat, e.latlng.lng));
}

export function dispose(elementId) {
    const c = ctx(elementId);
    if (!c) return;
    c.map.remove();
    delete maps[elementId];
}
