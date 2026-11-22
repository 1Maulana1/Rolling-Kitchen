// Mini rasterizer that records Pixelwright draw_ops AND renders a local preview PNG.
const fs = require("fs"), zlib = require("zlib");
const PAL = { outline: "#1d1922" };
const ramp = (n, a) => a.forEach((h, i) => (PAL[`${n}.${i}`] = h));
ramp("conc", ["#2e2c33", "#4d4a52", "#7a766f", "#a9a398", "#cfc8b8"]);
ramp("water", ["#2a4a63", "#3d6f8f", "#7fb0c9"]);
ramp("wood", ["#2b1a12", "#4a2d1c", "#7a4a2a", "#a8703f", "#cf9a5c"]);
ramp("skin", ["#5a3624", "#8a5a3a", "#b9805a", "#dba57e"]);
ramp("red", ["#6a1f2c", "#b13a3a", "#e0654f"]);
ramp("blue", ["#1f3f6b", "#2f6fa8", "#62a3d6"]);
ramp("green", ["#1f4d35", "#3a8049", "#78b857"]);
ramp("orange", ["#a85a1a", "#e08a2a", "#f6c24e"]);
ramp("cream", ["#b89f7c", "#ddc9a0", "#f4ead2"]);
ramp("purple", ["#4a2a5a", "#7a4a8a", "#a87ab8"]);
ramp("teal", ["#1e5a5c", "#2f8a86", "#6cc0b4"]);
ramp("slab", ["#8f8a82", "#9d978d", "#aba599", "#b9b3a6"]);
ramp("steel", ["#5a6379", "#9aa3b6", "#d4dae6"]);
ramp("meat", ["#8a2a3a", "#c25064", "#e8909c"]);
PAL.hair = "#1d1418";

const W = 960, H = 400;
const hex = (h) => [parseInt(h.slice(1, 3), 16), parseInt(h.slice(3, 5), 16), parseInt(h.slice(5, 7), 16)];

class Layer {
  constructor(name, blend = "normal", opacity = 100) {
    this.name = name; this.blend = blend; this.opacity = opacity;
    this.px = new Array(W * H).fill(null); this.ops = [];
  }
}
const layers = [];
let cur = null;
const layer = (name, blend, opacity) => { cur = new Layer(name, blend, opacity); layers.push(cur); return cur; };
const use = (name) => { cur = layers.find((l) => l.name === name); return cur; };
const set = (x, y, c) => { if (x < 0 || y < 0 || x >= W || y >= H) return; cur.px[y * W + x] = c === "transparent" ? null : c; };
const rec = (o) => cur.ops.push(o);

const R = (x1, y1, x2, y2, c, f = true) => {
  x1 = Math.round(x1); y1 = Math.round(y1); x2 = Math.round(x2); y2 = Math.round(y2);
  rec({ op: "rect", from: [x1, y1], to: [x2, y2], color: c, filled: f });
  for (let y = y1; y <= y2; y++) for (let x = x1; x <= x2; x++) if (f || x === x1 || x === x2 || y === y1 || y === y2) set(x, y, c);
};
const L = (x1, y1, x2, y2, c) => {
  x1 = Math.round(x1); y1 = Math.round(y1); x2 = Math.round(x2); y2 = Math.round(y2);
  rec({ op: "line", from: [x1, y1], to: [x2, y2], color: c });
  let dx = Math.abs(x2 - x1), dy = -Math.abs(y2 - y1), sx = x1 < x2 ? 1 : -1, sy = y1 < y2 ? 1 : -1, err = dx + dy;
  for (;;) { set(x1, y1, c); if (x1 === x2 && y1 === y2) break; const e2 = 2 * err; if (e2 >= dy) { err += dy; x1 += sx; } if (e2 <= dx) { err += dx; y1 += sy; } }
};
const ellFill = (x1, y1, x2, y2, f) => {
  const cx = (x1 + x2) / 2, cy = (y1 + y2) / 2, rx = (x2 - x1 + 1) / 2, ry = (y2 - y1 + 1) / 2;
  for (let y = y1; y <= y2; y++) for (let x = x1; x <= x2; x++) {
    const inn = ((x - cx) / rx) ** 2 + ((y - cy) / ry) ** 2 <= 1;
    if (inn) f(x, y, (((x - cx) / (rx - 1.2)) ** 2 + ((y - cy) / (ry - 1.2)) ** 2) <= 1);
  }
};
const E = (x1, y1, x2, y2, c, f = true) => {
  x1 = Math.round(x1); y1 = Math.round(y1); x2 = Math.round(x2); y2 = Math.round(y2);
  rec({ op: "ellipse", from: [x1, y1], to: [x2, y2], color: c, filled: f });
  ellFill(x1, y1, x2, y2, (x, y, inner) => { if (f || !inner) set(x, y, c); });
};
const P = (pts, c, f = true) => {
  pts = pts.map((p) => [Math.round(p[0]), Math.round(p[1])]);
  rec({ op: "polygon", points: pts, color: c, filled: f });
  if (!f) { for (let i = 0; i < pts.length; i++) { const a = pts[i], b = pts[(i + 1) % pts.length]; lineNoRec(a[0], a[1], b[0], b[1], c); } return; }
  const ys = pts.map((p) => p[1]); const y0 = Math.min(...ys), y1 = Math.max(...ys);
  for (let y = y0; y <= y1; y++) {
    const xs = [];
    for (let i = 0; i < pts.length; i++) {
      const a = pts[i], b = pts[(i + 1) % pts.length];
      if ((a[1] <= y && b[1] > y) || (b[1] <= y && a[1] > y)) xs.push(a[0] + ((y - a[1]) / (b[1] - a[1])) * (b[0] - a[0]));
    }
    xs.sort((a, b) => a - b);
    for (let i = 0; i + 1 < xs.length; i += 2) for (let x = Math.round(xs[i]); x <= Math.round(xs[i + 1]); x++) set(x, y, c);
  }
};
const lineNoRec = (x1, y1, x2, y2, c) => {
  let dx = Math.abs(x2 - x1), dy = -Math.abs(y2 - y1), sx = x1 < x2 ? 1 : -1, sy = y1 < y2 ? 1 : -1, err = dx + dy;
  for (;;) { set(x1, y1, c); if (x1 === x2 && y1 === y2) break; const e2 = 2 * err; if (e2 >= dy) { err += dy; x1 += sx; } if (e2 <= dx) { err += dx; y1 += sy; } }
};
// points grouped (one op per call)
const PT = (pts, c) => {
  pts = pts.map((p) => [Math.round(p[0]), Math.round(p[1])]).filter((p) => p[0] >= 0 && p[1] >= 0 && p[0] < W && p[1] < H);
  if (!pts.length) return; rec({ op: "points", points: pts, color: c }); pts.forEach((p) => set(p[0], p[1], c));
};
// deterministic rng
let sd = 12345;
const seed = (s) => (sd = s);
const rnd = () => (sd = (sd * 1664525 + 1013904223) % 4294967296) / 4294967296;
const ri = (a, b) => a + Math.floor(rnd() * (b - a + 1));
const pick = (a) => a[ri(0, a.length - 1)];

// 3x5 font -> points
const FONT = {
  A: "010101111101101", B: "110101110101110", C: "011100100100011", D: "110101101101110", E: "111100110100111", F: "111100110100100",
  G: "011100101101011", H: "101101111101101", I: "111010010010111", J: "001001001101010", K: "101101110101101", L: "100100100100111",
  M: "101111111101101", N: "110101101101101", O: "010101101101010", P: "110101110100100", Q: "010101101111011", R: "110101110101101",
  S: "011100010001110", T: "111010010010010", U: "101101101101111", V: "101101101101010", W: "101101111111101", X: "101101010101101",
  Y: "101101010010010", Z: "111001010100111", 0: "111101101101111", 1: "010110010010111", 2: "110001010100111", 3: "110001010001110",
  4: "101101111001001", 5: "111100110001110", 6: "011100111101111", 7: "111001010010010", 8: "111101111101111", 9: "111101111001110",
  ".": "000000000000010", "-": "000000111000000", "/": "001001010100100", " ": "000000000000000",
};
const textW = (t) => t.length * 4 - 1;
const T = (t, x, y, c, center = false) => {
  if (center) x -= Math.floor(textW(t) / 2);
  const pts = [];
  [...t.toUpperCase()].forEach((ch, i) => {
    const g = FONT[ch] || FONT[" "];
    for (let k = 0; k < 15; k++) if (g[k] === "1") pts.push([x + i * 4 + (k % 3), y + Math.floor(k / 3)]);
  });
  PT(pts, c);
};
// ascii sprite -> points per colour. map: char -> token
const SPR = (rows, ox, oy, map, flip = false) => {
  const by = {};
  rows.forEach((row, y) => {
    const r = flip ? [...row].reverse().join("") : row;
    [...r].forEach((ch, x) => { const c = map[ch]; if (c) (by[c] = by[c] || []).push([ox + x, oy + y]); });
  });
  Object.entries(by).forEach(([c, p]) => PT(p, c));
};
const SCATTER = (colors, weights, count, gap, x1, y1, x2, y2, inside) => {
  rec({ op: "scatter", colors, weights, count, gap, seed: sd % 100000, inside, area: { from: [x1, y1], to: [x2, y2] } });
  // local approximation
  const placed = []; let tries = 0; const tot = weights.reduce((a, b) => a + b, 0);
  while (placed.length < count && tries++ < count * 30) {
    const x = ri(x1, x2), y = ri(y1, y2);
    if (inside && cur.px[y * W + x] !== inside) continue;
    if (placed.some((p) => Math.abs(p[0] - x) < gap && Math.abs(p[1] - y) < gap)) continue;
    placed.push([x, y]); let r = rnd() * tot, i = 0; while (r > weights[i]) { r -= weights[i]; i++; }
    set(x, y, colors[i]);
  }
};

function compose(skip = []) {
  const out = new Uint8Array(W * H * 3).fill(0);
  for (let i = 0; i < W * H; i++) { out[i * 3] = 26; out[i * 3 + 1] = 24; out[i * 3 + 2] = 30; }
  for (const l of layers) {
    if (skip.includes(l.name)) continue;
    for (let i = 0; i < W * H; i++) {
      const c = l.px[i]; if (!c) continue;
      const rgb = hex(PAL[c] || "#ff00ff"); const a = l.opacity / 100;
      for (let k = 0; k < 3; k++) {
        const base = out[i * 3 + k];
        const src = l.blend === "multiply" ? (base * rgb[k]) / 255 : rgb[k];
        out[i * 3 + k] = Math.round(base * (1 - a) + src * a);
      }
    }
  }
  return out;
}
function png(out, file, scale = 2) {
  const w = W * scale, h = H * scale; const raw = Buffer.alloc((w * 3 + 1) * h);
  for (let y = 0; y < h; y++) { raw[y * (w * 3 + 1)] = 0; for (let x = 0; x < w; x++) { const i = (Math.floor(y / scale) * W + Math.floor(x / scale)) * 3; const o = y * (w * 3 + 1) + 1 + x * 3; raw[o] = out[i]; raw[o + 1] = out[i + 1]; raw[o + 2] = out[i + 2]; } }
  const crc = (buf) => { let c, crcT = []; for (let n = 0; n < 256; n++) { c = n; for (let k = 0; k < 8; k++) c = c & 1 ? 0xedb88320 ^ (c >>> 1) : c >>> 1; crcT[n] = c >>> 0; } let r = 0xffffffff; for (const b of buf) r = crcT[(r ^ b) & 255] ^ (r >>> 8); return (r ^ 0xffffffff) >>> 0; };
  const chunk = (t, d) => { const len = Buffer.alloc(4); len.writeUInt32BE(d.length); const td = Buffer.concat([Buffer.from(t), d]); const cr = Buffer.alloc(4); cr.writeUInt32BE(crc(td)); return Buffer.concat([len, td, cr]); };
  const ihdr = Buffer.alloc(13); ihdr.writeUInt32BE(w, 0); ihdr.writeUInt32BE(h, 4); ihdr[8] = 8; ihdr[9] = 2;
  fs.writeFileSync(file, Buffer.concat([Buffer.from([137, 80, 78, 71, 13, 10, 26, 10]), chunk("IHDR", ihdr), chunk("IDAT", zlib.deflateSync(raw)), chunk("IEND", Buffer.alloc(0))]));
}
const saveOps = (dir) => {
  fs.mkdirSync(dir, { recursive: true });
  layers.forEach((l, li) => {
    const ch = []; for (let i = 0; i < l.ops.length; i += 64) ch.push(l.ops.slice(i, i + 64));
    ch.forEach((c, i) => fs.writeFileSync(`${dir}/${String(li).padStart(2, "0")}_${l.name.replace(/\W+/g, "_")}_${i}.json`, JSON.stringify(c)));
    console.log(l.name, l.ops.length, "ops", ch.length, "chunks");
  });
};
module.exports = { use, PAL, W, H, layer, layers, R, L, E, P, PT, T, SPR, SCATTER, seed, rnd, ri, pick, compose, png, saveOps, textW, set, get px() { return cur.px; } };
