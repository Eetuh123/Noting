window.exerciseChart = {
    create: (canvasId, config) => {
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

        const ctx = document.getElementById(canvasId).getContext('2d');
        return new Chart(ctx, config);
    },

    update: (chart, newData) => {
        chart.data.datasets = newData.datasets;
        chart.update();
    },

    destroy: (chart) => {
        chart.destroy();
    }
};