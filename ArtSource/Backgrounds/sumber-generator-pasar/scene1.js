const g = require("./lib.js");
const { R, L, E, P, PT, T, SPR, SCATTER, seed, rnd, ri, pick, PAL } = g;
// pale wall paints
[["pale.0", "#d8b8a0"], ["pale.1", "#b8d0c0"], ["pale.2", "#c8d4e0"], ["pale.3", "#e8d890"]].forEach(([k, v]) => (PAL[k] = v));
const W = g.W, H = g.H;

// ---------------- GROUND ----------------
g.layer("Ground");
seed(11);
R(0, 0, W - 1, H - 1, "slab.2");
// damp zones: subtle darker tone with speckled edges
for (let i = 0; i < 12; i++) { const x = ri(0, 900), y = ri(100, 380), w = ri(50, 130), h = ri(10, 26); E(x, y, x + w, y + h, "slab.1"); SCATTER(["slab.0"], [1], Math.floor(w * h / 14), 2, x + 2, y + 2, x + w - 2, y + h - 2, "slab.1"); }
for (let i = 0; i < 10; i++) { const x = ri(0, 900), y = ri(100, 380), w = ri(30, 90), h = ri(8, 18); E(x, y, x + w, y + h, "slab.3"); }
// slab joints
for (let x = 64; x < W; x += 128) L(x, 84, x, H - 1, "slab.0");
for (const y of [160, 256, 352]) L(0, y, W - 1, y, "slab.0");
SCATTER(["slab.0", "slab.3", "conc.2"], [5, 5, 1], 1400, 2, 0, 86, W - 1, H - 1, null);
// small puddles with sky reflection
for (const [x, y, w, h] of [[120, 214, 26, 7], [338, 226, 20, 6], [632, 176, 28, 7], [806, 228, 20, 6], [470, 300, 24, 6], [40, 330, 22, 6], [880, 322, 26, 6]]) { E(x, y, x + w, y + h, "water.0"); E(x + 1, y + 1, x + w - 1, y + h - 1, "water.1"); L(x + 3, y + 2, x + 9, y + 2, "water.2"); PT([[x + 4, y + h - 2], [x + w - 6, y + 2]], "cream.2"); }
// gutter (selokan) with grate at wall foot
R(0, 76, W - 1, 83, "conc.1"); R(0, 77, W - 1, 82, "conc.0");
for (let x = 0; x < W; x += 6) R(x, 77, x + 1, 82, "conc.2");
L(0, 84, W - 1, 84, "slab.3");
// raised front step
R(0, 70, W - 1, 75, "conc.4"); L(0, 70, W - 1, 70, "cream.2"); L(0, 75, W - 1, 75, "conc.2");

// ---------------- BACK WALL (shophouses) ----------------
g.layer("Dinding");
const B = [
  [0, 128, "pale.0", "cream.1", "TOKO MAJU", "wood"],
  [128, 238, "pale.1", "teal.1", "WARUNG IBU", "shutter"],
  [238, 366, "cream.1", "orange.1", "SEMBAKO", "open"],
  [366, 470, "pale.2", "blue.1", "APOTEK", "shutter"],
  [470, 592, "pale.3", "red.1", "TOKO KAIN", "open"],
  [592, 702, "pale.1", "green.1", "FOTO COPY", "shutter"],
  [702, 822, "pale.0", "orange.0", "DEPOT AIR", "open"],
  [822, 960, "cream.1", "blue.0", "BERKAH", "wood"],
];
seed(7);
B.forEach(([x1, x2, wall, accent, name, kind], bi) => {
  const w = x2 - x1;
  // party wall between buildings
  R(x1, 0, x1 + 1, 69, "conc.0");
  // roof: corrugated zinc
  R(x1 + 2, 0, x2 - 1, 13, "steel.0");
  for (let x = x1 + 2; x < x2; x += 4) { L(x, 0, x, 13, "steel.1"); L(x + 1, 0, x + 1, 13, "steel.0"); }
  L(x1 + 2, 13, x2 - 1, 13, "conc.0"); R(x1 + 2, 14, x2 - 1, 16, "conc.1");
  // rust streaks
  for (let k = 0; k < 4; k++) { const rx = ri(x1 + 6, x2 - 6); L(rx, 3, rx, ri(9, 13), "orange.0"); }
  // wall body
  R(x1 + 2, 17, x2 - 1, 69, wall);
  // vertical weathering + stains
  for (let k = 0; k < 6; k++) { const sx = ri(x1 + 4, x2 - 4); const len = ri(8, 30); for (let y = 17; y < 17 + len; y++) if (rnd() < 0.7) PT([[sx + (rnd() < 0.2 ? 1 : 0), y]], "conc.3"); }
  for (let k = 0; k < 40; k++) PT([[ri(x1 + 3, x2 - 2), ri(18, 68)]], pick(["conc.3", "cream.0"]));
  // upper windows
  const nWin = Math.max(2, Math.floor((w - 20) / 44));
  for (let k = 0; k < nWin; k++) {
    const wx = x1 + 12 + Math.floor(((w - 24) / nWin) * k) + 4;
    R(wx - 1, 20, wx + 16, 40, "wood.1"); // frame
    if ((k + bi) % 3 === 0) { // open window, curtain
      R(wx + 1, 22, wx + 15, 38, "conc.0");
      R(wx + 1, 22, wx + 7, 38, pick(["red.1", "blue.1", "green.1"])); L(wx + 4, 22, wx + 4, 38, "conc.1");
      R(wx + 8, 22, wx + 15, 26, "conc.1");
    } else { // louvre shutters
      R(wx + 1, 22, wx + 15, 38, "wood.2");
      for (let y = 23; y < 38; y += 3) { L(wx + 1, y, wx + 15, y, "wood.1"); L(wx + 1, y + 1, wx + 15, y + 1, "wood.3"); }
      L(wx + 8, 22, wx + 8, 38, "wood.0");
    }
    R(wx - 2, 41, wx + 17, 42, "conc.4"); L(wx - 2, 42, wx + 17, 42, "conc.2");
    // plant pot / laundry on some
    if ((k + bi) % 2 === 0) { R(wx + 2, 38, wx + 5, 41, "wood.3"); PT([[wx + 2, 36], [wx + 3, 35], [wx + 4, 36], [wx + 5, 37]], "green.1"); }
  }
  // laundry line
  if (bi % 2 === 1) { const ly = 46; L(x1 + 8, ly, x2 - 8, ly + 2, "conc.1"); for (let lx = x1 + 14; lx < x2 - 20; lx += 18) { const c = pick(["red.1", "blue.2", "orange.2", "cream.2", "green.2"]); R(lx, ly + 1, lx + 8, ly + 10, c); L(lx, ly + 1, lx + 8, ly + 1, "conc.0"); } }
  // AC unit / dish
  if (bi % 3 === 0) { R(x2 - 26, 22, x2 - 8, 32, "steel.2"); R(x2 - 26, 22, x2 - 8, 32, "steel.0", false); E(x2 - 22, 24, x2 - 12, 30, "steel.0"); R(x2 - 26, 33, x2 - 8, 34, "conc.1"); }
  if (bi % 4 === 1) { E(x1 + 8, 21, x1 + 22, 32, "cream.2"); E(x1 + 8, 21, x1 + 22, 32, "conc.1", false); L(x1 + 15, 32, x1 + 15, 38, "conc.1"); }
  // sign board across (hand-painted)
  const sw = g.textW(name) + 10, cx = Math.round((x1 + x2) / 2);
  const sy = 48;
  R(cx - sw / 2, sy, cx + sw / 2, sy + 11, "wood.0"); R(cx - sw / 2 + 1, sy + 1, cx + sw / 2 - 1, sy + 10, accent); R(cx - sw / 2, sy, cx + sw / 2, sy + 11, "outline", false);
  T(name, cx, sy + 3, "cream.2", true);
  PT([[cx - sw / 2 + 2, sy + 2], [cx + sw / 2 - 2, sy + 9]], "cream.1");
  // shop front (ground floor y 58-69 visible above step)
  R(x1 + 6, 60, x2 - 6, 69, "conc.0");
  if (kind === "shutter") { R(x1 + 6, 60, x2 - 6, 69, "steel.1"); for (let y = 61; y < 69; y += 2) L(x1 + 6, y, x2 - 6, y, "steel.0"); R(x1 + 6, 60, x2 - 6, 61, "steel.2"); }
  else if (kind === "wood") { for (let x = x1 + 6; x < x2 - 6; x += 7) { R(x, 60, x + 5, 69, "wood.2"); L(x + 6, 60, x + 6, 69, "wood.0"); } L(x1 + 6, 62, x2 - 6, 62, "wood.1"); }
  else { // open shop: dark interior + shelves + goods glimpses
    R(x1 + 8, 61, x2 - 8, 69, "conc.0");
    for (let x = x1 + 10; x < x2 - 14; x += 9) { R(x, 62, x + 6, 64, pick(["red.2", "orange.2", "green.2", "blue.2", "cream.2"])); R(x, 66, x + 6, 68, pick(["orange.1", "cream.1", "purple.2"])); }
    L(x1 + 8, 65, x2 - 8, 65, "wood.2");
  }
  // wall foot dirt
  for (let x = x1 + 3; x < x2 - 1; x += 2) if (rnd() < 0.6) PT([[x, 69]], "conc.2");
});
// roof gutters/drain pipes
for (const x of [127, 237, 365, 469, 591, 701, 821]) { R(x - 1, 14, x + 1, 69, "steel.1"); L(x, 14, x, 69, "steel.2"); }

if (require.main === module) { g.png(g.compose(), "scene1.png", 2); }
