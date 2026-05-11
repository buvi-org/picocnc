// maker.js — Main PicoCNC Maker application
// Wires together the 3D viewer, config panel, component visibility toggles,
// BOM table, and structural analysis display.

import { CncViewer } from './viewer.js';
import { ConfigPanel } from './config.js';

// ── Component colour map — consistent visual palette for all 12 components ──
const COMPONENT_COLORS = {
    'BaseFrame':      0x8899aa,
    'WorkBed':        0xaa8844,
    'YRails':         0x667788,
    'GantryUprights': 0x556688,
    'GantryBridge':   0xcc6633,
    'XRails':         0x667788,
    'ZAssembly':      0x99aabb,
    'SpindleMount':   0x555555,
    'MotorMounts':    0x333333,
    'LeadScrews':     0xcccccc,
    'DragChains':     0x444433,
    'Safety':         0xff4444,
};

// ── All 12 CNC component names in display order ──
const ALL_COMPONENTS = [
    'BaseFrame',
    'WorkBed',
    'YRails',
    'GantryUprights',
    'GantryBridge',
    'XRails',
    'ZAssembly',
    'SpindleMount',
    'MotorMounts',
    'LeadScrews',
    'DragChains',
    'Safety',
];

// ==========================================================================
// MakerApp — main orchestrator
// ==========================================================================

class MakerApp {
    constructor() {
        this.viewer = null;
        this.config = null;
        this._lastBuildResult = null;
        this._componentVisibility = new Map(); // name -> boolean
        this._assemblyMeshName = 'Assembly';
        this._buildErrorShown = false;
        this._loadedComponents = new Set(); // track which STLs we've loaded

        this.init();
    }

    async init() {
        // 1. Create viewer (will render a blank scene immediately)
        this.viewer = new CncViewer('viewer');

        // 2. Create config panel
        this.config = new ConfigPanel('config-panel');

        // 3. Build lifecycle events
        window.addEventListener('cnc-build-start', () => this.onBuildStart());
        window.addEventListener('cnc-component-ready', (e) => this.onComponentReady(e.detail));
        window.addEventListener('cnc-built', (e) => this.onBuildComplete(e.detail));

        // 4. Render component visibility toggles
        this.renderComponentToggles();

        // 5. Load config (from backend or local defaults)
        await this.config.loadConfig();
    }

    // ── Build start — clear previous state ──

    onBuildStart() {
        this.viewer.removeAll();
        this._loadedComponents.clear();
        this._lastBuildResult = null;
        this._buildErrorShown = false;

        // Reset info panels
        const bomEl = document.getElementById('bom');
        if (bomEl) bomEl.innerHTML = '<h3>Bill of Materials</h3><p class="muted">Building...</p>';

        const analysisEl = document.getElementById('analysis');
        if (analysisEl) analysisEl.innerHTML = '<h3>Analysis</h3><p class="muted">Building...</p>';
    }

    // ── A component STL is ready — load it into the viewer immediately ──

    async onComponentReady({ name, stlUrl }) {
        if (!name || !stlUrl) {
            console.warn('[maker] component-ready event missing name or stlUrl');
            return;
        }

        if (this._loadedComponents.has(name)) {
            console.debug(`[maker] Component ${name} already loaded, skipping`);
            return;
        }

        const color = COMPONENT_COLORS[name] || 0x888888;
        const visible = this._componentVisibility.has(name)
            ? this._componentVisibility.get(name)
            : true;

        console.debug(`[maker] Loading component: ${name} from ${stlUrl}`);
        await this.viewer.loadStl(name, stlUrl, color, visible);
        this._loadedComponents.add(name);

        // Hide assembly mesh as soon as we have individual components
        this.viewer.setVisible(this._assemblyMeshName, false);
    }

    // ── Build complete — show assembly, BOM, analysis ──

    async onBuildComplete(result) {
        this._lastBuildResult = result;

        if (!result) {
            console.error('[maker] Build result is null/undefined');
            this._showViewerError('Build returned no data');
            return;
        }

        // Load Assembly STL if no individual components loaded
        if (this._loadedComponents.size === 0 && result.assemblyStlUrl) {
            console.debug('[maker] No individual components — loading assembly only');
            await this.viewer.loadStl(this._assemblyMeshName, result.assemblyStlUrl, 0xcccccc, true);
        } else if (result.assemblyStlUrl) {
            // Load assembly hidden (individual components are visible)
            console.debug('[maker] Loading assembly (hidden)');
            await this.viewer.loadStl(this._assemblyMeshName, result.assemblyStlUrl, 0xcccccc, false);
        }

        // If nothing loaded at all, show error in viewer
        if (this._loadedComponents.size === 0 && !result.assemblyStlUrl) {
            this._showViewerError('No STL data — build may have produced empty geometry');
        }

        // Update BOM
        this.renderBom(result.bom);

        // Update analysis
        this.renderAnalysis(result.analysis, result.collisions);
    }

    // ── Show an error message in the viewer area ──

    _showViewerError(message) {
        if (this._buildErrorShown) return;
        this._buildErrorShown = true;

        console.error('[maker]', message);

        // If we have no meshes at all, show error in viewer
        if (this.viewer.getComponents().size === 0) {
            const container = document.getElementById('viewer-container');
            if (container) {
                const overlay = document.createElement('div');
                overlay.className = 'viewer-error-overlay';
                overlay.id = 'viewer-error';
                overlay.innerHTML = `
                    <div class="viewer-error-msg">
                        <span class="viewer-error-icon">&#9888;</span>
                        <p>${this._escapeHtml(message)}</p>
                        <small>Check the browser console (F12) for details.</small>
                    </div>
                `;
                container.appendChild(overlay);
            }
        }
    }

    _escapeHtml(str) {
        const div = document.createElement('div');
        div.textContent = str;
        return div.innerHTML;
    }

    // ── Show collision warning overlay when unexpected overlaps are found ──

    _showCollisionWarning(collisions) {
        const container = document.getElementById('viewer-container');
        if (!container) return;

        // Remove any existing collision warning
        const existing = container.querySelector('.viewer-collision-warning');
        if (existing) existing.remove();

        const overlay = document.createElement('div');
        overlay.className = 'viewer-collision-warning';
        const count = collisions.unexpectedWarnings;
        const detailLines = (collisions.details || [])
            .filter(d => d.startsWith('[WARNING]'))
            .map(d => this._escapeHtml(d.replace('[WARNING] ', '')))
            .join('<br>');

        overlay.innerHTML = `
            <div class="collision-warn-box">
                <strong>&#9888; ${count} unexpected collision${count !== 1 ? 's' : ''} detected</strong>
                ${detailLines ? `<div class="collision-warn-detail">${detailLines}</div>` : ''}
            </div>
        `;
        container.appendChild(overlay);

        // Auto-dismiss after 15 seconds
        setTimeout(() => {
            const warn = container.querySelector('.viewer-collision-warning');
            if (warn) warn.remove();
        }, 15000);
    }

    // ── BOM table ──

    renderBom(bom) {
        const el = document.getElementById('bom');
        if (!el) return;

        if (!bom || bom.length === 0) {
            el.innerHTML = '<h3>Bill of Materials</h3><p class="muted">No data — build to generate</p>';
            return;
        }

        let html = '<h3>Bill of Materials</h3>';
        html += '<table class="bom-table">';
        html += '<thead><tr><th>Axis</th><th>Part</th><th>Type</th><th>Qty</th><th>Spec</th></tr></thead>';
        html += '<tbody>';
        for (const item of bom) {
            html += `<tr>
                <td>${item.axis ?? '—'}</td>
                <td>${item.part ?? '—'}</td>
                <td>${item.type ?? '—'}</td>
                <td class="bom-qty">${item.qty ?? '—'}</td>
                <td class="bom-spec">${item.spec ?? '—'}</td>
            </tr>`;
        }
        html += '</tbody></table>';
        el.innerHTML = html;
    }

    // ── Structural analysis + collisions ──

    renderAnalysis(analysis, collisions) {
        const el = document.getElementById('analysis');
        if (!el) return;

        let html = '';

        if (analysis) {
            html += '<h3>Structural Analysis</h3>';
            html += '<div class="analysis-grid">';

            if (analysis.bridgeDeflectionMm != null) {
                const defl = Number(analysis.bridgeDeflectionMm).toFixed(4);
                html += `<div class="analysis-item">
                    <span class="analysis-label">Bridge Deflection</span>
                    <span class="analysis-value">${defl} mm</span>
                </div>`;
            }
            if (analysis.bridgeSafetyFactor != null) {
                const sf = Number(analysis.bridgeSafetyFactor).toFixed(1);
                html += `<div class="analysis-item">
                    <span class="analysis-label">Bridge Safety Factor</span>
                    <span class="analysis-value ${sf > 3 ? 'ok' : 'warn'}">${sf}x</span>
                </div>`;
            }
            if (analysis.leadScrewBucklingSafety != null) {
                const ls = Number(analysis.leadScrewBucklingSafety).toFixed(1);
                html += `<div class="analysis-item">
                    <span class="analysis-label">Screw Buckling Safety</span>
                    <span class="analysis-value ${ls > 2 ? 'ok' : 'warn'}">${ls}x</span>
                </div>`;
            }

            html += '</div>';
        }

        if (collisions) {
            html += '<h3>Collisions</h3>';

            const hasWarnings = collisions.unexpectedWarnings > 0;
            const totalOverlaps = collisions.overlappingPairs || 0;

            html += '<div class="analysis-grid">';
            html += `<div class="analysis-item">
                <span class="analysis-label">Total Overlapping Pairs</span>
                <span class="analysis-value">${totalOverlaps}</span>
            </div>`;
            html += `<div class="analysis-item">
                <span class="analysis-label">Unexpected Warnings</span>
                <span class="analysis-value ${hasWarnings ? 'warn' : 'ok'}">${collisions.unexpectedWarnings ?? 0}</span>
            </div>`;
            html += '</div>';

            // Show detailed collision list if available
            if (collisions.details && collisions.details.length > 0) {
                html += '<div class="collision-details">';
                for (const detail of collisions.details) {
                    const isWarning = detail.startsWith('[WARNING]');
                    const cls = isWarning ? 'collision-warn' : 'collision-expected';
                    html += `<div class="${cls}">${this._escapeHtml(detail)}</div>`;
                }
                html += '</div>';
            }

            // Flash warning overlay in viewer if unexpected collisions exist
            if (hasWarnings) {
                this._showCollisionWarning(collisions);
            }
        }

        if (!analysis && !collisions) {
            html = '<h3>Analysis</h3><p class="muted">No data — build to generate</p>';
        }

        el.innerHTML = html;
    }

    // ── Component visibility toggles (right panel) ──

    renderComponentToggles() {
        const container = document.getElementById('component-toggles');
        if (!container) return;

        container.innerHTML = '<h3>Components</h3>';

        for (const name of ALL_COMPONENTS) {
            const colorHex = '#' + COMPONENT_COLORS[name].toString(16).padStart(6, '0');
            const checked = true;

            const row = document.createElement('label');
            row.className = 'comp-toggle';

            const checkbox = document.createElement('input');
            checkbox.type = 'checkbox';
            checkbox.checked = checked;
            checkbox.setAttribute('data-component', name);

            const swatch = document.createElement('span');
            swatch.className = 'comp-swatch';
            swatch.style.backgroundColor = colorHex;

            row.appendChild(checkbox);
            row.appendChild(swatch);
            row.appendChild(document.createTextNode(name));
            row.appendChild(swatch.cloneNode()); // visual symmetry

            checkbox.addEventListener('change', () => {
                this.viewer.setVisible(name, checkbox.checked);
                this._componentVisibility.set(name, checkbox.checked);
            });

            container.appendChild(row);
            this._componentVisibility.set(name, checked);
        }
    }

    // ── Placeholder when backend is not available ──

    showPlaceholderInViewer() {
        console.info('[maker] Backend not available. Showing empty viewer with grid.');
    }
}

// ── Boot when DOM is ready ──

document.addEventListener('DOMContentLoaded', () => {
    new MakerApp();
});
