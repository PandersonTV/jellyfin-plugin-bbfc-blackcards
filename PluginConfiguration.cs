using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.BBFCBlackCards
{
    public class PluginConfiguration : BasePluginConfiguration
    {
        public string FfmpegPath { get; set; } = @"C:\Program Files\Jellyfin\Server\ffmpeg.exe";
        public bool OverwriteExisting { get; set; } = false;
    }
}