// Isolated loopback fixture serving real built assets/worker with two index versions.
// Never proxies app API calls or forwards cookies to the production stack.
import https from 'node:https';
import http from 'node:http';
import { createHash } from 'node:crypto';

function load(path) {
  if (!path.startsWith('/') || path.startsWith('/api/') || path.includes('..')) throw new Error('Invalid fixture asset path');
  return new Promise((resolve, reject) => {
    https.get({ hostname: 'localhost', port: 8443, path, rejectUnauthorized: false }, response => {
      if (response.statusCode !== 200) { response.resume(); reject(new Error('Production asset fetch failed')); return; }
      const chunks = []; response.on('data', chunk => chunks.push(chunk));
      response.on('end', () => resolve(Buffer.concat(chunks)));
    }).on('error', reject);
  });
}
const original = JSON.parse((await load('/ngsw.json')).toString());
const assets = new Map(await Promise.all([...new Set([...Object.keys(original.hashTable), '/ngsw-worker.js'])]
  .map(async path => [path, await load(path)])));
for (const [path, hash] of Object.entries(original.hashTable)) {
  if (createHash('sha1').update(assets.get(path)).digest('hex') !== hash) throw new Error('Production manifest/asset hash mismatch');
}
const index = assets.get('/index.html').toString();
let version = 'A';
let networkAvailable = true;
const types = { '.js': 'application/javascript', '.css': 'text/css', '.html': 'text/html', '.json': 'application/json', '.webmanifest': 'application/manifest+json', '.png': 'image/png', '.svg': 'image/svg+xml', '.ico': 'image/x-icon' };
http.createServer((request, response) => {
  const path = new URL(request.url, 'http://localhost:4173').pathname;
  response.setHeader('Cache-Control', 'no-store');
  if (path === '/__test/version' && request.method === 'POST') {
    let body = ''; request.on('data', chunk => { body += chunk; });
    request.on('end', () => { if (!['A', 'B', 'broken'].includes(body)) { response.writeHead(400).end(); return; } version = body; networkAvailable = true; response.end('ok'); }); return;
  }
  if (path === '/__test/network' && request.method === 'POST') {
    let body = ''; request.on('data', chunk => { body += chunk; });
    request.on('end', () => { if (!['online', 'offline'].includes(body)) { response.writeHead(400).end(); return; } networkAvailable = body === 'online'; response.end('ok'); }); return;
  }
  if (request.method !== 'GET') { response.writeHead(405).end(); return; }
  if (path === '/__test/ready') { response.end('ready'); return; }
  // Browser offline emulation does not consistently affect worker-owned fetches
  // in every engine. Refuse connections as well, including worker requests.
  if (!networkAvailable) { request.socket.destroy(); return; }
  if (path.startsWith('/api/')) {
    response.setHeader('Content-Type', 'application/json');
    if (path === '/api/antiforgery/token') { response.end(JSON.stringify({ requestToken: 'public-pwa-fixture' })); return; }
    response.writeHead(401).end('{}'); return;
  }
  const html = Buffer.from(index.replace('</head>', `<meta name="plantcare-pwa-probe" content="${version === 'A' ? 'A' : 'B'}"></head>`));
  if (path === '/ngsw.json') {
    const manifest = structuredClone(original);
    manifest.appData = { pwaAcceptanceVersion: version };
    manifest.hashTable['/index.html'] = version === 'broken' ? '0'.repeat(40) : createHash('sha1').update(html).digest('hex');
    response.setHeader('Content-Type', 'application/json'); response.end(JSON.stringify(manifest)); return;
  }
  const assetPath = ['/', '/login', '/plants', '/index.html'].includes(path) ? '/index.html' : path;
  const bytes = assetPath === '/index.html' ? html : assets.get(assetPath);
  if (!bytes) { response.writeHead(404).end(); return; }
  const extension = assetPath.slice(assetPath.lastIndexOf('.'));
  response.setHeader('Content-Type', types[extension] ?? 'application/octet-stream'); response.end(bytes);
}).listen(4173, '127.0.0.1', () => console.log('PWA fixture ready (production assets verified)'));
