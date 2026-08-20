using System;
using System.Collections.Generic;
using System.IO;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.BBFCBlackCards
{
    public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
    {
        public override string Name => "BBFC Black Cards";

        public override Guid Id => Guid.Parse("f6e8b0d2-9d32-4d64-8842-1e9a26369c5e");

        public override string Description => "Generates BBFC Black Cards and manages cinema intro sequences.";

        public static Plugin? Instance { get; private set; }

        public string PluginFolder => Path.Combine(_applicationPaths.PluginsPath, Name);

        private readonly IApplicationPaths _applicationPaths;

        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
            : base(applicationPaths, xmlSerializer)
        {
            Instance = this;
            _applicationPaths = applicationPaths;
        }

        public IEnumerable<PluginPageInfo> GetPages()
        {
            return new[]
            {
                new PluginPageInfo
                {
                    Name = this.Name,
                    EmbeddedResourcePath = "Jellyfin.Plugin.BBFCBlackCards.configPage.html"
                }
            };
        }
    }
}