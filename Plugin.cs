using System;
using System.IO;
using System.Collections.Generic;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.BBFCBlackCards
{
    public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
    {
        public override string Name => "BBFC Black Card Generator";
        public override Guid Id => Guid.Parse("f6e8b0d2-9d32-4d64-8842-1e9a26369c5e");
        public override string Description => "Generates official UK BBFC classification advice title cards into movie extras folders.";

        public static Plugin? Instance { get; private set; }
        public string PluginFolder { get; }

        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
            : base(applicationPaths, xmlSerializer)
        {
            Instance = this;
            PluginFolder = Path.Combine(applicationPaths.PluginsPath, "BBFCBlackCards");
        }

        public IEnumerable<PluginPageInfo> GetPages()
        {
            return new[]
            {
                new PluginPageInfo
                {
                    Name = "bbfcblackcards",
                    EmbeddedResourcePath = "Jellyfin.Plugin.BBFCBlackCards.configPage.html"
                }
            };
        }
    }
}