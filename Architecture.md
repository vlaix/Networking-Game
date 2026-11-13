# Project Context & Architecture Guide: Together We Are One

Dokumen ini merangkum keseluruhan arsitektur, struktur kode, skema database, kontrak API, dan konvensi teknis proyek **Together We Are One** (Kelompok 08 - Mata Kuliah Pemrograman Game Jaringan) sebagai konteks utama untuk pengembangan lanjutan di IDE Google Antigravity.

---

## 1. Ringkasan Proyek & Tech Stack

* **Judul Game:** Together We Are One
* **Genre:** 3D / 2.5D Co-op Puzzle Platformer (**Co-op Team Speedrun** — gaya *Pico Park*)
* **Anggota Tim:**
  * Aisyah Dwi Slamet Rahayu (5224600066)
  * Reyvaldi Tri Hanggara Putra / Valdi (5224600074)
  * Yorianmar Rahagi (5224600085)
* **Game Engine & Networking:** Unity `2022.3.62f3` + **Mirror Networking**
* **Backend Server:** Node.js + Express.js (`v5.3.0`) + CORS (`v2.8.6`)
* **Database:** SQLite3 (`game_data.db`)
* **Repositories:**
  * Backend: `https://github.com/Yorrr-ux/GameBackend_Kelompok_8`
  * Unity Client: `https://github.com/vlaix/Networking-Game`

---

## 2. Arsitektur Backend (Node.js + SQLite)

Server berjalan secara lokal pada `http://localhost:5000` dengan script development `npm run dev` (menggunakan `nodemon server.js`).

### A. Struktur File Backend
1. **`database.js`**: Menginisialisasi koneksi ke file lokal `./game_data.db` dan membuat tabel secara otomatis jika belum ada.
2. **`server.js`**: Mengatur middleware (`cors`, `express.json`) dan rute REST API.

### B. Skema Database (`game_data.db`)
* **Tabel `users`**:
  * `id` (`INTEGER PRIMARY KEY AUTOINCREMENT`)
  * `username` (`TEXT UNIQUE NOT NULL`)
  * `password_hash` (`TEXT NOT NULL`)
  * `created_at` (`DATETIME DEFAULT CURRENT_TIMESTAMP`)
* **Tabel `scores`**:
  * `id` (`INTEGER PRIMARY KEY AUTOINCREMENT`)
  * `user_id` (`INTEGER NOT NULL`, Foreign Key ke `users(id)`)
  * `score` (`INTEGER NOT NULL` — menyimpan waktu penyelesaian tim dalam **milidetik / ms**)
  * `played_at` (`DATETIME DEFAULT CURRENT_TIMESTAMP`)

### C. Kontrak Endpoint REST API

| Endpoint | Method | Status | Payload Input (JSON) | Response Output (JSON) | Keterangan |
| :--- | :--- | :--- | :--- | :--- | :--- |
| `/api/health` | `GET` | Aktif | *(Tanpa Body)* | `{ status, message, serverTime }` | Health check status server |
| `/api/score/save` | `POST` | Aktif | `{"userId": 1, "score": 25384}` | `{ status, message, scoreId }` | Menyimpan waktu penyelesaian tim (ms) ke tabel `scores` |
| `/api/score/highscore/:userId` | `GET` | Aktif | *(URL Param `userId`)* | `{ status, userId, highScore }` | Mengambil rekor waktu **tercepat** (`SELECT MIN(score) AS bestTime FROM scores WHERE user_id = ? AND score > 0`) |
| `/api/leaderboard` | `GET` | Aktif | *(Tanpa Body)* | `{ status, data: [{ username, highScore }] }` | Mengambil Top 10 waktu penyelesaian **tercepat** (`MIN(s.score)` di-JOIN dengan `users`, `ORDER BY highScore ASC LIMIT 10`) |
| `/api/register` | `POST` | Draft (`501`) | `{"username", "password"}` | `{ message }` | Belum diimplementasikan |
| `/api/login` | `POST` | Draft (`501`) | `{"username", "password"}` | `{ message }` | Belum diimplementasikan |

---

## 3. Arsitektur Unity Client (C# & Mirror)

### A. Mekanika Gameplay & Co-op Team Speedrun
* Game bersifat **kooperatif satu tim** (bukan balapan antar-pemain). Semua pemain di dalam sesi bekerja sama mencapai garis finis.
* Waktu dihitung dalam detik (`float elapsedTime`) sejak spawn, lalu dikonversi ke **milidetik (`int`)** (`Mathf.RoundToInt(elapsedTime * 1000f)`) saat tim menyentuh `FinishLine`.
* Saat tim menang, **seluruh pemain** mendapatkan status menang (**"STAGE CLEAR!"**) dan masing-masing menyimpan catatan waktu tim ke akunnya.
* Helper `PlayerController.FormatTime(float timeInSeconds)` memformat detik menjadi string `"00:00.00"`.

### B. Daftar Skrip Utama & Tanggung Jawabnya

1. **`PlayerController.cs`** (`NetworkBehaviour`)
   * Mengatur pergerakan platformer (`moveSpeed = 5f`, `jumpForce = 7f`, `rotationSpeed = 720f`) dan kamera bersama (`SharedCamera.activePlayers`).
   * Menyinkronkan `health` (`100`) dan `score` (dalam ms) via `[SyncVar]`.
   * Menjalankan stopwatch (`isTimerRunning`) di `Update()`. Input gerakan dikunci otomatis saat `isTimerRunning == false`.
   * Saat `PicuKondisiMenang()` dipanggil oleh `FinishLine`: menghentikan timer, mengunci `rb.velocity = Vector3.zero`, memanggil `[Command] CmdSetScore(finalTimeMs)`, dan memanggil `[Command] CmdReportWin(true)` ke `GameManager`.

2. **`GameManager.cs`** (`NetworkBehaviour` — Singleton)
   * Membutuhkan komponen **Network Identity** di Scene.
   * Menyediakan `public event Action<bool> OnGameOver` (`isTeamWin`).
   * **Aturan Penting Mirror Weaver:** Hanya gunakan tipe primitif (`bool`, `uint`, `int`, `string`) untuk parameter `[Command]` dan `[ClientRpc]`. Hindari passing `NetworkConnection`.
   * Alur: `TriggerGameOver(bool isTeamWin)` di Server $\rightarrow$ `[ClientRpc] RpcNotifyGameOver(bool isTeamWin)` $\rightarrow$ `OnGameOver?.Invoke(isTeamWin)`.

3. **`ScoreDTO.cs`** & **`LeaderboardDTO.cs`**
   * Kelas `[Serializable]` untuk `JsonUtility`: `SaveScoreRequest`, `SaveScoreResponse`, `HighScoreResponse`, `LeaderboardEntry`, dan `LeaderboardResponse`.

4. **`ScoreManager.cs`** (`MonoBehaviour` — Singleton)
   * Berkomunikasi dengan `http://localhost:5000/api` menggunakan `UnityWebRequest`.
   * `SaveCurrentScore(int finalScore, Action onComplete)` dan `GetPersonalHighScore(Action<int> onCallback)`.

5. **`GameOverUI.cs`** (`MonoBehaviour`)
   * Mendengarkan `GameManager.Instance.OnGameOver(bool isTeamWin)`.
   * Menampilkan `GameOverPanel`, membuka kursor (`Cursor.visible = true; Cursor.lockState = CursorLockMode.None;`), menampilkan `"STAGE CLEAR!"` (hijau) jika `isTeamWin == true` atau `"GAME OVER!"` (merah) jika tim gagal.
   * Menyimpan waktu tim ke backend lalu menarik rekor waktu tercepat (`MatchScoreText` dan `HighScoreText`).

6. **`LeaderboardItemUI.cs` & `LeaderboardManager.cs`** (`MonoBehaviour`)
   * `LeaderboardManager` memanggil `GET /api/leaderboard` saat `OnEnable()` dan saat `refreshButton` diklik, lalu meng-instantiate `LeaderboardItemPrefab` ke dalam `ScrollView Content`.
   * `LeaderboardItemUI` memformat `highScore` (ms) menggunakan `PlayerController.FormatTime(scoreMs / 1000f)` menjadi `"00:00.00"`.

---

## 4. Konvensi & Hal yang Harus Diperhatikan

* **Mirror Serialization:** Selalu gunakan tipe data primitif untuk parameter `[Command]` dan `[ClientRpc]`.
* **Speedrun Sorting Logic:** Semua kueri peringkat/high score wajib menggunakan `MIN(score)` dengan filter `score > 0` dan diurutkan `ASC` (waktu tercepat = peringkat #1).
* **Environment Windows:** Gunakan Command Prompt (`cmd`) atau atur PowerShell `Set-ExecutionPolicy RemoteSigned -Scope CurrentUser` jika perintah `npm` terblokir.