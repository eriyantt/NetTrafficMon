# NetTrafficMon - Realtime Windows Taskbar Traffic Monitor

Aplikasi Windows monitor trafik jaringan realtime (kecepatan Upload dan Download) yang dirancang khusus untuk berjalan di Taskbar Windows dan System Tray. Sangat ringan (~58 KB), hemat daya (CPU ~0%), dan tidak memerlukan instalasi driver pihak ketiga.

---

## Fitur Utama

- **Realtime Upload & Download Tracking**:
  - Mengukur kecepatan download dan upload jaringan secara presisi per detik (atau interval kustom).
  - Menghitung total data terkirim & diterima selama sesi aktif.
  - Mencatat kecepatan puncak (*Peak Speed*) untuk upload dan download.
- **Integrasi Taskbar Windows (Windows 10 & 11)**:
  - **Dock to Taskbar**: Menempel otomatis di area taskbar (di sebelah kiri System Tray / jam).
  - **Floating Mode**: Bisa digeser (*drag & drop*) ke posisi mana pun di layar atau monitor kedua.
  - **Position Memory**: Mengingat posisi terakhir saat aplikasi dibuka kembali.
- **3 Mode Tampilan Widget**:
  1. **Compact 2-Line (Stacked)**: Tampilan dua baris rapi (`▲ Upload` di atas, `▼ Download` di bawah). Cocok untuk taskbar standar.
  2. **Horizontal Single Line**: Tampilan satu baris memanjang (`▲ Upload   ▼ Download`).
  3. **With Mini Graph (Sparkline)**: Tampilan dua baris disertai grafik histogram mini realtime di sisi kanan.
- **System Tray (Notification Area)**:
  - Ikon khusus di system tray dengan *Dynamic Tooltip* yang menampilkan kecepatan saat kursor diarahkan ke ikon.
  - Menu konteks klik-kanan lengkap untuk pengaturan instan tanpa membuka jendela besar.
- **Jendela Grafik & Statistik Lengkap (Network Details)**:
  - Grafik kurva realtime 60 detik terakhir (Download: Hijau, Upload: Oranye) dengan indikator skala kecepatan.
  - Informasi detail kartu jaringan: Nama adapter, Deskripsi, IPv4 Address, MAC Address, Link Speed, dan Uptime sesi.
- **Pilihan Adapter Jaringan**:
  - **All Active Adapters (Combined)**: Menggabungkan seluruh interface fisik yang aktif.
  - Atau pilih kartu jaringan spesifik (misal: hanya Wi-Fi, Ethernet, atau VPN).
- **Kustomisasi Penuh**:
  - Pilihan satuan kecepatan: **Bytes/s** (`KB/s`, `MB/s`) atau **Bits/s** (`Kbps`, `Mbps`).
  - Tema visual: **Dark Fluent** (sesuai Taskbar Windows 10/11), **Light**, atau **Transparent**.
  - Pengatur transparansi widget (Opacity: 30% - 100%).
  - Pemilih warna aksen kustom untuk panah dan teks Upload/Download.
  - Pilihan interval refresh: 500 ms (Fast), 1000 ms (Normal), 2000 ms (Power Saving).
  - Pilihan presisi angka desimal (0, 1, atau 2).
- **Startup Otomatis**:
  - Opsi *Launch on Windows Startup* untuk berjalan otomatis saat komputer dinyalakan.
- **Single Instance**:
  - Mencegah aplikasi terbuka dua kali secara bersamaan.

---

## Struktur Folder

```text
netmon/
├── NetTrafficMon.exe    # File aplikasi utama (siap pakai, 58 KB)
├── start.bat            # Script instan untuk menjalankan aplikasi
├── build.ps1            # Script build otomatis dengan csc.exe bawaan Windows
├── app.ico              # File icon multi-resolusi aplikasi
├── src/
│   ├── Program.cs       # Entry point, single-instance mutex & DPI awareness
│   ├── TaskbarWidget.cs # Jendela overlay widget taskbar (GDI+ rendering)
│   ├── TrafficEngine.cs # Engine pembaca trafik jaringan & kalkulasi kecepatan
│   ├── TrafficData.cs   # Model data, formatter kecepatan/bytes, & color helpers
│   ├── TrayIconManager.cs # Ikon system tray & menu konteks klik kanan
│   ├── DetailsForm.cs   # Jendela statistik & grafik realtime 60 detik
│   ├── SettingsForm.cs  # Jendela konfigurasi & kustomisasi
│   ├── Settings.cs      # Penyimpanan pengaturan (settings.json) & registry startup
│   └── NativeMethods.cs # Win32 API interop (docking, dragging, styling)
└── README.md
```

---

## Cara Menjalankan

1. **Jalankan Langsung**:
   - Cukup klik dua kali file **`NetTrafficMon.exe`** atau **`start.bat`**.
   - Widget akan langsung muncul di taskbar Anda dan ikon akan muncul di System Tray.

2. **Kompilasi Ulang (Jika Mengubah Kode Sumber)**:
   - Jika Anda mengubah kode di dalam folder `src/`, cukup jalankan perintah di PowerShell:
     ```powershell
     powershell -ExecutionPolicy Bypass -File .\build.ps1
     ```
   - Aplikasi akan otomatis dikompilasi ulang menjadi `NetTrafficMon.exe`.

---

## Panduan Penggunaan & Kontrol

| Aksi | Fungsi |
| :--- | :--- |
| **Klik Kiri + Geser (Drag)** | Menggeser posisi widget ke mana saja di taskbar atau layar. |
| **Klik Kanan (Widget / Tray)** | Membuka menu konteks (Pilih Adapter, Ubah Layout, Satuan, Kunci Posisi, Pengaturan, Keluar). |
| **Klik Dua Kali (Double Click)** | Membuka jendela **Network Details & Live Graph** (grafik 60 detik & info adapter). |
| **Arahkan Kursor (Hover)** | Menampilkan tooltip ringkasan adapter, IP, kecepatan, dan total data terpakai. |

---

## Persyaratan Sistem

- Windows 10 atau Windows 11 (64-bit / 32-bit).
- Built-in Microsoft .NET Framework 4.0 atau lebih baru (sudah otomatis terpasang di semua Windows 10 dan 11).
- Tidak memerlukan hak Administrator untuk membaca trafik jaringan.
