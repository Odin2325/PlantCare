// Real HTTP format/count checks; generated fixtures and in-memory test identity only.
const assert = require('node:assert/strict');
const { randomUUID, createHash } = require('node:crypto');
const fs = require('node:fs');

module.exports = async function formats({ context, page, password, base, originalId, date }) {
  let step = 'format-jpeg', status;
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
    return JSON.parse(fs.readFileSync('/output/' + name + '.json', 'utf8'));
  }
  async function headers() {
    const response = await context.request.get('/api/antiforgery/token'); assert.equal(response.status(), 200);
    return { 'X-XSRF-TOKEN': (await response.json()).requestToken };
  }
  const read = name => fs.readFileSync('/fixtures/' + name);
  async function upload(buffer, mime, target = base) {
    const response = await context.request.post(target, { headers: await headers(), multipart: {
      requestId: randomUUID(), photographedOn: date, notes: 'Generated format sample',
      file: { name: 'photo', mimeType: mime, buffer } } });
    status = response.status(); return response;
  }
  async function remove(id) { assert.equal((await context.request.delete(base + '/' + id, { headers: await headers() })).status(), 204); }
  async function list(target = base) { const response = await context.request.get(target); assert.equal(response.status(), 200); return response.json(); }
  function cleanJpeg(bytes) {
    assert.equal(bytes.readUInt16BE(0), 0xffd8); assert.equal(bytes.readUInt16BE(bytes.length - 2), 0xffd9);
    for (let pos = 2; pos < bytes.length;) {
      assert.equal(bytes[pos++], 255);
      while (bytes[pos] === 255) pos++;
      const marker = bytes[pos++]; if (marker === 0xda) return;
      assert.ok(!(marker >= 0xe1 && marker <= 0xef) && marker !== 0xfe); // No source profiles/COM.
      const length = bytes.readUInt16BE(pos); assert.ok(length >= 2 && pos + length <= bytes.length);
      pos += length;
    }
    assert.fail('Missing JPEG scan');
  }
  async function normalized(entry) {
    const output = {};
    for (const kind of ['image', 'thumbnail']) {
      const response = await context.request.get(base + '/' + entry.id + '/' + kind);
      assert.equal(response.status(), 200); assert.equal(response.headers()['content-type'], 'image/jpeg');
      assert.match(response.headers()['cache-control'], /no-store/);
      output[kind] = await response.body(); cleanJpeg(output[kind]);
    }
    return output;
  }
  async function pixels(bytes) {
    return page.evaluate(async data => {
      const bitmap = await createImageBitmap(new Blob([new Uint8Array(data)], { type: 'image/jpeg' }));
      const canvas = document.createElement('canvas'); canvas.width = bitmap.width; canvas.height = bitmap.height;
      const context = canvas.getContext('2d'); context.drawImage(bitmap, 0, 0); bitmap.close();
      return { size: [canvas.width, canvas.height],
        colors: [[.25, .25], [.75, .25], [.25, .75], [.75, .75]].map(([x, y]) =>
          [...context.getImageData(Math.floor(canvas.width * x), Math.floor(canvas.height * y), 1, 1).data]) };
    }, Array.from(bytes));
  }
  try {
    const jpeg = read('oriented.jpg'); assert.ok(jpeg.includes(Buffer.from('Exif\0\0')));
    let response = await upload(jpeg, 'image/jpeg'); assert.equal(response.status(), 200);
    let entry = await response.json(), media = await normalized(entry);
    const oriented = await pixels(media.image); assert.deepEqual(oriented.size, [30, 60]);
    const expected = [[0, 0, 255], [255, 0, 0], [255, 255, 0], [0, 128, 0]];
    oriented.colors.forEach((color, i) => expected[i].forEach((value, channel) => assert.ok(Math.abs(color[channel] - value) < 35)));
    await remove(entry.id);
    step = 'format-alpha-and-static-webp';
    let whiteId;
    for (const [name, mime] of [['transparent.png', 'image/png'], ['static.webp', 'image/webp']]) {
      response = await upload(read(name), mime); assert.equal(response.status(), 200);
      entry = await response.json(); media = await normalized(entry);
      const white = await pixels(media.image); assert.deepEqual(white.size, [40, 20]);
      white.colors.forEach(color => { assert.ok(color.slice(0, 3).every(value => value >= 245)); assert.equal(color[3], 255); });
      if (mime === 'image/webp') whiteId = entry.id; else await remove(entry.id);
    }
    step = 'format-policy-rejections';
    const probes = [
      [Buffer.from('generated HEIC MIME probe'), 'image/heic', 415],
      [Buffer.from('GIF89a'), 'image/gif', 415],
      [jpeg, 'image/png', 422, 'photo-invalid'],
      [jpeg.subarray(0, jpeg.length - 10), 'image/jpeg', 422, 'photo-invalid'],
      [read('animation-header.png'), 'image/png', 422, 'photo-animation-unsupported'],
      [read('animation-header.webp'), 'image/webp', 422, 'photo-animation-unsupported']];
    for (const [buffer, mime, expectedStatus, code] of probes) {
      response = await upload(buffer, mime); assert.equal(response.status(), expectedStatus);
      if (code) assert.equal((await response.json()).detail, code);
    }
    assert.equal((await list()).length, 2);
    signal('formats-ready', { whiteId, originalId });
    step = 'count-per-plant'; await wait('formats-plant-ready');
    assert.equal((await list()).length, 20);
    response = await upload(read('static.webp'), 'image/webp'); assert.equal(response.status(), 409);
    const reusable = (await list()).find(item => item.id !== whiteId && item.id !== originalId); assert.ok(reusable);
    await remove(reusable.id); assert.equal((await list()).length, 19);
    response = await upload(read('static.webp'), 'image/webp'); assert.equal(response.status(), 200);
    assert.equal((await list()).length, 20);
    response = await upload(read('static.webp'), 'image/webp'); assert.equal(response.status(), 409);
    signal('formats-plant-done', {});
    step = 'count-account'; const seeded = await wait('formats-account-ready');
    const third = '/api/my-plants/' + seeded.thirdPlantId + '/photos';
    assert.equal((await list(third)).length, 10);
    response = await upload(read('static.webp'), 'image/webp', third); assert.equal(response.status(), 409);
    assert.equal((await list(third)).length, 10);
    step = 'count-export';
    response = await context.request.post('/api/account/export/photos', { headers: await headers(), data: { password } });
    status = response.status(); assert.equal(status, 200); assert.match(response.headers()['cache-control'], /no-store/);
    fs.writeFileSync('/output/count-photos.zip', await response.body());
    signal('formats-done', { supported: ['image/jpeg', 'image/png', 'image/webp'], orientationApplied: true,
      transparentPixelsFlattenedWhite: true, sourceProfilesAndCommentsRemoved: true,
      unsupportedMimeRefused: ['image/heic', 'image/gif'], corruptAndAnimationHeadersRefused: true,
      perPlantTwentyFirstRefused: true, deleteThenTwentiethAccepted: true, accountFiftyFirstRefused: true });
    await wait('formats-continue');
    step = 'count-quota-reuse';
    response = await upload(read('static.webp'), 'image/webp'); assert.equal(response.status(), 200);
    entry = await response.json(); await remove(entry.id); assert.equal((await list()).length, 1);
    console.log('PASS: real format orientation/alpha/metadata handling, bounded rejections, 20/50 entry limits and quota reuse.');
  } catch (error) { signal('formats-failure', { step, status }); throw error; }
};
