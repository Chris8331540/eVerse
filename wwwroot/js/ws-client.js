(function(){
 const statusEl = document.getElementById('conn-status');
 const outputEl = document.getElementById('output');

 function log(msg){
 if(!outputEl) return;
 outputEl.textContent = msg + '\n' + outputEl.textContent;
 }

 // Build WebSocket URL using host (works with mdns name or IP)
 const token = 'secret-token';
 const wsUrl = (function(){
 const host = window.location.hostname;
 const port = window.location.port || '5000';
 return `ws://${host}:${port}/ws?token=${token}`;
 })();

 let ws;
 function connect(){
 statusEl.textContent = 'Conectando...';
 ws = new WebSocket(wsUrl);
 ws.onopen = ()=>{ statusEl.textContent = 'Conectado'; log('Conectado a '+wsUrl); };
 ws.onmessage = (ev)=>{ statusEl.textContent = 'Recibiendo'; log(ev.data); };
 ws.onclose = ()=>{ statusEl.textContent = 'Desconectado'; log('Desconectado'); setTimeout(connect,3000); };
 ws.onerror = (e)=>{ statusEl.textContent = 'Error'; log('Error socket'); };
 }

 connect();
})();