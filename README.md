# BBFC Black Card Generator v1.2.1.0

Initial alpha release of the **BBFC Black Card Generator** plugin for Jellyfin. This plugin automatically scans your movie library and uses FFmpeg to generate authentic 5-second British Board of Film Classification (BBFC) title cards directly inside movie `extras/` folders.

### ✨ Features
* **Authentic BBFC Templates:** Full template support for official UK classifications: `U`, `PG`, `12A / 12`, `15`, and `18`.
* **Dynamic Typography:** Automatically calculates font scaling, multi-line wrapping, and bottom-anchored positioning for movie titles and BBFC advisory advice.
* **Scheduled Task Integration:** Runs on demand or on a set schedule via Jellyfin's **Scheduled Tasks** menu.
* **In-Dashboard Configuration:** Dedicated web UI to set custom FFmpeg paths and toggle file overwrite options.
* **Non-Destructive Extras Placement:** Safely creates `blackcard.mp4` inside existing `extras/` folders without modifying other featurettes.

---

### 📦 Installation

#### Method 1: Jellyfin Plugin Repository (Recommended)
1. In Jellyfin, go to **Dashboard** $\rightarrow$ **Plugins** $\rightarrow$ **Repositories**.
2. Add a new repository:
   * **Name:** `BBFC Black Cards`
   * **URL:** `https://raw.githubusercontent.com/PandersonTV/jellyfin-plugin-bbfc-blackcards/main/manifest.json`
3. Go to the **Catalog** tab, find **BBFC Black Card Generator**, and click **Install**.
4. Restart your Jellyfin server.

#### Method 2: Manual Installation
1. Download `jellyfin-plugin-bbfc-1.0.0.0.zip` from the assets below.
2. Extract the contents into your Jellyfin plugins directory:
   * Windows: `C:\ProgramData\Jellyfin\Server\plugins\BBFCBlackCards\`
   * Linux: `/var/lib/jellyfin/plugins/BBFCBlackCards/`
3. Restart your Jellyfin server.

---

### ⚠️ Alpha Notice
* Ensure **FFmpeg** is installed and accessible to your Jellyfin server.
* If movie folders are read-only, ensure Jellyfin has write permissions to create the `extras/` directory.
