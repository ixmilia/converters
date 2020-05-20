(function (svgDomId) {
    let drawing = document.getElementById(svgDomId);
    let viewport = drawing.querySelectorAll('.svg-viewport')[0];
    let transforms = viewport.transform.baseVal;

    function pan(transform, deltaX, deltaY) {
        let xoffset = transform.matrix.e;
        let yoffset = transform.matrix.f;
        transform.setTranslate(xoffset + deltaX, yoffset + deltaY);
    }

    function zoom(transform, scale) {
        let xs = transform.matrix.a;
        let ys = transform.matrix.d;
        transform.setScale(xs * scale, ys * scale);
    }

    function getTransformOfType(type) {
        for (let i = 0; i < transforms.length; i++) {
            let transform = transforms[i];
            if (transform.type === type) {
                return transform;
            }
        }
    }

    function getScaleTransform() {
        return getTransformOfType(SVGTransform.SVG_TRANSFORM_SCALE);
    }

    function getTranslateTransform() {
        return getTransformOfType(SVGTransform.SVG_TRANSFORM_TRANSLATE);
    }

    function doZoom(direction) {
        let scale = direction < 0 ? 1.2 : 0.8;
        let transform = getScaleTransform();
        zoom(transform, scale);
    }

    function doPan(deltax, deltay) {
        let panAmount = 0.1 * drawing.clientWidth;
        let transform = getTranslateTransform();
        pan(transform, panAmount * deltax, panAmount * deltay);
    }

    drawing.querySelectorAll('.button-zoom-out').forEach(button => {
        button.addEventListener('click', () => doZoom(1));
    });

    drawing.querySelectorAll('.button-zoom-in').forEach(button => {
        button.addEventListener('click', () => doZoom(-1));
    });

    drawing.querySelectorAll('.button-pan-left').forEach(button => {
        button.addEventListener('click', () => doPan(-1, 0));
    });

    drawing.querySelectorAll('.button-pan-right').forEach(button => {
        button.addEventListener('click', () => doPan(1, 0));
    });

    drawing.querySelectorAll('.button-pan-up').forEach(button => {
        button.addEventListener('click', () => doPan(0, -1));
    });

    drawing.querySelectorAll('.button-pan-down').forEach(button => {
        button.addEventListener('click', () => doPan(0, 1));
    });
})('$DRAWING-ID$');
