using System;
using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.BBFCBlackCards
{
    public class PluginConfiguration : BasePluginConfiguration
    {
        public string FfmpegPath { get; set; } = "ffmpeg";
        public bool OverwriteExisting { get; set; } = false;
        public bool EnableBlackCardIntro { get; set; } = true;

        // Slot 1
        public bool Slot1Enabled { get; set; } = false;
        public Guid Slot1LibraryId { get; set; } = Guid.Empty;
        public int Slot1Count { get; set; } = 1;
        public string Slot1MatchMode { get; set; } = "AudioCodec";

        // Slot 2
        public bool Slot2Enabled { get; set; } = false;
        public Guid Slot2LibraryId { get; set; } = Guid.Empty;
        public int Slot2Count { get; set; } = 2;
        public string Slot2MatchMode { get; set; } = "Genre";

        // Slot 3
        public bool Slot3Enabled { get; set; } = false;
        public Guid Slot3LibraryId { get; set; } = Guid.Empty;
        public int Slot3Count { get; set; } = 1;
        public string Slot3MatchMode { get; set; } = "None";

        // Slot 4
        public bool Slot4Enabled { get; set; } = false;
        public Guid Slot4LibraryId { get; set; } = Guid.Empty;
        public int Slot4Count { get; set; } = 1;
        public string Slot4MatchMode { get; set; } = "None";

        // Slot 5
        public bool Slot5Enabled { get; set; } = false;
        public Guid Slot5LibraryId { get; set; } = Guid.Empty;
        public int Slot5Count { get; set; } = 1;
        public string Slot5MatchMode { get; set; } = "None";
    }
}