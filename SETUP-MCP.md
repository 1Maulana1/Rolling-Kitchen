# Setup MCP for Unity (Claude Code ↔ Unity Editor)

MCP for Unity membuat Claude Code bisa membaca console, membuka scene, menjalankan menu, dan Play
di Unity Editor. Ada dua bagian: **paket di Unity** (sudah tercantum di proyek) dan **server MCP**
yang dijalankan Claude Code (sudah diatur di `.mcp.json`). Kamu cukup memasang alatnya.

## 1. Pasang alat (sekali saja)

| Alat | Keterangan |
|---|---|
| Unity **6000.6.3f1** | Lewat Unity Hub. Versi lain bisa memicu upgrade proyek; jangan dipakai. |
| **Git** | Dibutuhkan Unity untuk mengunduh paket MCP dari GitHub. https://git-scm.com |
| **uv** | Menjalankan server MCP. Pasang di PowerShell (lihat di bawah). |
| **Claude Code** | https://claude.com/claude-code |

Pasang uv di PowerShell, lalu **tutup dan buka lagi** terminal agar `uvx` dikenali:

```powershell
powershell -ExecutionPolicy ByPass -c "irm https://astral.sh/uv/install.ps1 | iex"
```

Cek: `uvx --version` harus menampilkan nomor versi.

## 2. Buka proyek di Unity

1. `git clone https://github.com/1Maulana1/Rolling-Kitchen.git`
2. Buka foldernya lewat Unity Hub (**Add project from disk**).
3. Tunggu Unity mengunduh paket. Paket **MCP for Unity v10.3.0** ikut terpasang otomatis
   dari `Packages/manifest.json`.
4. Di Unity, buka panel **MCP for Unity** dari menu **Window** dan pastikan bridge-nya berjalan
   (status terhubung / *running*).

## 3. Jalankan Claude Code

Buka terminal **di folder proyek** (folder yang berisi `.mcp.json`), lalu:

```powershell
claude
```

Saat pertama kali, Claude Code bertanya apakah server `UnityMCP` dari `.mcp.json` boleh dipakai:
pilih **setuju**. Server diunduh otomatis pada pemakaian pertama (perlu internet).

Cek sambungan (Unity Editor harus sedang terbuka):

```powershell
claude mcp list
```

`UnityMCP` harus berstatus terhubung.

## Kalau bermasalah

| Gejala | Penyebab / solusi |
|---|---|
| `uvx` tidak dikenali | Terminal belum dibuka ulang setelah memasang uv. |
| Unity error saat mengunduh `com.coplaydev.unity-mcp` | Git belum terpasang, atau tidak ada internet. Pasang Git lalu buka ulang Unity. |
| `UnityMCP` gagal terhubung | Unity Editor belum dibuka, atau bridge di panel MCP for Unity belum berjalan. |
| Tidak pernah ditanya soal `UnityMCP` | Claude Code dijalankan di luar folder proyek. Jalankan `claude` dari folder yang berisi `.mcp.json`. |

## Catatan untuk tim

- Versi paket (`Packages/manifest.json`, tag `v10.3.0`) dan server (`.mcp.json`,
  `mcpforunityserver==10.3.0`) **harus sama**. Kalau mau naik versi, ubah keduanya sekaligus.
- Jangan commit folder `Packages/com.coplaydev.unity-mcp/` (sudah ada di `.gitignore`):
  salinan lokal itu akan menimpa versi dari GitHub di komputer orang lain.
- Scene dibangun ulang lewat menu **Rolling Kitchen** di Unity (Build Title, Build Rumah,
  Build Pasar UI, Build Food Truck, Build Rekap). Mainkan dari scene **TitleScreen**.
