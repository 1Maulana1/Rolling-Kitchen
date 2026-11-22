// Generator sprite koki perempuan (pemain) 16x24 px, 4 arah x 3 frame (diam, langkah kiri, langkah kanan).
// Baris lembar: 0 = bawah, 1 = kiri, 2 = kanan, 3 = atas. Garis luar ditambahkan otomatis.
// Jalankan: node koki.js  -> ../../Assets/Art/Characters/koki.png (+ pratinjau 6x di folder ini)
const fs = require("fs"), zlib = require("zlib"), path = require("path");

const FW = 16, FH = 24, COLS = 3, ROWS = 4;
const C = {
  o: "#1d1922", w: "#f6f3ec", W: "#cfc7b8", h: "#5b3426", H: "#3d2119", s: "#f2c49c", S: "#d89a72",
  e: "#1d1922", r: "#c8402f", R: "#93291f", c: "#f6f3ec", k: "#cdc5b6", b: "#8a8277",
  p: "#3b3548", P: "#28232f", f: "#2a1f1f", m: "#c9776a",
};
const hex = (h) => [parseInt(h.slice(1, 3), 16), parseInt(h.slice(3, 5), 16), parseInt(h.slice(5, 7), 16)];

function frame(dir, step) {
  const g = Array.from({ length: FH }, () => Array(FW).fill(null));
  const R = (x1, y1, x2, y2, c) => { for (let y = y1; y <= y2; y++) for (let x = x1; x <= x2; x++) if (x >= 0 && x < FW && y >= 0 && y < FH) g[y][x] = c; };
  const side = dir === 1 || dir === 2; // digambar menghadap kiri; kanan = cermin di akhir
  const back = dir === 3;
  const bob = step === 0 ? 0 : 1; // badan turun 1 px saat melangkah

  // Kaki + sepatu
  if (!side) {
    const lUp = step === 1 ? 1 : 0, rUp = step === 2 ? 1 : 0;
    R(5, 18, 6, 21 - lUp, "p"); R(5, 22 - lUp, 6, 22 - lUp, "f");
    R(9, 18, 10, 21 - rUp, "p"); R(9, 22 - rUp, 10, 22 - rUp, "f");
    R(10, 18, 10, 21 - rUp, "P");
  } else {
    if (step === 0) { R(6, 18, 9, 21, "p"); R(8, 18, 9, 21, "P"); R(5, 22, 9, 22, "f"); }
    else { // kaki depan ke kiri, kaki belakang ke kanan
      const a = step === 1 ? 0 : 1;
      R(4 + a, 18, 6 + a, 21, "p"); R(3 + a, 22, 6 + a, 22, "f");
      R(8 - a, 18, 10 - a, 20, "P"); R(8 - a, 21, 10 - a, 21, "f");
    }
  }

  // Badan (jas koki) + celana atas
  const y0 = 11 + bob;
  if (!side) {
    R(4, y0, 11, 17, "c"); R(11, y0, 11, 17, "k"); R(4, 17, 11, 17, "k");
    if (!back) { R(6, y0, 9, y0, "r"); R(7, y0 + 1, 8, y0 + 1, "R"); g[y0 + 3][7] = "b"; g[y0 + 5][8] = "b"; }
    // Lengan berayun
    const la = step === 1 ? -1 : step === 2 ? 1 : 0;
    R(3, y0 + 1 + Math.max(0, la), 3, 15 + la, "k"); g[16 + la][3] = "s";
    R(12, y0 + 1 + Math.max(0, -la), 12, 15 - la, "k"); g[16 - la][12] = "s";
  } else {
    R(5, y0, 10, 17, "c"); R(10, y0, 10, 17, "k"); R(5, 17, 10, 17, "k");
    R(5, y0, 6, y0, "r");
    const sw = step === 1 ? -1 : step === 2 ? 1 : 0; // lengan depan berayun
    R(7 + sw, y0 + 1, 8 + sw, 15, "k"); R(7 + sw, 16, 8 + sw, 16, "s");
  }

  // Kepala
  const hy = 6 + bob;
  if (!side) {
    R(4, hy, 11, hy + 4, back ? "h" : "s");
    if (!back) {
      R(4, hy, 11, hy, "h"); R(4, hy, 4, hy + 3, "h"); R(11, hy, 11, hy + 3, "h"); R(5, hy + 1, 6, hy + 1, "h");
      g[hy + 2][6] = "e"; g[hy + 2][9] = "e"; g[hy + 3][5] = "m"; g[hy + 3][10] = "m";
      R(11, hy + 1, 11, hy + 4, "S");
    } else {
      R(4, hy + 4, 11, hy + 4, "H"); R(7, hy + 4, 8, hy + 6, "h"); R(8, hy + 5, 8, hy + 6, "H"); // ekor rambut
    }
  } else {
    R(4, hy, 10, hy + 4, "s");
    R(7, hy, 10, hy + 4, "h"); R(4, hy, 6, hy, "h"); R(10, hy + 1, 10, hy + 4, "H");
    R(10, hy + 2, 11, hy + 5, "h"); R(11, hy + 4, 11, hy + 6, "H"); // ekor rambut
    g[hy + 2][5] = "e"; g[hy + 3][4] = "m";
  }

  // Topi koki
  const ty = bob;
  R(5, ty + 1, 10, ty + 4, "w"); R(6, ty, 9, ty, "w"); R(10, ty + 1, 10, ty + 4, "W");
  R(4, ty + 5, 11, ty + 5, "W");

  // Garis luar otomatis
  const out = g.map((r) => r.slice());
  for (let y = 0; y < FH; y++) for (let x = 0; x < FW; x++) {
    if (g[y][x]) continue;
    const n = [[1, 0], [-1, 0], [0, 1], [0, -1]].some(([dx, dy]) => g[y + dy] && g[y + dy][x + dx]);
    if (n) out[y][x] = "o";
  }
  if (dir === 2) return out.map((r) => r.slice().reverse());
  return out;
}

// Lembar: dir 0 bawah, 1 kiri, 2 kanan (cermin kiri), 3 atas.
const SW = FW * COLS, SH = FH * ROWS;
const px = Array.from({ length: SH }, () => Array(SW).fill(null));
for (let d = 0; d < ROWS; d++) for (let s = 0; s < COLS; s++) {
  const f = frame(d === 2 ? 2 : d, s);
  for (let y = 0; y < FH; y++) for (let x = 0; x < FW; x++) px[d * FH + y][s * FW + x] = f[y][x];
}

function crc32(buf) {
  let c, crc = 0xffffffff;
  for (let n = 0; n < buf.length; n++) {
    c = (crc ^ buf[n]) & 0xff;
    for (let k = 0; k < 8; k++) c = c & 1 ? 0xedb88320 ^ (c >>> 1) : c >>> 1;
    crc = (crc >>> 8) ^ c;
  }
  return (crc ^ 0xffffffff) >>> 0;
}
function chunk(type, data) {
  const len = Buffer.alloc(4); len.writeUInt32BE(data.length);
  const td = Buffer.concat([Buffer.from(type), data]);
  const crc = Buffer.alloc(4); crc.writeUInt32BE(crc32(td));
  return Buffer.concat([len, td, crc]);
}
function writePng(file, grid, scale) {
  const w = grid[0].length * scale, h = grid.length * scale;
  const raw = Buffer.alloc((w * 4 + 1) * h);
  for (let y = 0; y < h; y++) {
    raw[y * (w * 4 + 1)] = 0;
    for (let x = 0; x < w; x++) {
      const c = grid[Math.floor(y / scale)][Math.floor(x / scale)];
      const i = y * (w * 4 + 1) + 1 + x * 4;
      if (c) { const [r, g, b] = hex(C[c]); raw[i] = r; raw[i + 1] = g; raw[i + 2] = b; raw[i + 3] = 255; }
    }
  }
  const ihdr = Buffer.alloc(13);
  ihdr.writeUInt32BE(w, 0); ihdr.writeUInt32BE(h, 4); ihdr[8] = 8; ihdr[9] = 6;
  fs.mkdirSync(path.dirname(file), { recursive: true });
  fs.writeFileSync(file, Buffer.concat([Buffer.from([137, 80, 78, 71, 13, 10, 26, 10]), chunk("IHDR", ihdr),
    chunk("IDAT", zlib.deflateSync(raw)), chunk("IEND", Buffer.alloc(0))]));
}

writePng(path.join(__dirname, "../../Assets/Art/Characters/koki.png"), px, 1);
writePng(path.join(__dirname, "koki-pratinjau.png"), px, 6);
console.log("koki.png", SW, "x", SH);
