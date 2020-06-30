(function (svgDomId, layerNames, defaultXTranslate, defaultYTranslate, defaultXScale, defaultYScale) {
    let drawing = document.getElementById(svgDomId);
    let translateElement = drawing.querySelectorAll('.svg-translate')[0];
    let scaleElement = drawing.querySelectorAll('.svg-scale')[0];
    let translateTransform = translateElement.transform.baseVal[0];
    let scaleTransform = scaleElement.transform.baseVal[0];
    let layerList = drawing.querySelectorAll('.layers-control')[0];

    function setTranslate(xt, yt) {
        translateTransform.setTranslate(xt, yt);
    }

    function setScale(xs, ys) {
        scaleTransform.setScale(xs, ys);
    }

    function pan(deltaX, deltaY) {
        let xoffset = translateTransform.matrix.e;
        let yoffset = translateTransform.matrix.f;
        setTranslate(xoffset + deltaX, yoffset + deltaY);
    }

    function zoom(scale) {
        let xs = scaleTransform.matrix.a;
        let ys = scaleTransform.matrix.d;
        setScale(xs * scale, ys * scale);
    }

    function doZoom(direction) {
        let scale = direction < 0 ? 1.2 : 0.8;
        zoom(scale);
    }

    function doPan(deltax, deltay) {
        let panAmount = 0.1 * drawing.clientWidth;
        pan(panAmount * deltax, panAmount * deltay);
    }

    layerNames.forEach(layerName => {
        let layerSelector = document.createElement('div');
        layerSelector.setAttribute('class', 'dxf-layer-selector');

        let input = document.createElement('input');
        input.checked = true;
        input.setAttribute('type', 'checkbox');
        input.setAttribute('name', layerName);
        input.addEventListener('click', () => {
            drawing.querySelectorAll(`.dxf-layer.${layerName}`).forEach(layer => {
                layer.style.display = input.checked ? 'block' : 'none';
            });
        });
        layerSelector.appendChild(input);

        let label = document.createElement('label');
        label.setAttribute('for', layerName);
        label.innerText = layerName;
        layerSelector.appendChild(label);

        layerList.appendChild(layerSelector);
    });

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

    drawing.querySelectorAll('.button-reset-view').forEach(button => {
        button.addEventListener('click', () => {
            setTranslate(defaultXTranslate, defaultYTranslate);
            setScale(defaultXScale, defaultYScale);
        });
    });
})('$DRAWING-ID$', $LAYER-NAMES$, $DEFAULT-X-TRANSLATE$, $DEFAULT-Y-TRANSLATE$, $DEFAULT-X-SCALE$, $DEFAULT-Y-SCALE$);
