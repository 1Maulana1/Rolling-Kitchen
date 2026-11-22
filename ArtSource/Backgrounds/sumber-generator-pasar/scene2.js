const g = require("./lib.js");
require("./scene1.js");
const { layer, use, R, L, E, P, PT, T, SCATTER, seed, rnd, ri, pick, PAL } = g;
const W = g.W, H = g.H;

layer("Shade", "multiply", 55);
layer("Back");
layer("Vendor");
layer("Counter");
layer("Contoh Dagangan");
layer("Tarp");
layer("People");
layer("Overlay");

const SKINS = [["skin.1", "skin.2", "skin.3"], ["skin.0", "skin.1", "skin.2"], ["skin.2", "skin.3", "skin.3"], ["skin.1", "skin.2", "skin.3"]];
const RAMPS = ["red", "blue", "green", "orange", "teal", "purple", "cream"];
const rp = (name, i) => `${name}.${i}`;

// ---------- STALL DEFINITIONS ----------
const YA = 90, YB = 222, SW = 112;
const STALLS = [
  { x: 14, y: YA, kind: "buah", tarp: "orange", slot: null, vendor: { shirt: "green", skin: 0, hat: "hijab", hijab: "teal" } },
  { x: 134, y: YA, kind: "sayur", tarp: "blue", slot: "SAYUR", vendor: { shirt: "red", skin: 1, hat: "topi" } },
  { x: 254, y: YA, kind: "jajan", tarp: "green", slot: null, vendor: { shirt: "purple", skin: 2, hat: "hijab", hijab: "red" } },
  { x: 574, y: YA, kind: "ayam", tarp: "red", slot: null, vendor: { shirt: "blue", skin: 3, hat: "none" } },
  { x: 694, y: YA, kind: "daging", tarp: "teal", slot: "DAGING", vendor: { shirt: "cream", skin: 0, hat: "cap" } },
  { x: 814, y: YA, kind: "kelapa", tarp: "blue", slot: null, vendor: { shirt: "orange", skin: 1, hat: "topi" } },
  { x: 14, y: YB, kind: "kain", tarp: "purple", slot: null, vendor: { shirt: "red", skin: 2, hat: "hijab", hijab: "blue" } },
  { x: 134, y: YB, kind: "ikan", tarp: "teal", slot: "IKAN", vendor: { shirt: "blue", skin: 0, hat: "cap" } },
  { x: 254, y: YB, kind: "plastik", tarp: "red", slot: null, vendor: { shirt: "green", skin: 3, hat: "none" } },
  { x: 574, y: YB, kind: "kerupuk", tarp: "green", slot: null, vendor: { shirt: "orange", skin: 1, hat: "hijab", hijab: "purple" } },
  { x: 694, y: YB, kind: "bumbu", tarp: "orange", slot: "BUMBU", vendor: { shirt: "teal", skin: 2, hat: "topi" } },
  { x: 814, y: YB, kind: "bunga", tarp: "blue", slot: null, vendor: { shirt: "purple", skin: 0, hat: "none" } },
];

// ---------- PEOPLE (procedural) ----------
function person(x, fy, o = {}) {
  const sk = SKINS[o.skin ?? 0], sh = o.shirt || "red", pn = o.pants || "blue.0", hairC = o.hair || "hair";
  const face = o.face || "front";
  use("Shade"); E(x - 6, fy - 2, x + 6, fy + 2, "conc.1");
  use("People");
  const lg = o.legs || pn;
  R(x - 4, fy - 9, x - 2, fy - 1, lg); R(x + 1, fy - 9, x + 3, fy - 1, lg); L(x + 3, fy - 9, x + 3, fy - 1, "conc.0");
  R(x - 5, fy - 1, x - 2, fy, "conc.0"); R(x + 1, fy - 1, x + 4, fy, "conc.0");
  // torso
  R(x - 5, fy - 19, x + 4, fy - 9, rp(sh, 1)); R(x - 5, fy - 19, x - 4, fy - 9, rp(sh, 2)); R(x + 3, fy - 19, x + 4, fy - 9, rp(sh, 0)); L(x - 5, fy - 9, x + 4, fy - 9, rp(sh, 0));
  if (face === "front") PT([[x - 1, fy - 19], [x, fy - 19]], sk[1]);
  // arms
  R(x - 7, fy - 18, x - 6, fy - 11, rp(sh, 1)); R(x + 5, fy - 18, x + 6, fy - 11, rp(sh, 0));
  PT([[x - 7, fy - 10], [x - 6, fy - 10], [x + 5, fy - 10], [x + 6, fy - 10]], sk[1]);
  // head
  E(x - 4, fy - 28, x + 4, fy - 20, sk[1]); PT([[x - 3, fy - 25], [x - 3, fy - 24]], sk[2]); PT([[x + 3, fy - 23], [x + 3, fy - 22]], sk[0]);
  if (face === "front") { PT([[x - 2, fy - 24], [x + 2, fy - 24]], "conc.0"); PT([[x, fy - 22]], sk[0]); }
  if (face === "back") E(x - 4, fy - 28, x + 4, fy - 21, hairC);
  if (face === "left" || face === "right") { const d = face === "left" ? -1 : 1; PT([[x + d * 4, fy - 23]], sk[2]); PT([[x + d * 2, fy - 24]], "conc.0"); }
  const hat = o.hat || "hair";
  if (hat === "hair") P([[x - 4, fy - 24], [x - 4, fy - 27], [x - 2, fy - 29], [x + 2, fy - 29], [x + 4, fy - 27], [x + 4, fy - 24], [x + 3, fy - 26], [x - 3, fy - 26]], hairC);
  if (hat === "cap") { P([[x - 4, fy - 25], [x - 3, fy - 29], [x + 3, fy - 29], [x + 4, fy - 25]], rp(o.hatc || "red", 1)); R(x - 5, fy - 25, x + 1, fy - 24, rp(o.hatc || "red", 0)); }
  if (hat === "topi") { R(x - 4, fy - 30, x + 4, fy - 26, "cream.2"); L(x - 4, fy - 26, x + 4, fy - 26, "cream.0"); }
  if (hat === "caping") { P([[x - 9, fy - 24], [x, fy - 33], [x + 9, fy - 24]], "orange.2"); L(x - 9, fy - 24, x + 9, fy - 24, "orange.0"); L(x - 5, fy - 28, x + 2, fy - 31, "orange.1"); }
  if (hat === "hijab") { const c = o.hijab || "teal"; E(x - 5, fy - 29, x + 5, fy - 19, rp(c, 1)); E(x - 3, fy - 27, x + 3, fy - 21, sk[1]); PT([[x - 2, fy - 24], [x + 2, fy - 24]], "conc.0"); L(x - 5, fy - 21, x - 5, fy - 17, rp(c, 1)); PT([[x - 4, fy - 27]], rp(c, 2)); }
  // carry items
  if (o.carry === "bag") { R(x + 6, fy - 11, x + 9, fy - 5, "cream.2"); PT([[x + 7, fy - 11], [x + 8, fy - 12]], "cream.1"); PT([[x + 7, fy - 8], [x + 8, fy - 7]], pick(["red.2", "green.2", "orange.2"])); }
  if (o.carry === "basket") { E(x + 5, fy - 12, x + 12, fy - 6, "wood.3"); E(x + 6, fy - 13, x + 11, fy - 9, "wood.1"); PT([[x + 7, fy - 13], [x + 9, fy - 14], [x + 10, fy - 13]], pick(["green.2", "red.2", "orange.2"])); }
  if (o.carry === "sack") { P([[x - 8, fy - 24], [x - 1, fy - 27], [x + 4, fy - 20], [x - 4, fy - 15]], "cream.1"); L(x - 8, fy - 24, x - 1, fy - 27, "cream.2"); L(x - 4, fy - 15, x + 4, fy - 20, "cream.0"); }
}
function bust(x, topY, o = {}) { // vendor upper body; legs hidden behind counter
  const fy = topY + 28; const sk = SKINS[o.skin ?? 0], sh = o.shirt || "red";
  use("Vendor");
  R(x - 6, fy - 19, x + 5, fy + 6, rp(sh, 1)); R(x - 6, fy - 19, x - 5, fy + 6, rp(sh, 2)); R(x + 4, fy - 19, x + 5, fy + 6, rp(sh, 0));
  R(x - 9, fy - 17, x - 7, fy + 2, rp(sh, 1)); R(x + 6, fy - 17, x + 8, fy + 2, rp(sh, 0));
  PT([[x - 9, fy + 2], [x - 8, fy + 2], [x + 6, fy + 2], [x + 7, fy + 2]], sk[1]);
  E(x - 4, fy - 28, x + 4, fy - 20, sk[1]); PT([[x - 3, fy - 25]], sk[2]); PT([[x + 3, fy - 23]], sk[0]); PT([[x - 2, fy - 24], [x + 2, fy - 24]], "conc.0"); PT([[x, fy - 22]], sk[0]); PT([[x - 1, fy - 21], [x + 1, fy - 21]], "red.1");
  const hat = o.hat || "none";
  if (hat === "none") P([[x - 4, fy - 24], [x - 4, fy - 27], [x - 2, fy - 29], [x + 2, fy - 29], [x + 4, fy - 27], [x + 4, fy - 24], [x + 3, fy - 26], [x - 3, fy - 26]], "hair");
  if (hat === "cap") { P([[x - 4, fy - 25], [x - 3, fy - 29], [x + 3, fy - 29], [x + 4, fy - 25]], "red.1"); R(x - 5, fy - 25, x + 1, fy - 24, "red.0"); }
  if (hat === "topi") { R(x - 4, fy - 30, x + 4, fy - 26, "cream.2"); L(x - 4, fy - 26, x + 4, fy - 26, "cream.0"); }
  if (hat === "hijab") { const c = o.hijab || "teal"; E(x - 5, fy - 29, x + 5, fy - 19, rp(c, 1)); E(x - 3, fy - 27, x + 3, fy - 21, sk[1]); PT([[x - 2, fy - 24], [x + 2, fy - 24]], "conc.0"); R(x - 6, fy - 22, x + 6, fy - 15, rp(c, 1)); PT([[x - 4, fy - 27]], rp(c, 2)); R(x - 6, fy - 15, x + 6, fy - 14, rp(c, 0)); }
}

// ---------- GOODS HELPERS ----------
function blob(x, y, d, ramp, hi = true) { E(x, y, x + d, y + d, ramp[0]); if (hi) PT([[x + 1, y + 1]], ramp[1]); PT([[x + d - 1, y + d - 1]], ramp[2] || ramp[0]); }
function mound(cx, base, w, h, ramps, step = 4) {
  // dome of blobs
  for (let row = 0; row < h; row += step - 1) {
    const rw = Math.round(w * (1 - (row / (h + 2)) ** 1.6));
    for (let x = cx - rw / 2; x < cx + rw / 2 - step + 2; x += step - 1 + (rnd() < 0.3 ? 1 : 0)) blob(Math.round(x + ri(-1, 1)), base - row - step, step, pick(ramps));
  }
}
function bakul(cx, base, w, h, ramps, fill = true) { // woven basket with produce
  E(cx - w / 2, base - 4, cx + w / 2, base + 2, "wood.1");
  R(cx - w / 2 + 1, base - h + 3, cx + w / 2 - 1, base - 2, "wood.3");
  for (let x = cx - w / 2 + 2; x < cx + w / 2 - 1; x += 3) L(x, base - h + 3, x, base - 2, "wood.2");
  L(cx - w / 2 + 1, base - 5, cx + w / 2 - 1, base - 5, "wood.1");
  E(cx - w / 2, base - h, cx + w / 2, base - h + 5, "wood.4");
  E(cx - w / 2 + 1, base - h + 1, cx + w / 2 - 1, base - h + 4, "wood.1");
  if (fill) mound(cx, base - h + 3, w - 2, 7, ramps, 4);
}
const TOM = [["red.2", "cream.2", "red.0"], ["red.1", "red.2", "red.0"]];
const CAB = [["green.2", "cream.2", "green.1"], ["green.1", "green.2", "green.0"]];
const CHI = [["red.1", "orange.2", "red.0"], ["orange.1", "orange.2", "orange.0"]];
const EGG = [["purple.1", "purple.2", "purple.0"]];
const CAR = [["orange.1", "orange.2", "orange.0"]];
const LEMON = [["orange.2", "cream.2", "orange.1"]];
const BAN = [["orange.2", "cream.2", "orange.0"]];
const MEL = [["green.0", "green.1", "green.0"]];
const SAK = (x, y, w, h, body, fill) => { // open sack with contents
  P([[x + 1, y + 3], [x + w - 1, y + 3], [x + w, y + h], [x, y + h]], body[1]); L(x, y + h, x + w, y + h, body[0]); L(x + w - 1, y + 3, x + w, y + h, body[0]); L(x + 1, y + 3, x, y + h, body[2]);
  E(x - 1, y + 1, x + w + 1, y + 6, body[1]); E(x + 1, y + 2, x + w - 1, y + 5, fill);
  for (let k = 0; k < Math.floor(w); k += 4) PT([[x + 2 + k, y + 8], [x + 3 + k, y + 10]], body[0]);
};
const FISH = (x, y, len, c) => { E(x, y, x + len, y + 3, c[0]); P([[x + len, y + 1], [x + len + 3, y - 1], [x + len + 3, y + 4]], c[0]); PT([[x + 2, y + 1]], "outline"); L(x + 1, y + 3, x + len - 2, y + 3, c[1]); L(x + 2, y, x + len - 3, y, c[2]); };

// ---------- STALL STRUCTURE ----------
function tarpShape(x, y, w, ramp) {
  use("Tarp");
  const top = [[x + 10, y], [x + w - 10, y]], bot = [[x - 6, y + 28], [x + w + 6, y + 28]];
  P([top[0], top[1], bot[1], bot[0]], rp(ramp, 1));
  // lit top strip, shaded lower fold
  P([top[0], top[1], [x + w + 1, y + 6], [x - 1, y + 6]], rp(ramp, 2));
  P([[x - 5, y + 23], [x + w + 5, y + 23], bot[1], bot[0]], rp(ramp, 0));
  // vertical seams
  for (let sx = x + 12; sx < x + w - 6; sx += 14) { const f = (sx - x) / w; L(Math.round(x + 10 + (sx - x - 10) * 0.9), y + 1, Math.round(sx - 6 * (1 - 2 * f) * 0 + (f - 0.5) * 8), y + 27, rp(ramp, 0)); }
  // patches & stains
  for (let k = 0; k < 3; k++) { const px = ri(x + 10, x + w - 26), py = ri(y + 6, y + 18); R(px, py, px + ri(6, 12), py + ri(4, 7), rp(pick(RAMPS.filter((r) => r !== ramp)), 1)); }
  for (let k = 0; k < 5; k++) { const px = ri(x + 4, x + w - 4); L(px, y + 8, px, y + ri(14, 22), rp(ramp, 0)); }
  // sag scallops at the lip
  for (let sx = x - 6; sx < x + w + 6; sx += 9) { E(sx, y + 25, sx + 9, y + 31, rp(ramp, 1)); E(sx + 1, y + 28, sx + 8, y + 31, rp(ramp, 0)); }
  // highlight edge + outline
  L(x + 10, y, x + w - 10, y, rp(ramp, 2)); L(x + 10, y, x - 6, y + 28, rp(ramp, 2));
  L(x + w - 10, y, x + w + 6, y + 28, rp(ramp, 0));
  // tear / hole
  if (rnd() < 0.6) { const hx = ri(x + 14, x + w - 20); R(hx, y + 12, hx + 3, y + 14, "conc.0"); }
  // rope ties to poles
  L(x + 2, y + 6, x + 12, y + 2, "wood.4"); L(x + w - 3, y + 6, x + w - 13, y + 2, "wood.4");
}
function stallBase(s) {
  const { x, y } = s, w = SW;
  // shade under tarp (multiply)
  use("Shade");
  R(x + 4, y + 26, x + w - 4, y + 66, "conc.2");
  P([[x - 2, y + 92], [x + w + 2, y + 92], [x + w + 14, y + 102], [x + 2, y + 104]], "conc.1");
  // floor shadow of counter
  E(x + 4, y + 90, x + w - 2, y + 98, "conc.1");
  use("Back");
  // dark backdrop with hanging fabric
  const tr = s.tarp;
  R(x + 5, y + 22, x + w - 6, y + 68, rp(tr, 0));
  for (let k = x + 7; k < x + w - 7; k += 6) L(k, y + 22, k, y + 68, "conc.0");
  R(x + 5, y + 22, x + w - 6, y + 24, "conc.0");
  // poles
  for (const px of [x + 1, x + w - 4]) { R(px, y + 5, px + 2, y + 95, "wood.3"); L(px, y + 5, px, y + 95, "wood.4"); L(px + 2, y + 5, px + 2, y + 95, "wood.1"); R(px - 1, y + 94, px + 3, y + 96, "wood.1"); }
  // cross bar for hanging goods
  R(x + 2, y + 27, x + w - 3, y + 28, "wood.2"); L(x + 2, y + 28, x + w - 3, y + 28, "wood.1");
}
function counter(s) {
  const { x, y } = s, w = SW, cl = s.cloth;
  use("Counter");
  // floor contact shadow strip drawn in front handled by Shade
  // top face (foreshortened)
  P([[x + 5, y + 64], [x + w - 5, y + 64], [x + w, y + 72], [x, y + 72]], cl ? rp(cl, 1) : "wood.3");
  if (cl) { for (let k = 0; k < 8; k++) { L(x + 7 + k * 13, y + 64, x + 2 + k * 14, y + 72, rp(cl, 2)); } L(x + 5, y + 64, x + w - 5, y + 64, rp(cl, 2)); }
  else { L(x + 5, y + 64, x + w - 5, y + 64, "wood.4"); for (let k = 0; k < 6; k++) L(x + 6 + k * 18, y + 65, x + 2 + k * 18, y + 72, "wood.2"); L(x, y + 72, x + w, y + 72, "wood.1"); }
  // front face
  R(x, y + 73, x + w, y + 93, cl ? rp(cl, 1) : "wood.2");
  if (cl) { for (let k = 0; k < 10; k++) { L(x + 4 + k * 11, y + 73, x + 4 + k * 11 + ri(-1, 1), y + 93, rp(cl, 0)); L(x + 8 + k * 11, y + 73, x + 8 + k * 11, y + 93, rp(cl, 2)); } }
  else { for (let k = 0; k < 12; k++) { L(x + 2 + k * 9, y + 73, x + 2 + k * 9, y + 93, "wood.1"); L(x + 3 + k * 9, y + 73, x + 3 + k * 9, y + 93, "wood.3"); } for (const gx of [x + 20, x + 60, x + 80]) PT([[gx, y + 80], [gx + 1, y + 82], [gx - 1, y + 86]], "wood.0"); }
  R(x, y + 73, x + w, y + 74, "wood.1"); R(x, y + 91, x + w, y + 93, "wood.0");
  // legs
  R(x, y + 93, x + 3, y + 96, "wood.1"); R(x + w - 3, y + 93, x + w, y + 96, "wood.1");
  // under-counter crate peek
  if (rnd() < 0.7) { R(x + ri(10, 40), y + 94, x + ri(46, 80), y + 97, "wood.1"); }
}

// ---------- KIND-SPECIFIC GOODS ----------
// coordinates: counter top plane y0+64..72; items sit on y0+70 baseline, extend upward
function goods(s) {
  const { x, y, kind } = s; const by = y + 70; // baseline on counter top
  const slot = !!s.slot;
  use(slot ? "Contoh Dagangan" : "Counter");
  const cx = (i, n) => x + 10 + ((SW - 20) * (i + 0.5)) / n;
  if (kind === "buah") {
    // watermelon half-pile, oranges basket, mango tray, pineapples
    for (let k = 0; k < 3; k++) { E(x + 8 + k * 11, by - 11, x + 20 + k * 11, by, "green.0"); E(x + 9 + k * 11, by - 10, x + 19 + k * 11, by - 2, "green.1"); for (let t = 0; t < 4; t++) L(x + 11 + k * 11 + t * 2, by - 9, x + 11 + k * 11 + t * 2, by - 3, "green.2"); }
    bakul(x + 60, by, 22, 9, LEMON); bakul(x + 84, by, 22, 9, [["orange.1", "orange.2", "orange.0"], ["red.2", "cream.2", "red.0"]]);
    E(x + 96, by - 12, x + 104, by - 2, "orange.2"); P([[x + 96, by - 12], [x + 100, by - 20], [x + 104, by - 12]], "green.2"); L(x + 98, by - 11, x + 102, by - 4, "orange.0");
    // hanging bananas under bar
    use("Tarp"); for (let k = 0; k < 4; k++) { const bx = x + 14 + k * 24; L(bx, y + 28, bx, y + 34, "wood.2"); for (let f = 0; f < 5; f++) { E(bx - 4 + f * 2, y + 33 + (f % 2), bx - 2 + f * 2, y + 44 + (f % 2), "orange.2"); L(bx - 4 + f * 2, y + 33, bx - 4 + f * 2, y + 43, "orange.1"); } }
  }
  if (kind === "sayur") {
    bakul(cx(0, 4), by, 22, 10, CAB); bakul(cx(1, 4), by, 22, 10, TOM); bakul(cx(2, 4), by, 22, 10, CHI); bakul(cx(3, 4), by, 22, 10, EGG);
    // carrots + long beans bunches leaning on counter back
    for (let k = 0; k < 6; k++) { const bx = x + 12 + k * 15; L(bx, by - 13, bx + 2, by - 2, "green.1"); L(bx + 1, by - 13, bx + 3, by - 2, "green.2"); }
    R(x + 8, by - 3, x + 12, by - 1, "red.2"); // rubber band bundles
  }
  if (kind === "jajan") {
    // trays of kue basah
    for (let r = 0; r < 2; r++) { R(x + 8, by - 9 + r * 5, x + 100, by - 6 + r * 5, "steel.1"); for (let k = 0; k < 11; k++) { const c = pick([["red.2", "red.0"], ["green.2", "green.0"], ["orange.2", "orange.0"], ["cream.2", "cream.0"], ["purple.2", "purple.0"]]); E(x + 10 + k * 8, by - 11 + r * 5, x + 15 + k * 8, by - 6 + r * 5, c[0]); PT([[x + 11 + k * 8, by - 10 + r * 5]], "cream.2"); PT([[x + 14 + k * 8, by - 6 + r * 5]], c[1]); } }
    // tahu & tempe in basins
    for (let k = 0; k < 3; k++) { E(x + 14 + k * 30, by - 21, x + 34 + k * 30, by - 14, "steel.1"); E(x + 16 + k * 30, by - 20, x + 32 + k * 30, by - 15, k === 1 ? "cream.0" : "orange.0"); for (let t = 0; t < 5; t++) R(x + 19 + k * 30 + t * 3, by - 19, x + 20 + k * 30 + t * 3, by - 17, k === 1 ? "cream.2" : "orange.2"); }
  }
  if (kind === "ayam") {
    // wire cage with chickens + egg trays
    R(x + 6, by - 24, x + 56, by - 1, "steel.0"); R(x + 7, by - 23, x + 55, by - 2, "conc.0");
    for (let k = x + 9; k < x + 55; k += 4) L(k, by - 23, k, by - 2, "steel.1"); for (let k = by - 21; k < by - 2; k += 5) L(x + 7, k, x + 55, k, "steel.1");
    for (let k = 0; k < 5; k++) { const hx = x + 12 + k * 9, hy = by - 11 - (k % 2) * 8; E(hx, hy, hx + 8, hy + 6, pick(["orange.1", "cream.2", "red.1", "cream.1"])); E(hx + 5, hy - 3, hx + 9, hy + 1, "red.2"); PT([[hx + 9, hy - 1]], "orange.2"); PT([[hx + 7, hy - 2]], "outline"); }
    for (let k = 0; k < 3; k++) { const ex = x + 62 + k * 16; R(ex, by - 8, ex + 13, by - 1, "cream.1"); for (let t = 0; t < 4; t++) for (let u = 0; u < 2; u++) E(ex + 1 + t * 3, by - 7 + u * 3, ex + 3 + t * 3, by - 5 + u * 3, "cream.2"); }
  }
  if (kind === "daging") {
    use("Tarp"); // hanging meats from bar
    for (let k = 0; k < 6; k++) { const hx = x + 10 + k * 17; L(hx, y + 28, hx, y + 33, "steel.1"); P([[hx - 4, y + 33], [hx + 4, y + 33], [hx + 3, y + 48], [hx - 3, y + 50]], "meat.1"); L(hx - 3, y + 36, hx + 2, y + 36, "cream.2"); L(hx - 3, y + 42, hx + 3, y + 42, "meat.0"); PT([[hx - 2, y + 34]], "meat.2"); }
    use("Contoh Dagangan");
    // chopping block (stump) + cleaver + trays of meat
    E(x + 8, by - 8, x + 28, by - 1, "wood.2"); E(x + 8, by - 11, x + 28, by - 5, "wood.4"); E(x + 12, by - 10, x + 24, by - 7, "wood.3"); L(x + 14, by - 15, x + 22, by - 10, "steel.2"); R(x + 22, by - 11, x + 26, by - 10, "wood.1");
    for (let k = 0; k < 2; k++) { R(x + 36 + k * 36, by - 9, x + 68 + k * 36 - 8, by - 1, "steel.1"); for (let t = 0; t < 5; t++) E(x + 38 + k * 36 + t * 5, by - 8, x + 43 + k * 36 + t * 5, by - 3, pick(["meat.1", "meat.2", "red.1"])); }
  }
  if (kind === "kelapa") {
    mound(x + 30, by, 40, 16, [["wood.2", "wood.3", "wood.1"], ["wood.1", "wood.2", "wood.0"]], 7);
    for (let k = 0; k < 4; k++) { E(x + 62 + k * 10, by - 7, x + 70 + k * 10, by - 1, "cream.2"); E(x + 63 + k * 10, by - 6, x + 69 + k * 10, by - 3, "cream.1"); } // cut coconuts
    L(x + 100, by - 9, x + 105, by - 1, "steel.2"); R(x + 103, by - 5, x + 106, by - 4, "wood.1"); // machete
  }
  if (kind === "kain") {
    use("Tarp"); // batik hung from bar
    for (let k = 0; k < 5; k++) { const hx = x + 8 + k * 21, c = pick(RAMPS); R(hx, y + 29, hx + 17, y + 50, rp(c, 1)); for (let t = 0; t < 6; t++) { PT([[hx + 3 + (t % 3) * 5, y + 33 + Math.floor(t / 3) * 8], [hx + 4 + (t % 3) * 5, y + 34 + Math.floor(t / 3) * 8]], rp(c, 2)); } R(hx, y + 29, hx + 17, y + 30, rp(c, 0)); L(hx + 17, y + 29, hx + 17, y + 50, rp(c, 0)); }
    use("Counter"); for (let k = 0; k < 5; k++) { const c = pick(RAMPS); R(x + 8 + k * 21, by - 9, x + 24 + k * 21, by - 1, rp(c, 1)); R(x + 8 + k * 21, by - 9, x + 24 + k * 21, by - 8, rp(c, 2)); L(x + 8 + k * 21, by - 1, x + 24 + k * 21, by - 1, rp(c, 0)); PT([[x + 12 + k * 21, by - 5], [x + 18 + k * 21, by - 4]], rp(c, 2)); }
  }
  if (kind === "ikan") {
    for (let k = 0; k < 3; k++) { const bx = x + 6 + k * 34; R(bx, by - 10, bx + 30, by - 1, "cream.2"); R(bx, by - 10, bx + 30, by - 1, "steel.1", false); R(bx + 1, by - 9, bx + 29, by - 8, "water.2"); for (let t = 0; t < 4; t++) FISH(bx + 3 + t * 6, by - 8 + (t % 2) * 3, 8, [["steel.1", "steel.0", "steel.2"], ["water.1", "water.0", "steel.2"]][t % 2]); PT([[bx + 4, by - 10], [bx + 12, by - 10], [bx + 22, by - 10]], "cream.2"); }
    for (let k = 0; k < 7; k++) E(x + 7 + k * 5, by - 13 - (k % 2), x + 11 + k * 5, by - 10, "meat.2"); // shrimp pile
  }
  if (kind === "plastik") {
    for (let k = 0; k < 4; k++) { const bx = x + 8 + k * 11, c = pick(["red", "blue", "orange", "green"]); P([[bx, by - 14], [bx + 9, by - 14], [bx + 8, by - 1], [bx + 1, by - 1]], rp(c, 1)); L(bx + 1, by - 14, bx + 1, by - 1, rp(c, 2)); L(bx + 8, by - 14, bx + 8, by - 1, rp(c, 0)); L(bx, by - 14, bx + 9, by - 14, rp(c, 2)); }
    for (let k = 0; k < 3; k++) { const bx = x + 55 + k * 16; E(bx, by - 8, bx + 12, by - 1, rp(pick(["blue", "red", "green"]), 1)); L(bx + 2, by - 6, bx + 10, by - 6, "cream.2"); }
    for (let k = 0; k < 4; k++) L(x + 6 + k * 3, by - 28 + k, x + 8 + k * 3, by - 14, "wood.3"); // brooms leaning
    use("Tarp"); for (let k = 0; k < 5; k++) { const hx = x + 12 + k * 20; L(hx, y + 28, hx, y + 38, "steel.1"); E(hx - 4, y + 38, hx + 4, y + 44, pick(["red.2", "blue.2", "orange.2"])); } // hanging sandals/buckets
  }
  if (kind === "kerupuk") {
    use("Tarp"); for (let k = 0; k < 6; k++) { const hx = x + 9 + k * 17; L(hx, y + 28, hx, y + 33, "steel.1"); E(hx - 3, y + 33, hx + 3, y + 47, "cream.1"); L(hx - 2, y + 37, hx + 2, y + 37, "cream.0"); PT([[hx - 1, y + 35]], "cream.2"); } // ikan asin/kerupuk bundles
    use("Counter"); for (let k = 0; k < 4; k++) { const jx = x + 8 + k * 26; R(jx, by - 17, jx + 18, by - 1, "steel.2"); R(jx, by - 17, jx + 18, by - 1, "steel.0", false); R(jx + 2, by - 15, jx + 16, by - 3, k % 2 ? "orange.2" : "cream.2"); R(jx - 1, by - 19, jx + 19, by - 18, "red.1"); }
  }
  if (kind === "bumbu") {
    SAK(x + 6, by - 19, 20, 19, ["cream.1", "cream.2", "cream.0"], "orange.1"); SAK(x + 30, by - 19, 20, 19, ["cream.1", "cream.2", "cream.0"], "red.1"); SAK(x + 54, by - 19, 20, 19, ["cream.1", "cream.2", "cream.0"], "conc.1"); SAK(x + 78, by - 19, 20, 19, ["cream.1", "cream.2", "cream.0"], "cream.2");
    use("Tarp"); for (let k = 0; k < 5; k++) { const hx = x + 12 + k * 21; L(hx, y + 28, hx, y + 34, "steel.1"); E(hx - 4, y + 34, hx + 4, y + 44, pick(["purple.1", "red.1", "cream.2"])); PT([[hx - 2, y + 36]], "cream.2"); } // hanging shallot/garlic nets
  }
  if (kind === "bunga") {
    for (let k = 0; k < 5; k++) { const bx = x + 8 + k * 20; R(bx, by - 8, bx + 14, by - 1, "steel.1"); L(bx, by - 8, bx + 14, by - 8, "steel.2"); E(bx - 2, by - 20, bx + 16, by - 7, "green.1"); for (let t = 0; t < 7; t++) { const fx = bx + ri(-1, 14), fy = by - ri(8, 19); R(fx, fy, fx + 1, fy + 1, pick(["red.2", "orange.2", "cream.2", "purple.2", "blue.2"])); } }
    use("Tarp"); for (let k = 0; k < 5; k++) { const hx = x + 10 + k * 22; L(hx, y + 28, hx, y + 33, "green.1"); E(hx - 5, y + 33, hx + 5, y + 43, "green.1"); PT([[hx - 2, y + 36], [hx + 2, y + 38], [hx, y + 40]], pick(["red.2", "orange.2", "purple.2"])); }
  }
}

function signboard(s) {
  const { x, y } = s; const txt = s.slot || null;
  use("Counter");
  const cx = x + SW / 2; const sw = Math.max(28, g.textW(txt || "AAAAA") + 8);
  R(cx - sw / 2, y + 76, cx + sw / 2, y + 86, "wood.0"); R(cx - sw / 2 + 1, y + 77, cx + sw / 2 - 1, y + 85, "cream.1"); R(cx - sw / 2 + 1, y + 77, cx + sw / 2 - 1, y + 78, "cream.2");
  if (txt) { use("Contoh Dagangan"); T(txt, cx, y + 79, "wood.0", true); }
  else { const names = { buah: "BUAH", jajan: "JAJAN", ayam: "AYAM", kelapa: "KELAPA", kain: "KAIN", plastik: "PLASTIK", kerupuk: "KERUPUK", bunga: "BUNGA" }; T(names[s.kind] || "", cx, y + 79, "wood.0", true); }
}

// ---- build all stalls (grouped by layer order so z-order is right) ----
seed(21);
STALLS.forEach((s) => { s.cloth = pick([null, null, "blue", "red", "green", null]); });
STALLS.forEach(stallBase);
STALLS.forEach((s) => bust(s.x + SW / 2, s.y + 34, s.vendor));
STALLS.forEach((s) => { counter(s); });
STALLS.forEach((s) => { goods(s); signboard(s); });
STALLS.forEach((s) => tarpShape(s.x, s.y, SW, s.tarp));
// bulbs hanging from tarps (bohlam)
use("Tarp"); STALLS.forEach((s) => { const bx = s.x + 20 + ri(0, 70); L(bx, s.y + 28, bx, s.y + 33, "outline"); E(bx - 2, s.y + 33, bx + 2, s.y + 37, "orange.2"); PT([[bx - 1, s.y + 34]], "cream.2"); });

module.exports = { person, bust, STALLS, SW, mound, bakul, SAK, FISH, CAB, TOM, CHI, EGG, CAR, LEMON, BAN };
if (require.main === module) { g.png(g.compose(), "scene2.png", 2); }
