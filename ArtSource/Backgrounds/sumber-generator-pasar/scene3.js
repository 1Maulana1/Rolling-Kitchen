const g = require("./lib.js");
const { person, bust, mound, bakul, SAK, FISH, CAB, TOM, CHI, EGG, CAR, LEMON } = require("./scene2.js");
const { use, R, L, E, P, PT, T, SCATTER, seed, rnd, ri, pick, W, H } = g;
const rp = (n, i) => `${n}.${i}`;

// ---------- helpers ----------
function child(x, fy, o = {}) {
  const sk = ["skin.1", "skin.2", "skin.3"], sh = o.shirt || "orange";
  use("Shade"); E(x - 4, fy - 1, x + 4, fy + 2, "conc.1");
  use("People");
  R(x - 3, fy - 6, x - 2, fy - 1, "blue.0"); R(x + 1, fy - 6, x + 2, fy - 1, "blue.0");
  R(x - 4, fy - 13, x + 3, fy - 6, rp(sh, 1)); R(x - 4, fy - 13, x - 3, fy - 6, rp(sh, 2)); R(x + 2, fy - 13, x + 3, fy - 6, rp(sh, 0));
  E(x - 3, fy - 20, x + 3, fy - 14, sk[1]); PT([[x - 2, fy - 17], [x + 1, fy - 17]], "conc.0"); P([[x - 3, fy - 17], [x - 3, fy - 20], [x, fy - 21], [x + 3, fy - 20], [x + 3, fy - 18], [x + 2, fy - 19], [x - 2, fy - 19]], "hair");
  if (o.balloon) { L(x + 5, fy - 13, x + 8, fy - 30, "conc.3"); E(x + 4, fy - 40, x + 12, fy - 29, rp(o.balloon, 1)); PT([[x + 6, fy - 37]], rp(o.balloon, 2)); }
}
function seated(x, y, o = {}) { // eater on stool, y = head top; on People layer
  const sk = ["skin.0", "skin.1", "skin.2"][o.skin ?? 1]; const sh = o.shirt || "red";
  use("Shade"); E(x - 8, y + 26, x + 8, y + 30, "conc.1");
  use("People");
  R(x - 5, y + 21, x - 3, y + 27, "blue.0"); R(x + 2, y + 21, x + 4, y + 27, "blue.0"); R(x - 6, y + 27, x - 3, y + 28, "conc.0"); R(x + 1, y + 27, x + 5, y + 28, "conc.0"); // legs
  R(x - 6, y + 11, x + 5, y + 21, rp(sh, 1)); R(x - 6, y + 11, x - 5, y + 21, rp(sh, 2)); R(x + 4, y + 11, x + 5, y + 21, rp(sh, 0));
  R(x - 8, y + 12, x - 7, y + 19, rp(sh, 1)); R(x + 6, y + 12, x + 7, y + 19, rp(sh, 0));
  E(x - 4, y, x + 4, y + 8, sk); PT([[x - 3, y + 3]], "skin.3"); PT([[x - 2, y + 4], [x + 2, y + 4]], "conc.0");
  P([[x - 4, y + 4], [x - 4, y + 1], [x - 2, y - 1], [x + 2, y - 1], [x + 4, y + 1], [x + 4, y + 4], [x + 3, y + 2], [x - 3, y + 2]], "hair");
  if (o.hijab) { E(x - 5, y - 1, x + 5, y + 9, rp(o.hijab, 1)); E(x - 3, y + 1, x + 3, y + 7, sk); PT([[x - 2, y + 4], [x + 2, y + 4]], "conc.0"); }
  // bowl
  R(x - 4, y + 17, x + 4, y + 21, "cream.2"); R(x - 4, y + 19, x + 4, y + 21, "cream.0"); L(x - 3, y + 17, x + 3, y + 17, "orange.1");
}
function stool(x, y, c = "red") { use("People"); R(x - 5, y, x + 5, y + 3, rp(c, 1)); L(x - 5, y, x + 5, y, rp(c, 2)); L(x - 4, y + 3, x - 4, y + 9, "conc.0"); L(x + 4, y + 3, x + 4, y + 9, "conc.0"); }
function bike(x, y, body, dir = 1) {
  const X = (dx) => (dir > 0 ? x + dx : x + 34 - dx);
  use("Shade"); E(x, y - 2, x + 36, y + 3, "conc.1");
  use("People");
  for (const wx of [5, 27]) { E(X(wx) - 6, y - 12, X(wx) + 6, y, "conc.0"); E(X(wx) - 3, y - 9, X(wx) + 3, y - 3, "steel.1"); PT([[X(wx), y - 6]], "steel.2"); }
  P([[X(8), y - 9], [X(13), y - 17], [X(25), y - 18], [X(31), y - 12], [X(27), y - 8], [X(14), y - 8]], rp(body, 1));
  P([[X(8), y - 9], [X(7), y - 21], [X(11), y - 22], [X(14), y - 12]], rp(body, 2));
  R(Math.min(X(14), X(26)), y - 19, Math.max(X(14), X(26)), y - 17, "conc.0");
  L(X(7), y - 22, X(4), y - 24, "conc.0"); E(X(2) - 2, y - 20, X(2) + 2, y - 16, "orange.2"); PT([[X(2), y - 18]], "cream.2");
  L(X(6), y - 14, X(5), y - 6, "steel.1"); L(X(17), y - 6, X(33), y - 4, "steel.0"); L(X(27), y - 8, X(31), y - 12, rp(body, 0));
}
function parasol(cx, y, w, c1, c2) {
  use("Tarp");
  const n = 8;
  for (let i = 0; i < n; i++) { const x0 = cx - w + (2 * w * i) / n, x1 = cx - w + (2 * w * (i + 1)) / n; P([[x0, y + 8 - Math.abs(i - n / 2 + 0.5) * 0], [x1, y + 8], [cx, y - 8]], i % 2 ? c1 : c2); }
  L(cx - w, y + 8, cx + w, y + 8, "conc.0"); PT([[cx, y - 9]], "steel.2"); for (let i = 0; i < n; i++) { const x0 = cx - w + (2 * w * i) / n; E(x0, y + 6, x0 + (2 * w) / n, y + 11, i % 2 ? c1 : c2); }
  L(cx, y + 9, cx, y + 48, "wood.2");
}
function chicken(x, y, c, dir = 1) { use("People"); const X = (d) => (dir > 0 ? x + d : x + 9 - d); E(X(0), y - 5, X(8), y, c[0]); E(X(6), y - 8, X(10), y - 4, c[0]); PT([[X(10), y - 6]], "orange.2"); PT([[X(8), y - 7]], "outline"); PT([[X(8), y - 9], [X(9), y - 9]], "red.2"); L(X(2), y, X(2), y + 2, "orange.1"); L(X(5), y, X(5), y + 2, "orange.1"); PT([[X(1), y - 3], [X(2), y - 3]], c[1]); use("Shade"); E(x - 1, y + 1, x + 10, y + 3, "conc.1"); }
function bubble(x, y, txt, c = "cream.2") { use("Overlay"); const w = g.textW(txt) + 6; R(x - w / 2, y, x + w / 2, y + 9, c); R(x - w / 2, y, x + w / 2, y + 9, "conc.0", false); PT([[x - 2, y + 10], [x - 1, y + 10], [x - 1, y + 11], [x, y + 10]], c); PT([[x - 3, y + 9], [x - 2, y + 9], [x - 1, y + 9], [x, y + 9]], c); T(txt, x, y + 2, "conc.0", true); }
function crate(x, y, c = "wood.2") { use("People"); R(x, y - 8, x + 14, y, c); R(x, y - 8, x + 14, y - 6, "wood.3"); L(x, y - 4, x + 14, y - 4, "wood.1"); L(x + 7, y - 8, x + 7, y, "wood.1"); R(x, y - 8, x + 14, y, "wood.0", false); use("Shade"); E(x, y - 1, x + 16, y + 3, "conc.1"); }

// ---------- CENTER ALLEY: bakso cart with tent, seated eaters ----------
seed(5);
// cart
use("Shade"); E(386, 276, 452, 284, "conc.1");
use("People");
R(392, 246, 444, 272, "red.1"); R(392, 246, 444, 249, "red.2"); R(392, 246, 393, 272, "red.2"); R(443, 246, 444, 272, "red.0");
R(394, 232, 442, 246, "steel.1"); R(395, 233, 441, 245, "water.2"); L(395, 235, 410, 235, "cream.2"); L(395, 238, 402, 238, "cream.2"); // glass case
for (let k = 0; k < 7; k++) E(398 + k * 6, 240, 402 + k * 6, 244, pick(["cream.1", "cream.2", "orange.1"]));
R(392, 272, 444, 274, "conc.0"); E(398, 270, 408, 280, "conc.0"); E(400, 272, 406, 278, "steel.1"); E(428, 270, 438, 280, "conc.0"); E(430, 272, 436, 278, "steel.1");
R(396, 252, 440, 262, "red.0"); T("BAKSO", 418, 254, "cream.2", true); PT([[396, 250], [440, 250]], "orange.2");
bust(418, 204, { shirt: "cream", skin: 1, hat: "cap" }); // vendor
use("Vendor"); // vendor sits behind case: redraw case on People to overlap
parasol(418, 196, 34, "blue.2", "cream.2");
// stools + eaters
stool(466, 262, "red"); stool(494, 262, "blue"); use("People"); R(458, 252, 504, 256, "wood.3"); L(458, 252, 504, 252, "wood.4"); L(460, 256, 460, 266, "wood.1"); L(502, 256, 502, 266, "wood.1");
seated(468, 232, { shirt: "blue", skin: 0 }); seated(496, 232, { shirt: "green", skin: 2, hijab: "orange" });
use("People"); R(464, 254, 472, 256, "cream.2"); R(490, 254, 498, 256, "cream.2");
// steam
use("Overlay"); for (let k = 0; k < 3; k++) { const sx = 404 + k * 14; for (let t = 0; t < 14; t++) PT([[sx + Math.round(Math.sin(t / 2 + k) * 2), 230 - t]], t % 3 ? "cream.2" : "conc.4"); }

// ---------- more food carts in the alley ----------
// sate cart (grill) at left of alley
use("Shade"); E(388, 344, 448, 352, "conc.1");
use("People");
R(392, 314, 442, 332, "wood.2"); R(392, 314, 442, 317, "wood.3"); R(392, 314, 442, 332, "wood.0", false);
R(396, 306, 438, 314, "conc.0"); for (let k = 0; k < 9; k++) { L(306 + k * 4 + 92, 241 + 64, 306 + k * 4 + 92, 248 + 64, "wood.4"); PT([[306 + k * 4 + 92, 240 + 64], [306 + k * 4 + 92, 244 + 64]], "meat.1"); } // skewers on grill
for (let k = 0; k < 14; k++) PT([[306 + k * 3 + 92, 249 + 64]], k % 2 ? "red.2" : "orange.2"); // embers
R(392, 332, 442, 334, "conc.0"); E(396, 332, 406, 342, "conc.0"); E(398, 334, 404, 340, "steel.1"); E(428, 332, 438, 342, "conc.0"); E(430, 334, 436, 340, "steel.1");
R(400, 320, 434, 328, "red.1"); T("SATE", 417, 321, "cream.2", true);
bust(417, 272, { shirt: "orange", skin: 2, hat: "topi" });
use("Vendor"); L(430, 296, 440, 302, "cream.1"); E(438, 300, 444, 304, "orange.1"); // fan
use("Overlay"); for (let k = 0; k < 3; k++) { const sx = 312 + k * 14 + 92; for (let t = 0; t < 22; t++) PT([[sx + Math.round(Math.sin(t / 2.5 + k) * 3), 238 + 64 - t]], t % 3 ? "conc.3" : "conc.2"); }
// es teh cart + parasol on the right of the alley
use("Shade"); E(512, 280, 566, 288, "conc.1");
use("People");
R(516, 252, 560, 270, "blue.1"); R(516, 252, 560, 255, "blue.2"); R(516, 252, 560, 270, "blue.0", false);
R(522, 238, 554, 252, "steel.1"); R(523, 239, 553, 251, "water.2"); for (let k = 0; k < 5; k++) { E(526 + k * 5, 242, 530 + k * 5, 250, pick(["orange.2", "red.2", "cream.2"])); }
R(516, 270, 560, 272, "conc.0"); E(520, 270, 530, 280, "conc.0"); E(522, 272, 528, 278, "steel.1"); E(546, 270, 556, 280, "conc.0"); E(548, 272, 554, 278, "steel.1");
R(522, 258, 554, 266, "blue.0"); T("ES TEH", 538, 259, "cream.2", true);
bust(538, 204, { shirt: "red", skin: 1, hat: "hijab", hijab: "orange" });
parasol(538, 198, 30, "orange.2", "cream.2");
// plastic chairs + little table by the sate cart
use("People");


// ---------- CUSTOMERS & PASSERS-BY ----------
const P0 = (x, y, o) => ({ x, y, o });
const crowd = [
  // lane 1 (row A) facing the stalls
  P0(46, 204, { face: "back", shirt: "blue", hat: "hijab", hijab: "red", skin: 1, carry: "basket" }), P0(92, 209, { face: "back", shirt: "orange", skin: 0, pants: "conc.1" }),
  P0(168, 206, { face: "back", shirt: "green", skin: 2, carry: "bag" }), P0(206, 203, { face: "back", shirt: "purple", hat: "hijab", hijab: "teal", skin: 0, carry: "bag" }),
  P0(268, 205, { face: "back", shirt: "cream", skin: 3, hat: "topi" }), P0(300, 209, { face: "back", shirt: "red", hat: "hijab", hijab: "blue", skin: 2, carry: "basket" }),
  P0(608, 205, { face: "back", shirt: "red", hat: "topi", skin: 1, carry: "sack" }), P0(650, 208, { face: "back", shirt: "teal", hat: "hijab", hijab: "green", skin: 2 }),
  P0(730, 204, { face: "back", shirt: "teal", skin: 3 }), P0(772, 209, { face: "back", shirt: "cream", hat: "hijab", hijab: "green", skin: 2, carry: "basket" }),
  P0(852, 205, { face: "back", shirt: "orange", skin: 1, hat: "cap", hatc: "blue" }), P0(892, 208, { face: "back", shirt: "purple", skin: 0, carry: "bag" }),
  // lane 1 walkers
  P0(372, 200, { face: "right", shirt: "red", skin: 0, carry: "bag" }), P0(586, 198, { face: "left", shirt: "green", hat: "caping", skin: 1, carry: "sack" }),
  P0(238, 214, { face: "left", shirt: "cream", hat: "hijab", hijab: "purple", skin: 2, carry: "basket" }), P0(930, 202, { face: "left", shirt: "blue", skin: 3 }),
  P0(470, 196, { face: "right", shirt: "orange", skin: 2, hat: "cap", hatc: "teal", carry: "bag" }), P0(420, 214, { face: "left", shirt: "teal", skin: 0, hat: "hijab", hijab: "red" }),
  // alley eaters/buyers
  P0(412, 358, { face: "back", shirt: "blue", skin: 1 }), P0(444, 360, { face: "back", shirt: "green", skin: 2, hat: "hijab", hijab: "orange", carry: "bag" }),
  P0(530, 254, { face: "back", shirt: "purple", skin: 0 }),
  // lane 2 (row B) facing the stalls
  P0(46, 336, { face: "back", shirt: "purple", skin: 2 }), P0(78, 340, { face: "back", shirt: "teal", hat: "hijab", hijab: "orange", skin: 1, carry: "bag" }), P0(112, 335, { face: "back", shirt: "red", skin: 3 }),
  P0(150, 338, { face: "back", shirt: "orange", skin: 0, hat: "topi" }), P0(190, 334, { face: "back", shirt: "cream", skin: 1, hat: "hijab", hijab: "blue", carry: "bag" }),
  P0(270, 336, { face: "back", shirt: "red", skin: 3, carry: "bag" }), P0(312, 340, { face: "back", shirt: "green", hat: "hijab", hijab: "purple", skin: 2 }),
  P0(604, 338, { face: "back", shirt: "blue", hat: "hijab", hijab: "red", skin: 0, carry: "basket" }), P0(642, 334, { face: "back", shirt: "orange", skin: 1, hat: "cap", hatc: "red" }),
  P0(716, 336, { face: "back", shirt: "green", skin: 2 }), P0(756, 340, { face: "back", shirt: "teal", hat: "hijab", hijab: "orange", skin: 1, carry: "bag" }),
  P0(834, 338, { face: "back", shirt: "cream", skin: 1, hat: "cap", hatc: "red" }), P0(874, 334, { face: "back", shirt: "red", skin: 0, hat: "hijab", hijab: "blue", carry: "bag" }),
  // lane 2 walkers
  P0(396, 330, { face: "right", shirt: "teal", skin: 1 }), P0(556, 330, { face: "left", shirt: "purple", skin: 2, carry: "sack" }), P0(930, 332, { face: "left", shirt: "orange", skin: 2, hat: "cap", hatc: "green" }),
  P0(446, 318, { face: "right", shirt: "red", skin: 3, carry: "basket" }), P0(500, 324, { face: "left", shirt: "blue", skin: 0, hat: "hijab", hijab: "green" }),
  // kids
  P0(184, 209, { kid: true, shirt: "red", balloon: "orange" }), P0(330, 207, { kid: true, shirt: "blue" }), P0(640, 342, { kid: true, shirt: "green", balloon: "blue" }), P0(484, 262, { kid: true, shirt: "orange", balloon: "red" }), P0(428, 300, { kid: true, shirt: "purple" }),
  // workers
  P0(378, 396, { face: "left", shirt: "teal", skin: 1, carry: "sack", hat: "cap", hatc: "red" }), P0(60, 386, { face: "front", shirt: "orange", skin: 2, hat: "cap", hatc: "orange", pants: "conc.1" }),
];
crowd.sort((a, b) => a.y - b.y).forEach((c) => (c.o.kid ? child(c.x, c.y, c.o) : person(c.x, c.y, c.o)));
// woman carrying a bakul on her head
use("People"); E(500 - 7, 301, 500 + 7, 306, "wood.3"); E(500 - 6, 299, 500 + 6, 303, "wood.1"); PT([[496, 299], [499, 298], [503, 299]], "green.2"); PT([[498, 298], [501, 297]], "red.2");

// ---------- lapak lesehan (vendors sit on the ground on plastic mats) ----------
function lesehan(x, y, mat, kind, o) {
  use("Shade"); E(x - 4, y + 22, x + 78, y + 32, "conc.1");
  use("Counter");
  P([[x + 6, y + 6], [x + 70, y + 6], [x + 78, y + 28], [x - 2, y + 28]], rp(mat, 1)); P([[x + 6, y + 6], [x + 70, y + 6], [x + 72, y + 12], [x + 4, y + 12]], rp(mat, 2)); L(x - 2, y + 28, x + 78, y + 28, rp(mat, 0));
  for (let k = 0; k < 6; k++) L(x + 10 + k * 11, y + 7, x + 4 + k * 14, y + 28, rp(mat, 0));
  if (kind === "sayur") { mound(x + 16, y + 26, 22, 12, [["green.2", "cream.2", "green.1"], ["green.1", "green.2", "green.0"]], 4); mound(x + 40, y + 26, 22, 12, TOM[0] ? [["red.2", "cream.2", "red.0"], ["orange.1", "orange.2", "orange.0"]] : [], 4); mound(x + 62, y + 26, 20, 10, [["purple.1", "purple.2", "purple.0"], ["orange.1", "orange.2", "orange.0"]], 4); }
  if (kind === "ikan") { for (let k = 0; k < 4; k++) { R(x + 8 + k * 17, y + 14, x + 20 + k * 17, y + 24, "cream.1"); R(x + 8 + k * 17, y + 14, x + 20 + k * 17, y + 24, "conc.1", false); for (let u = 0; u < 3; u++) FISH(x + 9 + k * 17, y + 16 + u * 3, 8, [["cream.2", "cream.0", "cream.2"], ["orange.1", "orange.0", "orange.2"]][u % 2]); } }
  use("Vendor");
  const sk = ["skin.1", "skin.2", "skin.3"][o.skin];
  // seated cross-legged: sarong skirt, torso, head
  P([[x + 26, y + 12], [x + 48, y + 12], [x + 54, y + 26], [x + 20, y + 26]], rp(o.shirt, 0)); P([[x + 26, y + 12], [x + 36, y + 12], [x + 30, y + 26], [x + 20, y + 26]], rp(o.shirt, 1)); L(x + 22, y + 26, x + 52, y + 26, rp(o.shirt, 0));
  R(x + 29, y - 2, x + 45, y + 13, rp("cream", 1)); R(x + 29, y - 2, x + 31, y + 13, rp("cream", 2)); R(x + 43, y - 2, x + 45, y + 13, rp("cream", 0));
  R(x + 26, y, x + 28, y + 10, rp("cream", 1)); R(x + 46, y, x + 48, y + 10, rp("cream", 0)); PT([[x + 26, y + 11], [x + 27, y + 11], [x + 47, y + 11], [x + 48, y + 11]], sk);
  E(x + 33, y - 12, x + 41, y - 3, sk); PT([[x + 34, y - 9]], "skin.3"); PT([[x + 35, y - 8], [x + 39, y - 8]], "conc.0");
  if (o.hat === "caping") { P([[x + 26, y - 7], [x + 37, y - 17], [x + 48, y - 7]], "orange.2"); L(x + 26, y - 7, x + 48, y - 7, "orange.0"); L(x + 31, y - 11, x + 36, y - 15, "orange.1"); }
  else { E(x + 31, y - 14, x + 43, y - 2, rp(o.hijab, 1)); E(x + 33, y - 12, x + 41, y - 4, sk); PT([[x + 35, y - 8], [x + 39, y - 8]], "conc.0"); }
  use("Counter"); bakul(x + 6, y + 27, 14, 8, [["orange.1", "orange.2", "orange.0"]]); // bakul beside
}
lesehan(396, 104, "blue", "sayur", { shirt: "red", skin: 0, hat: "caping" });
lesehan(486, 112, "orange", "ikan", { shirt: "teal", skin: 1, hat: "hijab", hijab: "purple" });
person(430, 166, { face: "back", shirt: "green", skin: 2, hat: "hijab", hijab: "red", carry: "basket" }); person(520, 170, { face: "back", shirt: "blue", skin: 3, hat: "topi" });

// bargaining bubbles
bubble(138, 192, "20RB?");
bubble(706, 190, "BELI 2?"); bubble(578, 322, "MURAH");
// price tags on counters
use("Counter");
const tag = (x, y, t) => { R(x, y, x + g.textW(t) + 3, y + 6, "cream.2"); R(x, y, x + g.textW(t) + 3, y + 6, "conc.1", false); T(t, x + 2, y + 1, "red.0"); };
[[20, 160, "5RB"], [64, 160, "10RB"], [150, 160, "3RB"], [190, 160, "8RB"], [270, 160, "2RB"], [584, 160, "30RB"], [640, 160, "15RB"], [700, 160, "90RB"], [744, 160, "70RB"], [830, 160, "5RB"],
 [20, 292, "50RB"], [60, 292, "75RB"], [144, 292, "40RB"], [186, 292, "25RB"], [262, 292, "20RB"], [590, 292, "6RB"], [640, 292, "10RB"], [706, 292, "4RB"], [750, 292, "12RB"], [830, 292, "15RB"]].forEach(([x, y, t]) => tag(x, y, t));
// hanging scale at sayur and ikan stalls
use("Tarp");
for (const [x, y] of [[190, 118], [180, 250]]) { L(x, y - 20, x, y - 10, "conc.0"); E(x - 5, y - 10, x + 5, y, "steel.2"); E(x - 5, y - 10, x + 5, y, "conc.0", false); PT([[x, y - 9], [x, y - 8]], "red.1"); L(x, y - 5, x + 2, y - 7, "conc.0"); L(x, y, x, y + 4, "conc.0"); E(x - 6, y + 4, x + 6, y + 8, "steel.1"); }
// ---------- VEHICLES ----------
bike(70, 392, "red", 1); bike(112, 394, "blue", 1); bike(154, 391, "green", -1); bike(740, 392, "orange", -1); bike(784, 394, "teal", 1);
// handcart
use("Shade"); E(296, 392, 366, 399, "conc.1");
use("People");
R(300, 374, 358, 388, "wood.2"); R(300, 374, 358, 377, "wood.3"); R(300, 374, 358, 388, "wood.0", false); L(358, 380, 372, 372, "wood.2"); L(358, 384, 372, 372, "wood.1");
for (let k = 0; k < 5; k++) E(302 + k * 11, 364, 312 + k * 11, 376, pick(["green.1", "green.2", "orange.1", "red.1"]));
E(312, 380, 326, 394, "conc.0"); E(315, 383, 323, 391, "steel.1"); E(338, 380, 352, 394, "conc.0"); E(341, 383, 349, 391, "steel.1");
// pickup truck unloading (bottom right)
use("Shade"); E(590, 390, 740, 399, "conc.1");
use("People");
R(596, 360, 700, 386, "red.1"); R(596, 360, 700, 363, "red.2"); R(596, 383, 700, 386, "red.0"); // bak
for (let k = 0; k < 7; k++) L(602 + k * 15, 363, 602 + k * 15, 383, "red.0");
R(700, 352, 736, 386, "cream.1"); P([[700, 352], [724, 352], [736, 366], [736, 372], [700, 372]], "cream.1"); P([[704, 356], [722, 356], [730, 366], [704, 366]], "water.2"); L(704, 356, 722, 356, "cream.2"); R(700, 372, 736, 386, "cream.0"); R(734, 376, 738, 382, "orange.2");
E(612, 376, 636, 398, "conc.0"); E(618, 382, 630, 392, "steel.1"); E(704, 376, 728, 398, "conc.0"); E(710, 382, 722, 392, "steel.1");
for (let k = 0; k < 6; k++) { const sx = 600 + k * 16; E(sx, 346, sx + 16, 362, k % 2 ? "cream.1" : "cream.2"); E(sx + 1, 347, sx + 15, 360, k % 2 ? "cream.2" : "cream.1"); }
for (let k = 0; k < 4; k++) E(606 + k * 22, 338, 622 + k * 22, 354, pick(["green.2", "green.1"]));
person(764, 396, { face: "left", shirt: "orange", skin: 1, carry: "sack", hat: "cap", hatc: "teal" }); person(578, 392, { face: "right", shirt: "blue", skin: 0, carry: "sack" });
// floor goods & debris
use("Counter");
crate(10, 232); crate(24, 232, "wood.1"); crate(17, 224); crate(370, 314); crate(384, 316, "wood.1"); crate(930, 314); crate(930, 304);
// cat on crate, chickens, pigeons
use("People");
E(25, 214, 35, 220, "orange.1"); E(31, 210, 37, 215, "orange.1"); PT([[32, 209], [36, 209]], "orange.1"); PT([[33, 212], [35, 212]], "outline"); L(25, 218, 22, 216, "orange.1"); PT([[28, 217], [30, 215]], "orange.0");
chicken(592, 236, ["orange.1", "cream.2"], 1); chicken(604, 244, ["cream.2", "orange.1"], -1); chicken(580, 248, ["red.1", "orange.2"], 1);
for (const [x, y] of [[444, 332], [452, 336], [520, 306]]) { E(x, y, x + 6, y + 4, "steel.1"); PT([[x + 5, y]], "steel.0"); PT([[x + 6, y + 1]], "orange.1"); L(x + 1, y + 2, x + 3, y + 2, "steel.2"); }

// ---------- floor litter ----------
use("Counter");
seed(33);
for (let i = 0; i < 90; i++) { const x = ri(4, 950), y = ri(188, 396); const c = pick(["green.1", "green.2", "green.1", "orange.1", "cream.1", "red.1"]); PT([[x, y], [x + 1, y], [x, y + 1]], c); }
for (const [x, y] of [[190, 188], [330, 205], [610, 195], [150, 330], [700, 326], [860, 340]]) { E(x, y, x + 7, y + 3, "cream.2"); PT([[x + 3, y + 1]], "blue.2"); } // plastic bags

// ---------- overhead wires + hanging bulbs ----------
use("Overlay");
function wire(x1, y1, x2, y2, sag) { let px = null; for (let t = 0; t <= 1.0001; t += 0.02) { const x = x1 + (x2 - x1) * t, y = y1 + (y2 - y1) * t + sag * 4 * t * (1 - t); if (px) L(px[0], px[1], Math.round(x), Math.round(y), "conc.0"); px = [Math.round(x), Math.round(y)]; } }
wire(0, 6, 300, 30, 10); wire(300, 30, 640, 8, 14); wire(640, 8, 960, 26, 10); wire(0, 20, 480, 44, 16); wire(480, 44, 960, 14, 12); wire(120, 56, 360, 62, 8); wire(560, 60, 860, 58, 9);
for (const [x, y] of [[300, 30], [640, 8], [480, 44]]) { R(x - 1, y - 2, x + 1, y + 6, "wood.1"); R(x - 4, y, x + 4, y + 1, "wood.1"); }
// hanging bulb strings over alleys
for (const [x1, x2, y] of [[370, 570, 178], [6, 360, 186], [572, 950, 186]]) { let px = null; for (let t = 0; t <= 1.0001; t += 0.05) { const x = x1 + (x2 - x1) * t, yy = y + 8 * 4 * t * (1 - t); if (px) L(px[0], px[1], Math.round(x), Math.round(yy), "conc.0"); if (t > 0.04 && t < 0.97) { E(Math.round(x) - 1, Math.round(yy) + 1, Math.round(x) + 1, Math.round(yy) + 4, "orange.2"); PT([[Math.round(x), Math.round(yy) + 2]], "cream.2"); } px = [Math.round(x), Math.round(yy)]; } }

if (require.main === module) { g.png(g.compose(), "scene3.png", 2); }
