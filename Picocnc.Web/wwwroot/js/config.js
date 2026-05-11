// config.js — Parameter configuration panel for PicoCNC Maker
// Renders parameter controls (sliders, dropdowns) and preset buttons.
// Communicates with /api/* endpoints on the backend.

// ── Hardcoded default parameters — used when backend /api/config is unavailable ──
// These mirror the defaults in Picocnc_Parameters.cs exactly.

const DEFAULT_PARAMETERS = {
    'Envelope': [
        { key: 'fWorkAreaX',  label: 'Work Area X',      value: 500,  unit: 'mm', min: 200,  max: 2000, step: 50 },
        { key: 'fWorkAreaY',  label: 'Work Area Y',      value: 400,  unit: 'mm', min: 200,  max: 1500, step: 50 },
        { key: 'fWorkAreaZ',  label: 'Work Area Z',      value: 120,  unit: 'mm', min: 50,   max: 400,  step: 10 },
        { key: 'fBaseOuterZ', label: 'Base Outer Z',     value: 150,  unit: 'mm', min: 80,   max: 400,  step: 10 },
    ],
    'Wall Thicknesses': [
        { key: 'fBaseWallThick',   label: 'Base Wall',        value: 15,  unit: 'mm', min: 5,  max: 40, step: 1 },
        { key: 'fRibThick',        label: 'Rib Thickness',    value: 10,  unit: 'mm', min: 5,  max: 30, step: 1 },
        { key: 'fGantryWallThick', label: 'Gantry Wall',      value: 8,   unit: 'mm', min: 3,  max: 20, step: 1 },
    ],
    'Ribs': [
        { key: 'fRibSpacing', label: 'Rib Spacing', value: 120, unit: 'mm', min: 60, max: 200, step: 10 },
    ],
    'Rail Dimensions': [
        { key: 'fRailWidth',    label: 'Rail Width',     value: 20,  unit: 'mm', min: 10, max: 40,  step: 1 },
        { key: 'fRailHeight',   label: 'Rail Height',    value: 25,  unit: 'mm', min: 15, max: 50,  step: 1 },
        { key: 'fRailInsetX',   label: 'Rail Inset X',   value: 30,  unit: 'mm', min: 10, max: 60,  step: 5 },
        { key: 'fBoltHoleDia',  label: 'Bolt Hole Dia',  value: 5.2, unit: 'mm', min: 3,  max: 10,  step: 0.2 },
        { key: 'fBoltSpacingY', label: 'Bolt Spacing Y', value: 80,  unit: 'mm', min: 40, max: 120, step: 10 },
    ],
    'Uprights': [
        { key: 'fUprightX', label: 'Upright X', value: 40,  unit: 'mm', min: 20, max: 80,  step: 5 },
        { key: 'fUprightY', label: 'Upright Y', value: 60,  unit: 'mm', min: 30, max: 100, step: 5 },
        { key: 'fUprightZ', label: 'Upright Z', value: 200, unit: 'mm', min: 100, max: 400, step: 10 },
    ],
    'Gantry Bridge': [
        { key: 'fGantryBridgeY', label: 'Bridge Depth (Y)', value: 60, unit: 'mm', min: 30, max: 100, step: 5 },
        { key: 'fGantryBridgeZ', label: 'Bridge Height (Z)', value: 80, unit: 'mm', min: 40, max: 150, step: 5 },
    ],
    'Z-Axis': [
        { key: 'fZPlateX',    label: 'Z Plate X',    value: 80,  unit: 'mm', min: 40,  max: 160, step: 10 },
        { key: 'fZPlateY',    label: 'Z Plate Y',    value: 15,  unit: 'mm', min: 8,   max: 30,  step: 2 },
        { key: 'fZPlateZ',    label: 'Z Plate Z',    value: 250, unit: 'mm', min: 100, max: 400, step: 10 },
        { key: 'fZRailSpace', label: 'Z Rail Space', value: 50,  unit: 'mm', min: 30,  max: 80,  step: 5 },
        { key: 'fZRailSize',  label: 'Z Rail Size',  value: 15,  unit: 'mm', min: 8,   max: 25,  step: 2 },
    ],
    'Spindle': [
        { key: 'fSpindleOD',   label: 'Spindle OD',     value: 65, unit: 'mm', min: 40,  max: 120, step: 5 },
        { key: 'fClampOD',     label: 'Clamp OD',       value: 80, unit: 'mm', min: 50,  max: 140, step: 5 },
        { key: 'fClampHeight', label: 'Clamp Height',   value: 60, unit: 'mm', min: 30,  max: 100, step: 5 },
        { key: 'fClampSlit',   label: 'Clamp Slit',     value: 3,  unit: 'mm', min: 1,   max: 6,   step: 0.5 },
    ],
    'Motor Mounts': [
        { key: 'fNema23Width',      label: 'NEMA 23 Width',      value: 57,   unit: 'mm', min: 40, max: 70, step: 1 },
        { key: 'fNema23BoltCircle', label: 'NEMA 23 Bolt Circle', value: 47.14, unit: 'mm', min: 30, max: 60, step: 1 },
        { key: 'fNema23ShaftBore',  label: 'Shaft Bore',        value: 12,   unit: 'mm', min: 8,  max: 20, step: 0.5 },
        { key: 'fMountPlateThick',  label: 'Mount Plate',       value: 8,    unit: 'mm', min: 4,  max: 20, step: 1 },
    ],
    'Lead Screws': [
        { key: 'fLeadScrewDia', label: 'Lead Screw Dia', value: 12, unit: 'mm', min: 8,  max: 20, step: 1 },
        { key: 'fNutBlockSize', label: 'Nut Block Size', value: 25, unit: 'mm', min: 15, max: 40, step: 5 },
    ],
    'T-Slot': [
        { key: 'fTSlotUpperW',  label: 'T-Slot Upper W',  value: 10,  unit: 'mm', min: 10, max: 30,  step: 2 },
        { key: 'fTSlotLowerW',  label: 'T-Slot Lower W',  value: 10,  unit: 'mm', min: 5,  max: 15,  step: 1 },
        { key: 'fTSlotDepth',   label: 'T-Slot Depth',    value: 10,  unit: 'mm', min: 5,  max: 20,  step: 1 },
        { key: 'fTSlotSpacing', label: 'T-Slot Spacing',  value: 100, unit: 'mm', min: 50, max: 200, step: 10 },
    ],
    'Work Bed': [
        { key: 'fTableThick', label: 'Table Thickness', value: 20, unit: 'mm', min: 10, max: 40, step: 2 },
    ],
    'Drag Chain': [
        { key: 'fChainWidth',  label: 'Chain Width',  value: 30, unit: 'mm', min: 15, max: 50, step: 5 },
        { key: 'fChainHeight', label: 'Chain Height', value: 20, unit: 'mm', min: 10, max: 40, step: 5 },
    ],
    'Voxel Resolution': [
        { key: 'fVoxelSizeMM', label: 'Voxel Size', value: 2.0, unit: 'mm', min: 0.5, max: 4.0, step: 0.5 },
    ],
    'Material & Budget': [
        { key: 'eCutMaterial', label: 'Material to Cut', value: 'Aluminum', options: ['Wood', 'Aluminum', 'Steel', 'Composites'] },
        { key: 'eBudgetTier',  label: 'Budget Tier',     value: 'Standard', options: ['Budget', 'Standard', 'Premium'] },
    ],
};

// ── Built-in presets matching Picocnc_Presets.cs ──

const DEFAULT_PRESETS = [
    { strName: 'Mini',       strDescription: '~A4 desktop engraver/router — lowest cost entry point' },
    { strName: 'Desktop',    strDescription: 'Typical 500x400 hobby CNC router — most popular DIY size' },
    { strName: 'Workbench',  strDescription: 'Mid-size machine for larger projects on a dedicated bench' },
    { strName: 'Full Sheet', strDescription: 'Full 4x4 ft sheet capability — semi-production machine' },
    { strName: 'Steel Mill', strDescription: 'Rigid machine for mild steel milling — low-speed high-torque' },
];

// ==========================================================================
// ConfigPanel — manages the left-side parameter configuration UI
// ==========================================================================

export class ConfigPanel {
    constructor(containerId) {
        this.container = document.getElementById(containerId);
        if (!this.container) {
            console.error('[config] Container element not found:', containerId);
            return;
        }
        this.params = [];              // flat list of all param defs
        this.onParamChange = null;     // callback(updates) when user changes any param
        this._backendAvailable = false;
        this._ws = null;               // active WebSocket during build
    }

    // ── Load config from backend or fall back to local defaults ──

    async loadConfig() {
        let paramGroups = null;

        try {
            const resp = await fetch('/api/config');
            if (resp.ok) {
                const data = await resp.json();
                paramGroups = data.parameters;
                this._backendAvailable = true;
                console.debug('[config] Loaded config from /api/config');
            } else {
                console.warn(`[config] /api/config returned ${resp.status} — using local defaults`);
            }
        } catch (err) {
            console.warn('[config] Backend /api/config not available:', err.message);
        }

        if (!paramGroups) {
            paramGroups = DEFAULT_PARAMETERS;
            this._backendAvailable = false;
        }

        this.render(paramGroups);
    }

    // ── Render all parameter sections ──

    render(paramGroups) {
        this.container.innerHTML = '';
        this.params = [];

        // ── Header ──
        const header = document.createElement('h1');
        header.textContent = 'PicoCNC Maker';
        this.container.appendChild(header);

        // ── Backend status banner ──
        if (!this._backendAvailable) {
            const banner = document.createElement('div');
            banner.className = 'api-banner';
            banner.textContent = 'Backend API not available — using local defaults.';
            this.container.appendChild(banner);
        }

        // ── Parameter sections ──
        for (const [category, params] of Object.entries(paramGroups)) {
            const section = document.createElement('div');
            section.className = 'param-section';

            const sectionHeader = document.createElement('h3');
            sectionHeader.textContent = category;
            section.appendChild(sectionHeader);

            for (const param of params) {
                const row = this.createParamRow(param);
                section.appendChild(row);
                this.params.push(param);
            }

            this.container.appendChild(section);
        }

        // ── Presets ──
        this.renderPresets();

        // ── Build button ──
        const buildBtn = document.createElement('button');
        buildBtn.id = 'btn-build';
        buildBtn.textContent = 'BUILD CNC';
        buildBtn.className = 'btn-primary';
        buildBtn.addEventListener('click', () => this.triggerBuild());
        this.container.appendChild(buildBtn);

        // ── Build status area ──
        const statusArea = document.createElement('div');
        statusArea.id = 'build-status-area';
        statusArea.innerHTML = `
            <div id="build-status"></div>
            <div id="build-progress-bar"><div id="build-progress-fill"></div></div>
        `;
        this.container.appendChild(statusArea);
    }

    // ── Create a parameter row (slider or dropdown) ──

    createParamRow(param) {
        const row = document.createElement('div');
        row.className = 'param-row';

        // Label
        const label = document.createElement('label');
        label.textContent = param.label;
        row.appendChild(label);

        if (param.options) {
            // ── Dropdown (for enum-style params: material, budget) ──
            const select = document.createElement('select');
            select.setAttribute('data-key', param.key);

            for (const opt of param.options) {
                const option = document.createElement('option');
                option.value = opt;
                option.textContent = opt;
                if (opt === param.value) option.selected = true;
                select.appendChild(option);
            }

            select.addEventListener('change', () => {
                this.sendUpdate({ [param.key]: select.value });
            });

            row.appendChild(select);
        } else {
            // ── Slider + value display (for numeric params) ──
            const slider = document.createElement('input');
            slider.type = 'range';
            slider.setAttribute('data-key', param.key);
            slider.min = param.min;
            slider.max = param.max;
            slider.step = param.step;
            slider.value = param.value;

            const display = document.createElement('span');
            display.className = 'param-value';
            display.textContent = `${Number(param.value)} ${param.unit}`;

            slider.addEventListener('input', () => {
                display.textContent = `${Number(slider.value)} ${param.unit}`;
            });

            slider.addEventListener('change', () => {
                const val = parseFloat(slider.value);
                display.textContent = `${val} ${param.unit}`;
                this.sendUpdate({ [param.key]: val });
            });

            row.appendChild(slider);
            row.appendChild(display);
        }

        return row;
    }

    // ── Render preset buttons ──

    async renderPresets() {
        let presets = null;

        if (this._backendAvailable) {
            try {
                const resp = await fetch('/api/presets');
                if (resp.ok) {
                    presets = await resp.json();
                }
            } catch (err) {
                console.warn('[config] /api/presets unavailable:', err.message);
            }
        }

        if (!presets) {
            presets = DEFAULT_PRESETS;
        }

        const section = document.createElement('div');
        section.className = 'param-section';
        section.innerHTML = '<h3>Presets</h3>';

        const grid = document.createElement('div');
        grid.className = 'preset-grid';

        for (const preset of presets) {
            const btn = document.createElement('button');
            btn.className = 'btn-preset';
            btn.textContent = preset.strName;
            btn.title = preset.strDescription;

            btn.addEventListener('click', async () => {
                if (this._backendAvailable) {
                    try {
                        await fetch(`/api/preset/${preset.strName}`, { method: 'POST' });
                        await this.loadConfig();  // refresh panel from backend
                    } catch (err) {
                        console.error(`[config] Failed to apply preset ${preset.strName}:`, err.message);
                    }
                } else {
                    console.warn('[config] Backend not available — cannot apply preset');
                }
            });

            grid.appendChild(btn);
        }

        section.appendChild(grid);
        this.container.appendChild(section);
    }

    // ── Send parameter update to backend ──

    async sendUpdate(updates) {
        if (this._backendAvailable) {
            try {
                await fetch('/api/config', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify(updates),
                });
                console.debug('[config] Sent update:', updates);
            } catch (err) {
                console.error('[config] Failed to send update:', err.message);
            }
        }

        // Always notify listeners — even offline, UI should react
        if (this.onParamChange) {
            this.onParamChange(updates);
        }
    }

    // ── Trigger build via WebSocket ──

    async triggerBuild() {
        const status = document.getElementById('build-status');
        const btn = document.getElementById('btn-build');
        const progFill = document.getElementById('build-progress-fill');

        // Close any existing WebSocket
        if (this._ws) {
            this._ws.close();
            this._ws = null;
        }

        this._setBuildUI('building', btn, status, progFill);
        status.textContent = 'Connecting...';
        this._updateProgress(progFill, 0);

        // Announce build started (so maker.js can clear viewer)
        window.dispatchEvent(new CustomEvent('cnc-build-start'));

        try {
            const protocol = location.protocol === 'https:' ? 'wss:' : 'ws:';
            const wsUrl = `${protocol}//${location.host}/api/build/ws`;
            const ws = new WebSocket(wsUrl);
            this._ws = ws;
            let componentCount = 0;
            const totalComponents = 12;

            ws.onopen = () => {
                console.debug('[config] WebSocket connected');
            };

            ws.onmessage = (evt) => {
                try {
                    const msg = JSON.parse(evt.data);

                    switch (msg.type) {
                        case 'stage':
                            status.textContent = msg.stage;
                            // Advance progress bar slightly for each stage
                            // (stages before components get ~20% of the bar)
                            if (progFill) {
                                const cur = parseFloat(progFill.style.width || '0');
                                if (cur < 20) {
                                    this._updateProgress(progFill, Math.min(20, cur + 2));
                                }
                            }
                            break;

                        case 'component':
                            componentCount++;
                            status.textContent = `Exported ${msg.name} (${componentCount}/${totalComponents})`;
                            const compPct = 20 + Math.round((componentCount / totalComponents) * 75);
                            this._updateProgress(progFill, compPct);
                            // Dispatch event so viewer loads this component immediately
                            window.dispatchEvent(new CustomEvent('cnc-component-ready', {
                                detail: { name: msg.name, stlUrl: msg.stlUrl }
                            }));
                            break;

                        case 'complete':
                            this._updateProgress(progFill, 100);
                            const duration = msg.result?.durationSec != null
                                ? `${msg.result.durationSec.toFixed(1)}s`
                                : 'unknown duration';
                            status.textContent = `Build complete in ${duration}`;
                            status.className = 'status-ok';
                            progFill?.classList.add('done');
                            this._setBuildUI('done', btn);
                            // Dispatch final result
                            window.dispatchEvent(new CustomEvent('cnc-built', { detail: msg.result }));
                            ws.close();
                            this._ws = null;
                            break;

                        case 'error':
                            status.textContent = 'Build failed — check console for details';
                            status.className = 'status-error';
                            this._setBuildUI('error', btn);
                            console.error('[config] Build error:', msg.error);
                            ws.close();
                            this._ws = null;
                            break;

                        default:
                            console.debug('[config] Unknown WS message type:', msg.type);
                    }
                } catch (err) {
                    console.error('[config] Failed to parse WebSocket message:', err.message);
                }
            };

            ws.onerror = (err) => {
                console.error('[config] WebSocket error:', err);
                status.textContent = 'Connection error — retry?';
                status.className = 'status-error';
                this._setBuildUI('error', btn);
            };

            ws.onclose = (evt) => {
                console.debug(`[config] WebSocket closed: code=${evt.code} reason=${evt.reason}`);
                if (evt.code !== 1000 && evt.code !== 1005) {
                    // Abnormal close — show error if we don't already have one
                    if (status.className !== 'status-ok' && status.className !== 'status-error') {
                        status.textContent = `Connection closed unexpectedly (code ${evt.code})`;
                        status.className = 'status-error';
                        this._setBuildUI('error', btn);
                    }
                }
                this._ws = null;
            };

        } catch (err) {
            status.textContent = `Failed to connect: ${err.message}`;
            status.className = 'status-error';
            this._setBuildUI('error', btn);
            console.error('[config] WebSocket connection failed:', err.message);
        }
    }

    // ── Update build button / status UI state ──

    _setBuildUI(state, btn, status, progFill) {
        if (state === 'building') {
            if (btn) {
                btn.disabled = true;
                btn.textContent = 'BUILDING...';
            }
            if (status) {
                status.className = 'status-building';
            }
            if (progFill) {
                progFill.classList.remove('done');
                progFill.style.width = '0%';
            }
        } else {
            if (btn) {
                btn.disabled = false;
                btn.textContent = 'BUILD CNC';
            }
        }
    }

    _updateProgress(progFill, pct) {
        if (progFill) {
            progFill.style.width = `${pct}%`;
        }
    }

    /** Returns true if the backend API is reachable. */
    get backendAvailable() {
        return this._backendAvailable;
    }
}
