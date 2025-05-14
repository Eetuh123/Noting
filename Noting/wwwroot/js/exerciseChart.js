window.exerciseChart = {
    _charts: {},
    create: (canvasId, config) => {
        const ctx = document.getElementById(canvasId).getContext('2d');

        config.options = config.options || {};
        config.options.scales = config.options.scales || {};
        config.options.scales.y2 = config.options.scales.y2 || {};
        config.options.scales.y2.ticks = config.options.scales.y2.ticks || {};
        config.options.scales.y = config.options.scales.y || {};
        config.options.scales.y.ticks = config.options.scales.y.ticks || {};

        config.options.scales.y.ticks.callback = k => k + 'kg';
        config.options.scales.y.position = 'left';
        config.options.scales.y2.ticks.callback = r => r + 'reps';
        config.options.scales.y2.position = 'right';

        config.options.elements = config.options.elements || {};
        config.options.elements.line = config.options.elements.line || {};
        config.options.elements.point = config.options.elements.point || {};
        config.options.elements.line.backgroundColor = '#1A1A1A';
        config.options.elements.point.backgroundColor = '#1A1A1A';
        config.options.elements.point.pointBorderColor = '#1A1A1A';

        window.exerciseChart._charts[canvasId] = new Chart(ctx, config);
    },

    update: (canvasId, newDatasets) => {
        const chart = window.exerciseChart._charts[canvasId];
        if (!chart) return;
        chart.data.datasets = newDatasets;
        chart.update();
    },

    destroy: (CanvasId) => {
        const chart = window.exerciseChart._charts[canvasId];
        if (chart) {
            chart.destroy();
            delete window.exerciseChart._charts[canvasId];
        }
    }
};