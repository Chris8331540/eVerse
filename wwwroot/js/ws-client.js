(function () {
    const outputEl = document.getElementById('output');

    function log(msg) {
        if (!outputEl) return;
        outputEl.textContent = msg + '\n';
    }

    // Build WebSocket URL using host (works with mdns name or IP)
    const token = 'secret-token';
    const wsUrl = (function () {
        const host = window.location.hostname;
        const port = window.location.port || '5000';
        return `ws://${host}:${port}/ws?token=${token}`;
    })();

    let ws;
    function connect() {
        ws = new WebSocket(wsUrl);
        ws.onopen = () => { console.log('Conectado a ' + wsUrl); };
        ws.onmessage = (ev) => { log(ev.data); };
        ws.onclose = () => { console.log('Desconectado'); setTimeout(connect, 3000); };
        ws.onerror = (e) => { console.log('Error socket'); };
    }

    connect();

    // Theme toggle: cambia la clase del <body> según el estado del checkbox
    function setupThemeToggle() {
        const checkbox = document.querySelector('.theme-checkbox');
        const body = document.body;
        const modeLabel = document.querySelector('.style-mode');
        if (!checkbox || !body) return;

        function updateTheme() {
            if (checkbox.checked) {
                body.classList.remove('white-mode');
                body.classList.add('black-mode');
                if (modeLabel) modeLabel.textContent = 'Modo oscuro';
            } else {
                body.classList.remove('black-mode');
                body.classList.add('white-mode');
                if (modeLabel) modeLabel.textContent = 'Modo claro';
            }
        }

        checkbox.addEventListener('change', updateTheme);
        // Inicializar según el estado actual del checkbox
        updateTheme();
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', setupThemeToggle);
    } else {
        setupThemeToggle();
    }
})();