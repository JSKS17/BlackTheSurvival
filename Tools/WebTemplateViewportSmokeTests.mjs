import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import test from 'node:test';
import vm from 'node:vm';

const template = await readFile(new URL('../Assets/WebGLTemplates/BlackTheSurvival/index.html', import.meta.url), 'utf8');
const css = await readFile(new URL('../Assets/WebGLTemplates/BlackTheSurvival/TemplateData/style.css', import.meta.url), 'utf8');
const script = template.match(/<script>\s*([\s\S]*?)<\/script>/)[1]
  .replace(/^#if[^\n]*\n[\s\S]*?^#endif[^\n]*\n/gm, '')
  .replace(/\{\{\{[^}]*\}\}\}/g, 'fixture');

function target() {
  const listeners = new Map();
  return {
    addEventListener(type, handler) {
      if (!listeners.has(type)) listeners.set(type, []);
      listeners.get(type).push(handler);
    },
    dispatch(type, event = {}) {
      for (const handler of listeners.get(type) ?? []) handler(event);
    }
  };
}

function fixture({ visualViewport = true, deferReady = false } = {}) {
  const frames = [], focusCalls = [], fullscreenCalls = [], observations = [];
  const elements = new Map();
  for (const id of ['unity-canvas', 'loading', 'loading-animation', 'loading-message', 'loading-progress',
    'loading-percent', 'retry', 'fullscreen', 'notice', 'game-area', 'game-frame']) {
    elements.set(id, { ...target(), style: {}, setAttribute() {}, querySelector: () => ({ textContent: '' }) });
  }
  const canvas = elements.get('unity-canvas'), area = elements.get('game-area'), frame = elements.get('game-frame');
  canvas.focus = options => focusCalls.push(options);
  area.clientWidth = 1200; area.clientHeight = 720;
  const window = { ...target(), location: { origin: 'https://fixture.invalid' } };
  window.parent = window;
  window.dispatchEvent = event => window.dispatch(event.type, event);
  if (visualViewport) window.visualViewport = target();
  const document = {
    ...target(), hidden: false, fullscreenElement: null,
    getElementById: id => elements.get(id),
    createElement: type => type === 'canvas' ? { getContext: () => ({}) } : {},
    body: { appendChild: element => element.onload() }
  };
  let config, finishLoading, attached;
  const ready = new Promise(resolve => { attached = resolve; });
  const instance = { SetFullscreen: value => fullscreenCalls.push(value) };
  const loading = new Promise(resolve => { finishLoading = () => resolve(instance); });
  window.BTSAccountBridge = { prepare: () => Promise.resolve(), attachUnity: attached };
  vm.runInNewContext(script, {
    window, document, location: { protocol: 'https:', reload() {} }, console,
    ResizeObserver: class {
      constructor(handler) { this.handler = handler; }
      observe(element) { observations.push({ handler: this.handler, element }); }
    },
    requestAnimationFrame: handler => frames.push(handler),
    CustomEvent: class { constructor(type, options) { this.type = type; this.detail = options.detail; } },
    createUnityInstance: (_canvas, value) => { config = value; return loading; }
  });
  if (!deferReady) finishLoading();
  return {
    window, document, canvas, area, frame, elements, focusCalls, fullscreenCalls, observations,
    ready, finishLoading, get config() { return config; }, get pendingFrames() { return frames.length; },
    flush() { while (frames.length) frames.shift()(0); }
  };
}

test('Unity automatically matches the CSS canvas at low DPI', async () => {
  const f = fixture(); await f.ready;
  assert.equal(f.config.matchWebGLToCanvasSize, true);
  assert.equal(f.config.devicePixelRatio, 1);
  assert.equal(f.window.blackTheSurvival.status, 'ready');
  assert.equal(f.frame.style.width, '1200px');
  assert.equal(f.frame.style.height, '675px');
  assert.equal(f.observations[0].element, f.area);
});

test('fullscreen, window and visual viewport changes coalesce and restore focus without scrolling', async () => {
  const f = fixture(); await f.ready;
  const before = f.focusCalls.length;
  f.area.clientWidth = 1000; f.area.clientHeight = 500;
  f.canvas.style.width = '100vw'; f.canvas.style.height = '100vh';
  f.document.fullscreenElement = f.canvas;
  f.window.dispatch('resize'); f.document.dispatch('fullscreenchange'); f.window.visualViewport.dispatch('resize');
  assert.equal(f.pendingFrames, 1);
  f.flush();
  assert.equal(f.frame.style.width, '888px');
  assert.equal(f.frame.style.height, '499px');
  assert.equal(f.focusCalls.length, before + 1);
  assert.equal(f.focusCalls.at(-1).preventScroll, true);
  assert.equal(f.canvas.style.width, '100vw');
  assert.equal(f.canvas.style.height, '100vh');
  f.document.fullscreenElement = null;
  f.area.clientWidth = 390; f.area.clientHeight = 700;
  f.document.dispatch('fullscreenchange'); f.flush();
  assert.equal(f.frame.style.width, '390px');
  assert.equal(f.frame.style.height, '219px');
});

test('viewport changes never steal focus during loading or when hidden', async () => {
  const f = fixture({ deferReady: true });
  f.window.dispatch('resize'); f.flush();
  assert.equal(f.focusCalls.length, 0);
  f.finishLoading(); await f.ready;
  const before = f.focusCalls.length;
  f.document.hidden = true;
  f.window.visualViewport.dispatch('resize'); f.flush();
  assert.equal(f.focusCalls.length, before);
});

test('existing pointer and Unity fullscreen input remain native', async () => {
  const f = fixture(); await f.ready;
  let prevented = false;
  f.canvas.dispatch('pointerdown', { preventDefault: () => { prevented = true; } });
  assert.equal(prevented, false);
  assert.equal(f.focusCalls.at(-1).preventScroll, true);
  f.elements.get('fullscreen').dispatch('click');
  assert.deepEqual(f.fullscreenCalls, [1]);
  assert.equal(f.focusCalls.at(-1).preventScroll, true);
});

test('visual viewport API is optional and canvas long press cannot select text', async () => {
  const f = fixture({ visualViewport: false }); await f.ready;
  f.window.dispatch('resize'); f.flush();
  const canvasRule = css.match(/#unity-canvas\s*\{([^}]+)\}/)[1];
  assert.match(canvasRule, /touch-action:\s*none/);
  assert.match(canvasRule, /user-select:\s*none/);
  assert.match(canvasRule, /-webkit-touch-callout:\s*none/);
  assert.doesNotMatch(css, /:fullscreen/);
});
