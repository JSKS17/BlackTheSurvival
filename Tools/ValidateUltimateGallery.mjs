/** Read-only browser verification of the standalone animation catalogue.
 * Run: node Tools/ValidateUltimateGallery.mjs
 * --allow-incomplete is a preparation smoke check, never a final pass.
 * --evidence-dir Temp/example writes reports/screenshots to another folder.
 */
import assert from 'node:assert/strict';
import fs from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath, pathToFileURL } from 'node:url';
import { createHash } from 'node:crypto';
import { createRequire } from 'node:module';

const require = createRequire(import.meta.url);
const { chromium } = require('C:/Users/azxar/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const gallery = path.join(root, 'ArtSource/AnimationPrototypes/index.html');
const evidenceArg = process.argv.indexOf('--evidence-dir');
assert(evidenceArg === -1 || (process.argv[evidenceArg + 1] && !process.argv[evidenceArg + 1].startsWith('--')), '--evidence-dir requires a directory path');
const evidence = evidenceArg === -1 ? path.join(root, 'docs/validation-ultimate-collection') : path.resolve(root, process.argv[evidenceArg + 1]);
const allowIncomplete = process.argv.includes('--allow-incomplete');
const html = await fs.readFile(gallery, 'utf8');
const htmlSha256 = createHash('sha256').update(html).digest('hex');
const catalogMatch = html.match(/<script id="catalog" type="application\/json">([\s\S]*?)<\/script>/);
assert(catalogMatch, 'Embedded catalogue exists');
const catalog = JSON.parse(catalogMatch[1]);
assert.equal(catalog.length, 91, 'Catalogue includes 91 subjects');
assert.equal(new Set(catalog.map(item => item.id)).size, 91, 'Subject IDs are unique');
const ready = catalog.filter(item => item.ready);
assert(allowIncomplete || ready.length === 91, `Only ${ready.length}/91 subjects are ready; run again after assembly`);
await fs.mkdir(evidence, { recursive: true });
const errors = [], checks = [], subjects = [], screenshots = [];
let browser;
const report = { passed: false, preparation_only: allowIncomplete, html_sha256: htmlSha256, subjects_expected: 91,
  subjects_ready: ready.length, subjects_checked: 0, frames_checked: 0, checks, subjects, screenshots,
  screenshot_provenance: [], browser_errors: errors };
const record = (name, details = {}) => checks.push({ name, passed: true, ...details });

try {
  browser = await chromium.launch({ executablePath: 'C:/Program Files/Google/Chrome/Application/chrome.exe', headless: true });
  report.browser_version = browser.version();
  const context = await browser.newContext({ viewport: { width: 1440, height: 1100 }, deviceScaleFactor: 1, reducedMotion: 'reduce' });
  const page = await context.newPage();
  page.on('pageerror', error => errors.push({ type: 'pageerror', message: error.message }));
  page.on('console', message => { if (message.type() === 'error') errors.push({ type: 'console', message: message.text() }); });
  page.on('requestfailed', request => errors.push({ type: 'requestfailed', url: request.url(), message: request.failure()?.errorText }));
  // Queue RAF callbacks without advancing them. Playback is then deterministic and
  // frame stepping still uses the catalogue's own public controls and functions.
  await page.addInitScript(() => {
    window.__galleryRaf = [];
    window.requestAnimationFrame = callback => { window.__galleryRaf.push(callback); return window.__galleryRaf.length; };
  });
  await page.goto(pathToFileURL(gallery).href, { waitUntil: 'load' });
  await page.waitForSelector('#list .card');
  assert.equal(await page.locator('#list .card').count(), 91);
  assert.equal(await page.locator('#list .card:not(:disabled)').count(), ready.length);
  assert((await page.locator('#count').innerText()).includes(`${ready.length} / 91`));
  record('catalogue count and usable subject buttons');

  async function chooseSubject(item) {
    await page.locator('#search').fill('');
    await page.locator(`#list .card[data-id="${item.id}"]`).click();
    assert.equal(await page.locator('#title').innerText(), `${item.name} R · ${item.ultimate}`);
    assert.equal(await page.locator('#list .card.active').getAttribute('data-id'), item.id);
    assert.equal(new URL(page.url()).hash, `#${item.id}`);
    for (const key of ['gif', 'webp', 'atlas']) {
      const link = await page.locator(`#${key}`).getAttribute('href');
      assert.equal(link.split('?')[0], item[key], `${item.id}: ${key} link`);
      assert((await fs.stat(path.resolve(path.dirname(gallery), link.split('?')[0]))).isFile(), `${item.id}: ${key} file exists`);
    }
    const loaded = await page.evaluate(async item => {
      const img = new Image();
      img.src = new URL(item.atlas, location.href).href;
      await img.decode();
      return { width: img.naturalWidth, height: img.naturalHeight, background: getComputedStyle(document.querySelector('#sprite')).backgroundImage };
    }, item);
    assert.deepEqual([loaded.width, loaded.height], item.atlas_size, `${item.id}: actual background atlas dimensions`);
    assert(loaded.background.includes(item.atlas.replaceAll(' ', '%20')), `${item.id}: background atlas is selected`);
    const referenceHidden = await page.locator('#reference-block').getAttribute('hidden');
    assert.equal(referenceHidden === null, Boolean(item.original), `${item.id}: original reference visibility`);
    if (item.original) {
      const reference = await page.evaluate(async () => {
        const img = document.querySelector('#original-reference');
        await img.decode();
        return { width: img.naturalWidth, height: img.naturalHeight, src: img.getAttribute('src') };
      });
      assert.deepEqual([reference.width, reference.height], [128, 128], `${item.id}: original reference remains 128px`);
      assert.equal(reference.src.split('?')[0], item.original, `${item.id}: correct original reference`);
    }
    const officialHidden = await page.locator('#official-reference-block').getAttribute('hidden');
    assert.equal(officialHidden === null, Boolean(item.official), `${item.id}: official illustration visibility`);
    if (item.official) {
      const official = await page.evaluate(async () => {
        const img = document.querySelector('#official-reference');
        await img.decode();
        return { width: img.naturalWidth, height: img.naturalHeight, src: img.getAttribute('src') };
      });
      assert(official.width > 0 && official.height > 0, `${item.id}: official illustration loads`);
      assert.equal(official.src.split('?')[0], item.official, `${item.id}: correct official illustration`);
    }
    const styleHidden = await page.locator('#style-reference-block').getAttribute('hidden');
    assert.equal(styleHidden === null, Boolean(item.style_ref), `${item.id}: Nia style reference visibility`);
    if (item.style_ref) {
      const styleReference = await page.evaluate(async () => {
        const img = document.querySelector('#style-reference');
        await img.decode();
        return { width: img.naturalWidth, height: img.naturalHeight, src: img.getAttribute('src') };
      });
      assert.deepEqual([styleReference.width, styleReference.height], [362, 362], `${item.id}: approved Nia reference dimensions`);
      assert.equal(styleReference.src.split('?')[0], item.style_ref, `${item.id}: approved Nia style selected`);
    }
    return loaded;
  }

  async function inspectFrame(item, expected) {
    const state = await page.evaluate(() => {
      const stage = document.querySelector('#stage'), sprite = document.querySelector('#sprite');
      const a = stage.getBoundingClientRect(), b = sprite.getBoundingClientRect();
      const css = getComputedStyle(sprite);
      return { frame, playing, status: document.querySelector('#status').textContent,
        stage: { x: a.x, y: a.y, width: a.width, height: a.height },
        sprite: { x: b.x, y: b.y, width: b.width, height: b.height },
        imageRendering: css.imageRendering, backgroundPosition: css.backgroundPosition, backgroundSize: css.backgroundSize };
    });
    assert.equal(state.frame, expected, `${item.id}: selected frame`);
    assert(state.status.startsWith(`${expected + 1} / 12 · `), `${item.id}: frame indicator`);
    assert(state.status.includes(item.labels[expected]), `${item.id}: per-frame motion label`);
    assert.equal(state.imageRendering, 'pixelated');
    for (const value of Object.values(state.sprite)) assert(Number.isFinite(value), `${item.id}: finite rendered coordinates`);
    assert(!`${state.backgroundPosition} ${state.backgroundSize}`.includes('NaN'), `${item.id}: valid atlas crop CSS`);
    assert(state.sprite.width > 0 && state.sprite.height > 0, `${item.id}: visible sprite region`);
    const epsilon = 3;
    assert(state.sprite.x >= state.stage.x - epsilon && state.sprite.y >= state.stage.y - epsilon, `${item.id}: frame begins inside stage`);
    assert(state.sprite.x + state.sprite.width <= state.stage.x + state.stage.width + epsilon, `${item.id}: right edge inside stage`);
    assert(state.sprite.y + state.sprite.height <= state.stage.y + state.stage.height + epsilon, `${item.id}: lower edge inside stage`);
    report.frames_checked++;
    return state;
  }

  for (const item of ready) {
    const loaded = await chooseSubject(item);
    await page.evaluate(() => { playing = false; document.querySelector('#play').textContent = '재생'; });
    for (let index = 0; index < 12; index++) {
      await inspectFrame(item, index);
      if (index < 11) await page.evaluate(() => step(1));
    }
    await page.locator('#next').click();
    assert.equal(await page.evaluate(() => frame), 0, `${item.id}: next wraps 12 to 1`);
    await page.locator('#prev').click();
    assert.equal(await page.evaluate(() => frame), 11, `${item.id}: previous wraps 1 to 12`);
    subjects.push({ id: item.id, name: item.name, frames_checked: 12, atlas_loaded: true, atlas_dimensions: [loaded.width, loaded.height], bounds_passed: true });
    report.subjects_checked++;
  }
  record('all ready subjects select correctly and render all 12 frames within stage', { subjects: report.subjects_checked, frames: report.frames_checked });

  const searchExamples = [
    { query: '니아', expected: 'nia', kind: 'Korean character name' },
    { query: 'CrAvEr', expected: 'craver', kind: 'case-insensitive English ID' },
    { query: '죽음의 무도', expected: 'elena', kind: 'ultimate name' },
    { query: '  취호격파산  ', expected: 'dailin', kind: 'trimmed ultimate search' },
  ];
  for (const item of searchExamples) {
    await page.locator('#search').fill(item.query);
    const visible = await page.locator('#list .card:not([hidden])').evaluateAll(cards => cards.map(card => card.dataset.id));
    assert(visible.includes(item.expected), `${item.kind}: expected subject`);
    assert.equal(await page.locator('#empty').isVisible(), false);
    record(`search: ${item.kind}`, { query: item.query, matches: visible });
  }
  await page.locator('#search').fill('__no_matching_subject_84227__');
  assert.equal(await page.locator('#list .card:not([hidden])').count(), 0);
  assert.equal(await page.locator('#empty').isVisible(), true);
  await page.locator('#search').fill('');
  assert.equal(await page.locator('#list .card:not([hidden])').count(), 91);
  record('empty search feedback and reset');

  const nia = ready.find(item => item.id === 'nia');
  await chooseSubject(nia);
  // Exercise the real play button and speed select. Advance the application's
  // own scheduled RAF once by an exact interval for stable timing assertions.
  await page.evaluate(() => { playing = true; document.querySelector('#play').textContent = '일시 정지'; });
  await page.locator('#play').click();
  assert.equal(await page.evaluate(() => playing), false);
  assert.equal(await page.locator('#play').innerText(), '재생');
  await page.locator('#play').click();
  assert.equal(await page.evaluate(() => playing), true);
  assert.equal(await page.locator('#play').innerText(), '일시 정지');
  for (const [value, expected] of [['0.5', 2], ['1', 4], ['1.5', 6]]) {
    await page.locator('#speed').selectOption(value);
    assert.equal(await page.evaluate(() => speed), Number(value));
    const result = await page.evaluate(() => {
      frame = 0; elapsed = 0; playing = true; last = performance.now();
      const callback = window.__galleryRaf.shift();
      if (!callback) throw new Error('Animation callback was not scheduled');
      callback(last + 500);
      return frame;
    });
    assert.equal(result, expected, `${value}x speed advances correct frames in 500ms`);
  }
  await page.locator('#speed').selectOption('1');
  await page.locator('#play').click();
  const beforePause = await page.evaluate(() => frame);
  await page.evaluate(() => { const callback = window.__galleryRaf.shift(); callback(last + 500); });
  assert.equal(await page.evaluate(() => frame), beforePause, 'Paused playback stays on same frame');
  record('play, pause, and 0.5x / 1x / 1.5x playback controls');

  await chooseSubject(nia);
  await page.locator('#full-sheet > summary').click();
  await page.locator('#sheet').scrollIntoViewIfNeeded();
  await page.waitForFunction(() => document.querySelector('#sheet').complete && document.querySelector('#sheet').naturalWidth > 0);
  assert.equal(await page.locator('#full-sheet').getAttribute('open'), '');
  await page.locator('#full-sheet > summary').click();
  record('expandable 12-frame original atlas loads');

  async function capture(id, filename, frameIndex = 0) {
    const item = ready.find(entry => entry.id === id);
    if (!item) return;
    await chooseSubject(item);
    await page.evaluate(index => { frame = index; playing = false; render(); }, frameIndex);
    await page.evaluate(() => window.scrollTo(0, 0));
    const destination = path.join(evidence, filename);
    await page.screenshot({ path: destination, fullPage: true });
    screenshots.push(filename);
    const bytes = await fs.readFile(destination);
    report.screenshot_provenance.push({ file: filename, source: 'Playwright page.screenshot; original PNG bytes, no image editing',
      subject: id, frame: frameIndex + 1, viewport: page.viewportSize(), full_page: true,
      width: bytes.readUInt32BE(16), height: bytes.readUInt32BE(20), sha256: createHash('sha256').update(bytes).digest('hex') });
  }
  await capture('nia', 'gallery-desktop-nia.png', 8);
  await capture('debi_marlene', 'gallery-desktop-twins.png', 6);
  await capture('chiara', 'gallery-desktop-chiara.png', 7);
  await capture('craver', 'gallery-desktop-craver.png', 6);

  await page.setViewportSize({ width: 375, height: 812 });
  const mobile = [];
  for (const id of ['nia', 'chloe', 'irem', 'craver']) {
    const item = ready.find(entry => entry.id === id);
    if (!item) continue;
    await chooseSubject(item);
    await page.evaluate(() => { playing = false; });
    for (let index = 0; index < 12; index++) {
      await inspectFrame(item, index);
      if (index < 11) await page.evaluate(() => step(1));
    }
    const width = await page.evaluate(() => ({ viewport: innerWidth, document: document.documentElement.scrollWidth, body: document.body.scrollWidth }));
    assert(width.document <= width.viewport + 1 && width.body <= width.viewport + 1, `${id}: 375px mobile has no horizontal document overflow`);
    mobile.push({ id, ...width });
  }
  record('375px mobile layout and representative 12-frame bounds', { mobile });
  await capture('nia', 'gallery-mobile-nia.png', 8);
  await capture('chloe', 'gallery-mobile-chloe.png', 6);
  await capture('irem', 'gallery-mobile-irem.png', 7);

  await page.goto(pathToFileURL(gallery).href + '#craver', { waitUntil: 'load' });
  // A hash-only navigation reuses the current document. Reload to verify the
  // direct-link startup path, exactly as opening this link in a fresh tab does.
  await page.reload({ waitUntil: 'load' });
  if (ready.some(item => item.id === 'craver')) {
    assert((await page.locator('#title').innerText()).startsWith('크레이버 R'));
    record('direct subject hash opens matching animation');
  }
  assert.equal(errors.length, 0, `Browser errors: ${JSON.stringify(errors)}`);
  record('no JavaScript, console, or failed-resource errors');
  report.passed = !allowIncomplete;
  report.completed_at = new Date().toISOString();
  console.log(`${allowIncomplete ? 'PREPARATION' : 'PASS'}: ${report.subjects_checked} subjects, ${report.frames_checked} frame views, ${checks.length} checks, ${screenshots.length} screenshots`);
} catch (error) {
  report.failure = error.stack || String(error);
  console.error(report.failure);
  process.exitCode = 1;
} finally {
  await browser?.close();
  await fs.writeFile(path.join(evidence, allowIncomplete ? 'gallery-preparation.json' : 'gallery-browser-validation.json'), JSON.stringify(report, null, 2) + '\n', 'utf8');
}
