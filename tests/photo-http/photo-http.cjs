// Real HTTPS/API/SQL/IPC; generated accounts/images only. No request mocking.
const { chromium } = require('/tests/node_modules/playwright');
const { expect } = require('/tests/node_modules/@playwright/test');
const assert = require('node:assert/strict');
const { randomUUID, randomBytes, createHash } = require('node:crypto');
const fs = require('node:fs');
const origin = 'https://plantcare-photo.test:8443';
const png = process.env.PHOTO_HTTP_PNG ? fs.readFileSync('/fixtures/capacity.png')
  : Buffer.from('iVBORw0KGgoAAAANSUhEUgAAAAgAAAAECAIAAAA8r+mnAAAAEElEQVR4nGNgaGDAjqgnAQAgkRABZnGeigAAAABJRU5ErkJggg==', 'base64');
const fullWidth = png.readUInt32BE(16), fullHeight = png.readUInt32BE(20);
assert.ok(fullWidth <= 1600 && fullHeight <= 1600); // This HTTP fixture does not require full-image resizing.
const thumbScale = Math.min(1, 320 / fullWidth, 320 / fullHeight);
const thumbSize = [Math.round(fullWidth * thumbScale), Math.round(fullHeight * thumbScale)];
let step = 'launch';
let responseStatus;

(async () => {
  const browser = await chromium.launch();
  const context = await browser.newContext({ baseURL: origin, ignoreHTTPSErrors: true, viewport: { width: 390, height: 844 } });
  // An internal Docker network has no default route, so Chromium may report
  // offline despite working local HTTPS. Model the online hint only; every
  // HTTP request still crosses the real isolated network/API/SQL/decoder.
  await context.addInitScript(() => Object.defineProperty(Navigator.prototype, 'onLine', { configurable: true, get: () => true }));
  const other = await browser.newContext({ baseURL: origin, ignoreHTTPSErrors: true });
  async function headers(client) {
    const token = await client.request.get('/api/antiforgery/token'); assert.equal(token.status(), 200);
    return { 'X-XSRF-TOKEN': (await token.json()).requestToken };
  }
  async function post(client, url, data) {
    return client.request.post(url, { headers: await headers(client), data });
  }
  async function account(client) {
    const email = 'photo-http-' + randomUUID() + '@example.test', password = 'Pc9!' + randomBytes(24).toString('hex');
    assert.equal((await post(client, '/api/auth/register', { email, password })).status(), 200);
    let id;
    await expect.poll(async () => {
      const response = await client.request.get('http://mailpit:8025/api/v1/messages');
      assert.equal(response.status(), 200);
      const items = (await response.json()).messages || [];
      id = items.find(m => m.Subject === 'Confirm your PlantCare account' && m.To?.some(t => t.Address === email))?.ID;
      return Boolean(id);
    }, { timeout: 20000 }).toBe(true);
    const message = await (await client.request.get('http://mailpit:8025/api/v1/message/' + id)).json();
    const href = /href="([^"]+)"/.exec(message.HTML)?.[1]; assert.ok(href);
    const target = new URL(href.replaceAll('&amp;', '&'));
    assert.equal(target.origin, origin); assert.equal(target.pathname, '/api/auth/confirmEmail');
    assert.equal((await client.request.get(target.href)).status(), 200);
    assert.equal((await post(client, '/api/auth/login?useCookies=true', { email, password })).status(), 200);
    return { password };
  }
  step = 'synthetic-accounts';
  const owner = await account(context), outsider = await account(other);
  const catalog = await (await context.request.get('/api/plant-species')).json(); assert.ok(catalog.length);
  const added = await post(context, '/api/my-plants', { plantSpeciesId: catalog[0].id, nickname: 'Photo HTTP test plant' });
  assert.equal(added.status(), 201); const plant = await added.json();
  const base = '/api/my-plants/' + plant.id + '/photos';
  const beforePlant = await (await context.request.get('/api/my-plants/' + plant.id)).json();
  const beforeHistory = await (await context.request.get('/api/my-plants/' + plant.id + '/care/history')).json();
  const page = await context.newPage(); const errors = [];
  page.on('pageerror', e => errors.push(e.name));
  step = 'browser-plant-details'; await page.goto('/my-plants/' + plant.id);
  const journal = page.getByRole('region', { name: 'Photo journal', exact: true });
  await expect(journal.getByText('No growth photos saved yet.', { exact: true })).toBeVisible();
  step = 'browser-select-photo';
  // Observe FormData arguments while delegating unchanged to the browser.
  // DevTools can omit multipart data from network inspection; status lookup
  // below independently verifies this request ID reached the real server.
  await page.evaluate(() => {
    const append = FormData.prototype.append;
    window.__photoHttpContract = {};
    FormData.prototype.append = function (...args) {
      if (args[0] === 'requestId') window.__photoHttpContract.requestId = args[1];
      if (args[0] === 'file') window.__photoHttpContract.filename = args[2];
      return append.apply(this, args);
    };
  });
  const date = new Date(Date.now() - 86400000).toISOString().slice(0, 10);
  await journal.getByLabel('Choose plant photo', { exact: true }).setInputFiles({ name: 'private-camera.png', mimeType: 'image/png', buffer: png });
  await journal.getByLabel('Photo date', { exact: true }).fill(date);
  await journal.getByLabel('Photo notes (optional)', { exact: true }).fill('Generated first leaf');
  const uploadRequest = page.waitForRequest(r => new URL(r.url()).pathname === base && r.method() === 'POST');
  const uploadResponse = page.waitForResponse(r => new URL(r.url()).pathname === base && r.request().method() === 'POST');
  step = 'browser-upload-response';
  const uploadStarted = Date.now();
  await journal.getByRole('button', { name: 'Save photo', exact: true }).click();
  assert.equal((await uploadResponse).status(), 200);
  const uploadMilliseconds = Date.now() - uploadStarted;
  step = 'browser-csrf-contract';
  const request = await uploadRequest;
  const requestHeaders = await request.allHeaders();
  assert.ok(requestHeaders['x-xsrf-token']); assert.match(requestHeaders['content-type'], /multipart\/form-data/);
  step = 'browser-form-contract';
  const contract = await page.evaluate(() => window.__photoHttpContract);
  assert.equal(contract.filename, 'photo');
  const requestId = contract.requestId; assert.match(requestId, /^[a-f0-9-]{36}$/);
  step = 'browser-saved-entry';
  await expect(journal.getByText('Generated first leaf', { exact: true })).toBeVisible();
  await expect(journal.locator('.draft-preview')).toHaveCount(0);
  const entries = await (await context.request.get(base)).json(); assert.equal(entries.length, 1);
  const entry = entries[0]; assert.equal(entry.photographedOn, date);
  assert.equal((await (await context.request.get(base + '/requests/' + requestId)).json()).id, entry.id);
  step = 'private-normalized-image';
  const image = await context.request.get(base + '/' + entry.id + '/image');
  assert.equal(image.status(), 200); assert.match(image.headers()['cache-control'], /no-store/);
  assert.equal(image.headers()['content-type'], 'image/jpeg');
  const jpeg = await image.body(); assert.ok(jpeg.subarray(0, 2).equals(Buffer.from([255, 216]))); assert.ok(!jpeg.equals(png));
  step = 'browser-decoded-thumbnail';
  await expect.poll(() => journal.locator('.photo-grid img').evaluate(e => [e.naturalWidth, e.naturalHeight])).toEqual(thumbSize);
  console.log('PASS: real phone browser upload crosses HTTPS, authenticated API, isolated decoder and private SQL storage.');

  step = 'owner-isolation-and-idempotency';
  const multipart = { requestId, photographedOn: date, notes: 'Generated first leaf', file: { name: 'photo', mimeType: 'image/png', buffer: png } };
  assert.equal((await context.request.post(base, { multipart })).status(), 400);
  const duplicate = await context.request.post(base, { headers: await headers(context), multipart });
  assert.equal(duplicate.status(), 200); assert.equal((await duplicate.json()).id, entry.id);
  for (const path of [base, base + '/' + entry.id + '/image', base + '/' + entry.id + '/thumbnail', base + '/requests/' + requestId]) {
    assert.equal((await other.request.get(path)).status(), 404);
  }
  assert.equal((await other.request.delete(base + '/' + entry.id, { headers: await headers(other) })).status(), 404);
  const invalid = await context.request.post(base, { headers: await headers(context), multipart: {
    requestId: randomUUID(), photographedOn: date, file: { name: 'photo', mimeType: 'image/png', buffer: Buffer.from('not an image') } } });
  assert.equal(invalid.status(), 422); assert.equal((await invalid.json()).detail, 'photo-invalid');
  assert.equal((await (await context.request.get(base)).json()).length, 1);
  assert.deepEqual(await (await context.request.get('/api/my-plants/' + plant.id)).json(), beforePlant);
  assert.deepEqual(await (await context.request.get('/api/my-plants/' + plant.id + '/care/history')).json(), beforeHistory);
  console.log('PASS: CSRF/owner isolation, duplicate reconciliation, sanitized invalid-image rejection and unchanged care.');

  step = 'viewer-edit-reload-export';
  await journal.getByRole('button', { name: 'View photo from ' + date, exact: true }).click();
  const viewer = journal.getByRole('region', { name: 'Opened growth photo', exact: true });
  await expect(viewer).toBeVisible(); await expect(viewer).toBeFocused();
  await expect.poll(() => viewer.locator('img').evaluate(e => e.naturalWidth)).toBe(fullWidth);
  await viewer.getByRole('button', { name: 'Close growth photo', exact: true }).click();
  await journal.getByRole('button', { name: 'Edit photo from ' + date, exact: true }).click();
  await journal.getByLabel('Edit photo notes', { exact: true }).fill('Generated second leaf');
  await journal.getByRole('button', { name: 'Save photo notes', exact: true }).click();
  await expect(journal.getByText('Generated second leaf', { exact: true })).toBeVisible();
  await page.reload(); await expect(journal.getByText('Generated second leaf', { exact: true })).toBeVisible();
  const exported = await post(context, '/api/account/export/photos', { password: owner.password });
  assert.equal(exported.status(), 200); assert.match(exported.headers()['cache-control'], /no-store/);
  const exportedBytes = await exported.body();
  fs.writeFileSync('/output/photos.zip', exportedBytes);
  const thumbnail = await context.request.get(base + '/' + entry.id + '/thumbnail'); assert.equal(thumbnail.status(), 200);
  fs.writeFileSync('/output/photo-sample.json', JSON.stringify({ inputBytes: png.length, sourceWidth: fullWidth, sourceHeight: fullHeight,
    imageBytes: jpeg.length, thumbnailBytes: (await thumbnail.body()).length, zipBytes: exportedBytes.length, uploadMilliseconds }));
  if (process.env.PHOTO_HTTP_LARGE === '1') {
    const signal = (name, value) => {
      fs.writeFileSync('/output/' + name + '.tmp', JSON.stringify(value));
      fs.renameSync('/output/' + name + '.tmp', '/output/' + name + '.json');
    };
    const wait = async name => {
      const deadline = Date.now() + 180000;
      while (!fs.existsSync('/output/' + name + '.json')) {
        if (Date.now() > deadline) throw Error();
        await new Promise(resolve => setTimeout(resolve, 100));
      }
      return JSON.parse(fs.readFileSync('/output/' + name + '.json', 'utf8'));
    };
    const upload = async (buffer, target = base) => {
      const response = await context.request.post(target, { headers: await headers(context), multipart: {
        ...multipart, requestId: randomUUID(), file: { name: 'photo', mimeType: 'image/png', buffer } } });
      responseStatus = response.status();
      return response;
    };
    const timings = {};
    step = 'large-byte-limit';
    const limit = fs.readFileSync('/fixtures/limit.png'); assert.equal(limit.length, 8 * 1024 * 1024);
    let began = Date.now();
    let response = await upload(limit); assert.equal(response.status(), 200);
    timings.byteLimitMilliseconds = Date.now() - began;
    let temporary = await response.json();
    const normalized = await context.request.get(base + '/' + temporary.id + '/image');
    assert.equal(normalized.status(), 200);
    assert.equal(createHash('sha256').update(await normalized.body()).digest('hex'), createHash('sha256').update(jpeg).digest('hex'));
    assert.equal((await context.request.delete(base + '/' + temporary.id, { headers: await headers(context) })).status(), 204);
    step = 'large-pixel-limit';
    began = Date.now(); response = await upload(fs.readFileSync('/fixtures/20mp.png'));
    assert.equal(response.status(), 200); timings.pixelLimitMilliseconds = Date.now() - began;
    temporary = await response.json();
    const resized = await context.request.get(base + '/' + temporary.id + '/image'); assert.equal(resized.status(), 200);
    const resizedBytes = await resized.body(); assert.ok(resizedBytes.length <= 512 * 1024);
    assert.deepEqual(await page.evaluate(async bytes => {
      const bitmap = await createImageBitmap(new Blob([new Uint8Array(bytes)], { type: 'image/jpeg' }));
      const size = [bitmap.width, bitmap.height]; bitmap.close(); return size;
    }, Array.from(resizedBytes)), [1600, 1280]);
    assert.equal((await context.request.delete(base + '/' + temporary.id, { headers: await headers(context) })).status(), 204);
    step = 'large-rejections';
    response = await upload(fs.readFileSync('/fixtures/over-limit.png')); assert.equal(response.status(), 413);
    response = await upload(fs.readFileSync('/fixtures/21mp.png')); assert.equal(response.status(), 422);
    assert.equal((await response.json()).detail, 'photo-dimensions-exceeded');
    response = await context.request.post(base, { headers: { ...await headers(context), 'Content-Type': 'multipart/form-data; boundary=generated' },
      data: Buffer.alloc(9 * 1024 * 1024 + 1, 120) });
    responseStatus = response.status(); assert.equal(response.status(), 413);
    assert.equal((await (await context.request.get(base)).json()).length, 1);
    signal('large-ready', { photoId: entry.id });
    const seeded = await wait('large-seeded');
    const secondBase = '/api/my-plants/' + seeded.plantId + '/photos';
    step = 'large-account-quota';
    const secondCount = (await (await context.request.get(secondBase)).json()).length;
    assert.ok(secondCount < 20);
    response = await upload(png, secondBase); assert.equal(response.status(), 409);
    assert.equal((await (await context.request.get(secondBase)).json()).length, secondCount);
    step = 'large-account-export';
    began = Date.now();
    response = await post(context, '/api/account/export/photos', { password: owner.password });
    responseStatus = response.status(); assert.equal(response.status(), 200);
    assert.match(response.headers()['cache-control'], /no-store/);
    const archive = await response.body(); assert.ok(archive.length > 18 * 1024 * 1024);
    timings.exportMilliseconds = Date.now() - began;
    fs.writeFileSync('/output/large-photos.zip', archive);
    signal('large-done', { ...timings, acceptedFileBytes: limit.length, acceptedPixels: 20000000,
      rejectedFileBytes: 8 * 1024 * 1024 + 1, rejectedBodyBytes: 9 * 1024 * 1024 + 1, rejectedPixels: 21000000,
      accountQuotaRejected: true });
    if (process.env.PHOTO_HTTP_RESTORED_APP === '1') {
      step = 'restored-application';
      await require('./photo-recovered.cjs')({ browser, context, other, password: owner.password,
        plantId: plant.id, photoId: entry.id, count: seeded.entries,
        imageHash: createHash('sha256').update(jpeg).digest('hex'),
        thumbnailHash: createHash('sha256').update(await thumbnail.body()).digest('hex') });
    }
    await wait('large-continue');
    step = 'large-quota-reuse';
    response = await upload(png); assert.equal(response.status(), 200);
    temporary = await response.json();
    assert.equal((await context.request.delete(base + '/' + temporary.id, { headers: await headers(context) })).status(), 204);
    assert.equal((await (await context.request.get(base)).json()).length, 1);
    console.log('PASS: exact input byte/pixel limits, oversized rejection, near-quota private export and subsequent quota reuse.');
  }
  if (process.env.PHOTO_HTTP_MIXED === '1') {
    // Registration, confirmation and login for two accounts already consume
    // six shared authentication permits. Keep production limits unchanged and
    // let that fixed window expire before repeated password-verified exports.
    step = 'mixed-rate-window';
    await new Promise(resolve => setTimeout(resolve, 61000));
    step = 'mixed-workload';
    const signal = (name, value) => {
      fs.writeFileSync('/output/' + name + '.tmp', JSON.stringify(value));
      fs.renameSync('/output/' + name + '.tmp', '/output/' + name + '.json');
    };
    const wait = async name => {
      const deadline = Date.now() + 180000;
      while (!fs.existsSync('/output/' + name + '.json')) {
        if (Date.now() > deadline) throw Error();
        await new Promise(resolve => setTimeout(resolve, 100));
      }
    };
    signal('mixed-ready', { plantId: plant.id });
    await wait('mixed-start');
    const start = Date.now(), operations = [];
    for (let i = 0; i < 4; i++) {
      step = 'mixed-upload';
      let began = Date.now();
      const upload = await context.request.post(base, { headers: await headers(context), multipart: { ...multipart, requestId: randomUUID() } });
      responseStatus = upload.status();
      assert.equal(upload.status(), 200);
      const temporary = await upload.json();
      operations.push([began, Date.now()]);
      step = 'mixed-export';
      began = Date.now();
      const zip = await post(context, '/api/account/export/photos', { password: owner.password });
      responseStatus = zip.status();
      assert.equal(zip.status(), 200); assert.match(zip.headers()['cache-control'], /no-store/);
      const bytes = await zip.body(); assert.equal(bytes.readUInt32LE(0), 0x04034b50);
      operations.push([began, Date.now()]);
      step = 'mixed-delete';
      assert.equal((await context.request.delete(base + '/' + temporary.id, { headers: await headers(context) })).status(), 204);
      await new Promise(resolve => setTimeout(resolve, 5000));
    }
    step = 'mixed-notifications';
    await expect.poll(async () => {
      const notifications = await context.request.get('/api/notifications?take=100');
      assert.equal(notifications.status(), 200);
      return (await notifications.json()).length;
    }, { timeout: 30000 }).toBe(100);
    signal('mixed-done', { window: [start, Date.now()], operations });
    await wait('mixed-continue');
    console.log('PASS: four additional real uploads and private ZIP exports with 100 in-app reminders visible.');
  }
  if (process.env.PHOTO_HTTP_SNAPSHOT === '1') {
    step = 'await-private-snapshot-check';
    fs.writeFileSync('/output/snapshot-ready.tmp', JSON.stringify({ photoId: entry.id, imageSha256: createHash('sha256').update(jpeg).digest('hex') }));
    fs.renameSync('/output/snapshot-ready.tmp', '/output/snapshot-ready.json');
    const deadline = Date.now() + 180000;
    while (!fs.existsSync('/output/snapshot-continue.json')) {
      if (Date.now() > deadline) throw Error();
      await new Promise(resolve => setTimeout(resolve, 200));
    }
  }
  console.log('PASS: private viewer/focus, notes editing, preserved photo date, reload and password-verified ZIP export.');

  step = 'delete-and-account-erasure';
  await journal.getByRole('button', { name: 'Delete photo from ' + date, exact: true }).click();
  await journal.getByRole('button', { name: 'Keep photo', exact: true }).click();
  assert.equal((await (await context.request.get(base)).json()).length, 1);
  await journal.getByRole('button', { name: 'Delete photo from ' + date, exact: true }).click();
  await journal.getByRole('button', { name: 'Confirm photo deletion', exact: true }).click();
  await expect(journal.getByText('No growth photos saved yet.', { exact: true })).toBeVisible();
  assert.equal((await context.request.get(base + '/' + entry.id + '/image')).status(), 404);
  const again = await context.request.post(base, { headers: await headers(context), multipart: { ...multipart, requestId: randomUUID() } });
  assert.equal(again.status(), 200);
  assert.equal((await post(context, '/api/account/delete', { password: owner.password, confirmation: 'DELETE' })).status(), 204);
  assert.equal((await context.request.get('/api/account/me')).status(), 401);
  assert.equal((await post(other, '/api/account/delete', { password: outsider.password, confirmation: 'DELETE' })).status(), 204);
  assert.deepEqual(errors, []);
  await browser.close();
  console.log('PASS: explicit photo deletion and account erasure through real authenticated endpoints.');
})().catch(() => {
  if (process.env.PHOTO_HTTP_MIXED === '1') fs.writeFileSync('/output/mixed-failure.json', JSON.stringify({ step, responseStatus }));
  if (process.env.PHOTO_HTTP_LARGE === '1') fs.writeFileSync('/output/large-failure.json', JSON.stringify({ step, responseStatus }));
  console.log('Photo HTTP browser failure at ' + step + '; sensitive details withheld.'); process.exit(1);
});
