// viewer.js — Three.js STL viewer for PicoCNC Maker
// Displays generated CNC machine components as 3D models.
// Uses STLLoader to load individual STL files and OrbitControls for camera.

import * as THREE from 'three';
import { OrbitControls } from 'three/addons/controls/OrbitControls.js';
import { STLLoader } from 'three/addons/loaders/STLLoader.js';

export class CncViewer {
    constructor(canvasId) {
        this.canvas = document.getElementById(canvasId);
        if (!this.canvas) {
            console.error('[viewer] Canvas element not found:', canvasId);
            return;
        }
        this.components = new Map();  // name -> { mesh, visible }
        this.loader = new STLLoader();
        this._animationId = null;
        this.init();
    }

    // ── initialise Three.js scene, camera, renderer, lights, grid ──

    init() {
        const rect = this.canvas.parentElement.getBoundingClientRect();

        // Scene
        this.scene = new THREE.Scene();
        this.scene.background = new THREE.Color(0x1a1a2e);

        // Camera — perspective, start at an isometric-ish angle
        this.camera = new THREE.PerspectiveCamera(
            50,
            Math.max(rect.width, 1) / Math.max(rect.height, 1),
            0.1,
            10000
        );
        this.camera.position.set(800, 600, 800);
        this.camera.lookAt(0, 200, 0);

        // Renderer
        this.renderer = new THREE.WebGLRenderer({ canvas: this.canvas, antialias: true });
        this.renderer.setSize(rect.width, rect.height);
        this.renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2));

        // ── Lighting ──
        // Ambient — fills in shadows so no part is pitch black
        this.scene.add(new THREE.AmbientLight(0x404060, 2));

        // Key light — bright directional from upper-front-right
        const keyLight = new THREE.DirectionalLight(0xffffff, 3);
        keyLight.position.set(1, 1, 0.5);
        this.scene.add(keyLight);

        // Fill light — softer, from upper-front-left
        const fillLight = new THREE.DirectionalLight(0x8888cc, 1.5);
        fillLight.position.set(-1, 0.5, -0.5);
        this.scene.add(fillLight);

        // Back light — rim light from behind
        const backLight = new THREE.DirectionalLight(0x4466aa, 1);
        backLight.position.set(0, 0.3, -1);
        this.scene.add(backLight);

        // ── Ground grid ──
        const grid = new THREE.GridHelper(800, 20, 0x333355, 0x222244);
        this.scene.add(grid);

        // ── OrbitControls ──
        this.controls = new OrbitControls(this.camera, this.renderer.domElement);
        this.controls.target.set(250, 200, 200);
        this.controls.enableDamping = true;
        this.controls.dampingFactor = 0.12;
        this.controls.update();

        // ── Resize ──
        window.addEventListener('resize', () => this.resize());

        // ── Render loop ──
        this.animate();
    }

    // ── render loop ──

    animate() {
        this._animationId = requestAnimationFrame(() => this.animate());
        this.controls.update();
        this.renderer.render(this.scene, this.camera);
    }

    // ── resize handler ──

    resize() {
        const rect = this.canvas.parentElement.getBoundingClientRect();
        if (rect.width === 0 || rect.height === 0) return;
        this.camera.aspect = rect.width / rect.height;
        this.camera.updateProjectionMatrix();
        this.renderer.setSize(rect.width, rect.height);
    }

    // ── load an STL and add to the scene ──

    /**
     * Fetches an STL from url, creates a mesh, and adds it to the scene.
     * If a component with the same name already exists, it is removed first.
     *
     * @param {string} name    — unique key for this component
     * @param {string} url     — URL to fetch the STL from (e.g. /api/stl/BaseFrame)
     * @param {number} color   — hex colour for the mesh material
     * @param {boolean} visible — initial visibility (default true)
     */
    async loadStl(name, url, color = 0x8899aa, visible = true) {
        try {
            // Remove existing mesh for this name
            this.remove(name);

            const geometry = await this.loader.loadAsync(url);

            // PicoGK uses Z-up (base on XY plane), Three.js uses Y-up (grid on XZ plane).
            // Rotate geometry so the machine stands upright on the ground grid.
            geometry.rotateX(-Math.PI / 2);

            const material = new THREE.MeshStandardMaterial({
                color: color,
                roughness: 0.5,
                metalness: 0.3,
                flatShading: false,
            });
            const mesh = new THREE.Mesh(geometry, material);
            mesh.name = name;
            mesh.visible = visible;

            this.scene.add(mesh);
            this.components.set(name, { mesh, visible });

            // Auto-fit camera on first component loaded
            if (this.components.size === 1) {
                geometry.computeBoundingBox();
                const box = geometry.boundingBox;
                const center = box.getCenter(new THREE.Vector3());
                const size = box.getSize(new THREE.Vector3());
                const maxDim = Math.max(size.x, size.y, size.z);
                // Position camera up and to the side for an isometric-like view
                this.camera.position.set(
                    center.x + maxDim * 0.9,
                    center.y + maxDim * 0.6,
                    center.z + maxDim * 0.9
                );
                this.controls.target.copy(center);
                this.controls.update();
            }

            console.debug(`[viewer] Loaded: ${name} (${url})`);
        } catch (err) {
            console.error(`[viewer] Failed to load STL "${name}" from ${url}:`, err.message);
        }
    }

    // ── remove a single component ──

    remove(name) {
        const comp = this.components.get(name);
        if (!comp) return;

        this.scene.remove(comp.mesh);
        if (comp.mesh.geometry) comp.mesh.geometry.dispose();
        if (comp.mesh.material) comp.mesh.material.dispose();
        this.components.delete(name);
    }

    // ── remove all components ──

    removeAll() {
        for (const [name] of this.components) {
            this.remove(name);
        }
    }

    // ── visibility helpers ──

    setVisible(name, visible) {
        const comp = this.components.get(name);
        if (comp) {
            comp.mesh.visible = visible;
            comp.visible = visible;
        }
    }

    toggleVisible(name) {
        const comp = this.components.get(name);
        if (comp) {
            this.setVisible(name, !comp.visible);
            return comp.visible;
        }
        return false;
    }

    /** Returns a Map of component name -> { mesh, visible } */
    getComponents() {
        return this.components;
    }

    // ── cleanup ──

    dispose() {
        if (this._animationId) {
            cancelAnimationFrame(this._animationId);
            this._animationId = null;
        }
        this.removeAll();
        if (this.renderer) {
            this.renderer.dispose();
        }
    }
}
