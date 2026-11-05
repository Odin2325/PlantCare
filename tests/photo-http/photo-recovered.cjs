// Generated fixture only. Keep passwords, cookies and key-dependent tokens in memory.
const { expect } = require('/tests/node_modules/@playwright/test');
const assert = require('node:assert/strict');
const { createHash } = require('node:crypto');
const fs = require('node:fs');

module.exports = async function recovered({ browser, context, other, password, plantId, photoId, imageHash, thumbnailHash, count }) {
  const origin = 'https://plantcare-recovered.test:8443';
  let step = 'recovery-wait-empty-control';
  const contexts = [];
  const signal = (name, data) => {
    fs.writeFileSync('/output/' + name + '.tmp', JSON.stringify(data));
    fs.renameSync('/output/' + name + '.tmp', '/output/' + name + '.json');
  };
  async function wait(name) {
    const deadline = Date.now() + 180000;
    while (!fs.existsSync('/output/' + name + '.json')) {
      if (Date.now() > deadline) throw Error();
      await new Promise(resolve => setTimeout(resolve, 100));
    }
  }
  async function client(source) {
    const state = await source.storageState();
    state.cookies = state.cookies.filter(c => c.domain.replace(/^\./, '') === 'plantcare-photo.test').map(c => ({ ...c, domain: 'plantcare-recovered.test' }));
    assert.ok(state.cookies.some(c => c.name === 'PlantCare.Authentication'));
    state.origins = [];
    const value = await browser.newContext({ baseURL: origin, ignoreHTTPSErrors: true, storageState: state, viewport: { width: 390, height: 844 } });
    await value.addInitScript(() => Object.defineProperty(Navigator.prototype, 'onLine', { configurable: true, get: () => true }));
    contexts.push(value); return value;
  }
  async function headers(value) {
    const response = await value.request.get('/api/antiforgery/token'); assert.equal(response.status(), 200);
    return { 'X-XSRF-TOKEN': (await response.json()).requestToken };
  }
  async function post(value, path, data) { return value.request.post(path, { headers: await headers(value), data }); }
  const base = '/api/my-plants/' + plantId + '/photos';
  try {
    await wait('recovery-empty-ready');
    const empty = await client(context);
    assert.equal((await empty.request.get('/api/account/me')).status(), 401);
    signal('recovery-empty-done', { refused: true });
    step = 'recovery-restored-session-and-ui';
    await wait('recovery-app-ready');
    const owner = await client(context), outsider = await client(other);
    const ownerResponse = await owner.request.get('/api/account/me'); assert.equal(ownerResponse.status(), 200);
    const ownerRecord = await ownerResponse.json();
    const sourceRecord = await (await context.request.get('/api/account/me')).json();
    assert.equal(ownerRecord.id, sourceRecord.id);
    const otherResponse = await outsider.request.get('/api/account/me'); assert.equal(otherResponse.status(), 200);
    const outsiderRecord = await otherResponse.json();
    const page = await owner.newPage(); await page.goto('/my-plants/' + plantId);
    const journal = page.getByRole('region', { name: 'Photo journal', exact: true });
    await expect(journal).toBeVisible();
    await expect(journal.locator('.photo-grid img')).toHaveCount(20);
    await expect.poll(() => journal.locator('.photo-grid img').first().evaluate(img => img.naturalWidth)).toBeGreaterThan(0);
    await expect(journal.getByLabel('Choose plant photo', { exact: true })).toHaveCount(0);
    step = 'recovery-private-photo-reads';
    const plants = await (await owner.request.get('/api/my-plants')).json();
    let read = 0;
    for (const plant of plants) {
      const target = '/api/my-plants/' + plant.id + '/photos';
      const response = await owner.request.get(target); assert.equal(response.status(), 200);
      for (const entry of await response.json()) {
        for (const [kind, hash] of [['image', imageHash], ['thumbnail', thumbnailHash]]) {
          const media = await owner.request.get(target + '/' + entry.id + '/' + kind);
          assert.equal(media.status(), 200); assert.match(media.headers()['cache-control'], /no-store/);
          assert.equal(createHash('sha256').update(await media.body()).digest('hex'), hash);
        }
        read++;
      }
    }
    assert.equal(read, count);
    for (const path of [base, base + '/' + photoId + '/image', base + '/' + photoId + '/thumbnail'])
      assert.equal((await outsider.request.get(path)).status(), 404);
    assert.equal((await outsider.request.delete(base + '/' + photoId, { headers: await headers(outsider) })).status(), 404);
    step = 'recovery-fresh-login-export-erasure';
    assert.equal((await post(owner, '/api/auth/logout')).status(), 204);
    assert.equal((await owner.request.get('/api/account/me')).status(), 401);
    assert.equal((await post(owner, '/api/auth/login?useCookies=true', { email: ownerRecord.email, password })).status(), 200);
    const exported = await post(owner, '/api/account/export/photos', { password }); assert.equal(exported.status(), 200);
    assert.match(exported.headers()['cache-control'], /no-store/);
    fs.writeFileSync('/output/recovered-photos.zip', await exported.body());
    assert.equal((await post(owner, '/api/account/delete', { password, confirmation: 'DELETE' })).status(), 204);
    assert.equal((await owner.request.get('/api/account/me')).status(), 401);
    assert.equal((await outsider.request.get('/api/account/me')).status(), 200);
    assert.equal((await outsider.request.get(base)).status(), 404);
    signal('recovery-app-done', { ownerId: ownerRecord.id, outsiderId: outsiderRecord.id });
    step = 'recovery-old-snapshot-after-erasure';
    await wait('recovery-replay-ready');
    const replayOwner = await client(context), replayOther = await client(other);
    assert.equal((await replayOwner.request.get('/api/account/me')).status(), 401);
    assert.equal((await post(replayOwner, '/api/auth/login?useCookies=true', { email: ownerRecord.email, password })).status(), 401);
    assert.equal((await replayOther.request.get('/api/account/me')).status(), 200);
    assert.equal((await replayOther.request.get(base)).status(), 404);
    signal('recovery-replay-done', { passed: true });
    console.log('PASS: restored key/session control, private photo UI/reads/export, account erasure and old-snapshot erasure replay.');
  } catch (error) {
    signal('recovery-app-failure', { step });
    throw error;
  } finally {
    for (const value of contexts) await value.close();
  }
};
