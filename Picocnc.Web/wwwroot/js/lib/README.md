# Three.js library files for PicoCNC Maker

This project loads Three.js from CDN via an import map in `index.html`. No local library files are needed.

## Import map (in index.html)

```html
<script type="importmap">
{
    "imports": {
        "three": "https://unpkg.com/three@0.170.0/build/three.module.min.js",
        "three/addons/": "https://unpkg.com/three@0.170.0/examples/jsm/"
    }
}
</script>
```

## Offline usage

If you need to run without internet access, download these files into this directory:

- [three.module.min.js](https://unpkg.com/three@0.170.0/build/three.module.min.js) — rename to `three.module.min.js`
- [OrbitControls.js](https://unpkg.com/three@0.170.0/examples/jsm/controls/OrbitControls.js) — place in `js/lib/controls/OrbitControls.js`
- [STLLoader.js](https://unpkg.com/three@0.170.0/examples/jsm/loaders/STLLoader.js) — place in `js/lib/loaders/STLLoader.js`

Then update the import map in `index.html` to use local paths:

```html
<script type="importmap">
{
    "imports": {
        "three": "/js/lib/three.module.min.js",
        "three/addons/controls/OrbitControls.js": "/js/lib/controls/OrbitControls.js",
        "three/addons/loaders/STLLoader.js": "/js/lib/loaders/STLLoader.js"
    }
}
</script>
```

> Note: This `README.md` is documentation only. The app requires no local Three.js files when using the CDN import map.
