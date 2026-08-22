# BBFC Black Cards & Cinema Intro Provider for Jellyfin

[![Jellyfin Version](https://img.shields.io/badge/Jellyfin-10.11.x-purple.svg)](https://jellyfin.org/)
[![Latest Release](https://img.shields.io/github/v/release/PandersonTV/jellyfin-plugin-bbfc-blackcards?color=blue)](https://github.com/PandersonTV/jellyfin-plugin-bbfc-blackcards/releases)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

A Jellyfin plugin that recreates the authentic British cinema pre-roll experience. Automatically plays BBFC title/rating cards before feature films and powers a fully customisable, multi-slot pre-show sequence.

---

## 🎬 Key Features

* **BBFC Black Card Injection:** Automatically queues matching BBFC classification cards (e.g., `extras/blackcard.mp4`) directly before the main feature starts.
* **5-Slot Pre-Roll Sequence Engine:** Build a custom theatre sequence with up to five distinct slots (e.g., *Coming Soon Bumpers → Trailers → Audio Idents → Age Advice → Feature Presentation*).
* **Dynamic Metadata Matching:**
  * **Audio Codec:** Matches bumpers to the movie's primary track (Dolby Atmos, DTS:X, DTS-HD, TrueHD, Surround).
  * **Genre Match:** Filters trailers or clips to match the movie currently playing.
  * **Age Rating:** Serves rating bumpers corresponding to official BBFC classifications (U, PG, 12A/12, 15, 18).
* **Native Jellyfin 10.11.x Support:** Built on modern asynchronous provider pipelines (`Task<IEnumerable<IntroInfo>>`).
* **Web UI Dashboard:** Manage slot orders, library targets, and playback rules directly inside Jellyfin Admin Settings.

---

## 📦 Installation

### Option 1: Via Jellyfin Plugin Repository (Recommended)

1. In your Jellyfin web interface, open **Dashboard** $\rightarrow$ **Plugins** $\rightarrow$ **Repositories**.
2. Click **+ (Add)** and configure the manifest URL:
   * **Repository Name:** `BBFC Black Cards Repository`
   * **Repository URL:**
     ```text
     [https://raw.githubusercontent.com/PandersonTV/jellyfin-plugin-bbfc-blackcards/main/manifest.json](https://raw.githubusercontent.com/PandersonTV/jellyfin-plugin-bbfc-blackcards/main/manifest.json)
     ```
3. Navigate to the **Catalog** tab.
4. Locate **BBFC Black Cards**, click **Install**, and select the latest stable release (`v1.2.3.0`).
5. Restart your Jellyfin server.

---

### Option 2: Manual DLL Installation

1. Download `bbfc_plugin_1.2.3.0.zip` from the [Latest Release](https://github.com/PandersonTV/jellyfin-plugin-bbfc-blackcards/releases).
2. Extract `bbfc_plugin.dll`.
3. Create a folder in your Jellyfin plugins directory:
   * **Windows:** `C:\ProgramData\Jellyfin\Server\plugins\BBFCBlackCards_1.2.3.0\`
   * **Linux / Docker:** `/config/plugins/BBFCBlackCards_1.2.3.0/`
4. Copy `bbfc_plugin.dll` into that directory and restart Jellyfin.

---

## ⚙️ Configuration

1. In Jellyfin, navigate to **Dashboard** $\rightarrow$ **Plugins** $\rightarrow$ **BBFC Black Cards**.
2. **Enable BBFC Black Cards:** Toggles the injection of `blackcard.mp4` found inside your movie's `extras/` folder.
3. **Configure Pre-Roll Slots (1 to 5):**
   * **Enable Slot:** Turn specific sequence steps on or off.
   * **Target Library:** Select the Jellyfin library containing your bumpers, idents, or trailers.
   * **Item Count:** Set how many clips should play in this slot.
   * **Matching Mode:** Choose between `Random`, `Genre`, `Audio Codec`, or `Rating`.

---

## 🛠️ Building from Source

**Prerequisites:** [.NET 9.0 SDK](https://dotnet.microsoft.com/download)

