using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.BBFCBlackCards
{
    public class BBFCIntroProvider : IIntroProvider
    {
        public string Name => "BBFC Cinema Sequence Provider";

        private readonly ILibraryManager _libraryManager;
        private readonly ILogger<BBFCIntroProvider> _logger;
        private static readonly Random RandomRng = new();

        public BBFCIntroProvider(ILibraryManager libraryManager, ILogger<BBFCIntroProvider> logger)
        {
            _libraryManager = libraryManager;
            _logger = logger;
        }

        public Task<IEnumerable<IntroInfo>> GetIntros(BaseItem item, User user)
        {
            var intros = new List<IntroInfo>();

            if (item is not Movie movie)
            {
                return Task.FromResult<IEnumerable<IntroInfo>>(intros);
            }

            var config = Plugin.Instance?.Configuration ?? new PluginConfiguration();

            var slots = new[]
            {
                (SlotNum: 1, Enabled: config.Slot1Enabled, LibraryId: config.Slot1LibraryId, Count: config.Slot1Count, MatchMode: config.Slot1MatchMode),
                (SlotNum: 2, Enabled: config.Slot2Enabled, LibraryId: config.Slot2LibraryId, Count: config.Slot2Count, MatchMode: config.Slot2MatchMode),
                (SlotNum: 3, Enabled: config.Slot3Enabled, LibraryId: config.Slot3LibraryId, Count: config.Slot3Count, MatchMode: config.Slot3MatchMode),
                (SlotNum: 4, Enabled: config.Slot4Enabled, LibraryId: config.Slot4LibraryId, Count: config.Slot4Count, MatchMode: config.Slot4MatchMode),
                (SlotNum: 5, Enabled: config.Slot5Enabled, LibraryId: config.Slot5LibraryId, Count: config.Slot5Count, MatchMode: config.Slot5MatchMode)
            };

            foreach (var slot in slots)
            {
                if (!slot.Enabled || slot.LibraryId == Guid.Empty)
                {
                    continue;
                }

                var parentFolder = _libraryManager.GetItemById(slot.LibraryId);
                if (parentFolder == null)
                {
                    _logger.LogWarning("[BBFC Cinema] Slot {Slot}: Configured library ID '{Id}' was not found.", slot.SlotNum, slot.LibraryId);
                    continue;
                }

                var query = new InternalItemsQuery(user)
                {
                    ParentId = slot.LibraryId,
                    Recursive = true,
                    IsVirtualItem = false,
                    IncludeItemTypes = new[]
                    {
                        BaseItemKind.Movie,
                        BaseItemKind.Video,
                        BaseItemKind.Trailer,
                        BaseItemKind.MusicVideo,
                        BaseItemKind.Episode
                    }
                };

                var allClipsInLibrary = _libraryManager.GetItemList(query).ToList();
                if (allClipsInLibrary.Count == 0)
                {
                    _logger.LogWarning("[BBFC Cinema] Slot {Slot}: No video items found in library '{LibraryName}'", slot.SlotNum, parentFolder.Name);
                    continue;
                }

                // Filter clips using movie metadata
                var filteredClips = FilterClipsByMetadata(allClipsInLibrary, movie, slot.MatchMode);

                // Fallback to all clips if match produced 0 results
                if (filteredClips.Count == 0)
                {
                    _logger.LogInformation("[BBFC Cinema] Slot {Slot}: No strict '{Mode}' match found for '{Movie}'. Falling back to random library clips.", slot.SlotNum, slot.MatchMode, movie.Name);
                    filteredClips = allClipsInLibrary;
                }

                var selectedClips = filteredClips
                    .OrderBy(_ => RandomRng.Next())
                    .Take(Math.Max(1, slot.Count))
                    .ToList();

                foreach (var clip in selectedClips)
                {
                    _logger.LogInformation("[BBFC Cinema] Slot {Slot} [{Mode}]: Queued '{Clip}' (Id: {Id})", slot.SlotNum, slot.MatchMode, clip.Name, clip.Id);
                    intros.Add(new IntroInfo
                    {
                        ItemId = clip.Id,
                        Path = clip.Path
                    });
                }
            }

            // Append BBFC Black Card
            if (config.EnableBlackCardIntro && !string.IsNullOrEmpty(movie.ContainingFolderPath))
            {
                var cardPath = Path.Combine(movie.ContainingFolderPath, "extras", "blackcard.mp4");
                if (File.Exists(cardPath))
                {
                    _logger.LogInformation("[BBFC Cinema] Queued BBFC Black Card for '{Movie}'", movie.Name);
                    intros.Add(new IntroInfo { Path = cardPath });
                }
                else
                {
                    _logger.LogDebug("[BBFC Cinema] No blackcard.mp4 found in extras for '{Movie}'", movie.Name);
                }
            }

            return Task.FromResult<IEnumerable<IntroInfo>>(intros);
        }

        private List<BaseItem> FilterClipsByMetadata(List<BaseItem> clips, Movie movie, string matchMode)
        {
            switch (matchMode?.ToLowerInvariant())
            {
                case "genre":
                    if (movie.Genres == null || movie.Genres.Length == 0)
                    {
                        return clips;
                    }

                    var movieGenres = new HashSet<string>(movie.Genres, StringComparer.OrdinalIgnoreCase);
                    var matchedByGenre = clips.Where(c =>
                        (c.Genres != null && c.Genres.Any(g => movieGenres.Contains(g))) ||
                        (c.Path != null && movieGenres.Any(g => c.Path.Contains(g, StringComparison.OrdinalIgnoreCase)))
                    ).ToList();

                    return matchedByGenre.Count > 0 ? matchedByGenre : clips;

                case "audiocodec":
                    var audioStream = movie.GetMediaStreams().FirstOrDefault(s => s.Type == MediaStreamType.Audio);
                    if (audioStream == null)
                    {
                        return clips;
                    }

                    var codec = audioStream.Codec?.ToLowerInvariant() ?? string.Empty;
                    var profile = audioStream.Profile?.ToLowerInvariant() ?? string.Empty;
                    var title = audioStream.Title?.ToLowerInvariant() ?? string.Empty;

                    bool isAtmos = profile.Contains("atmos") || title.Contains("atmos");
                    bool isDts = codec.Contains("dts") || profile.Contains("dts");
                    bool isTrueHd = codec.Contains("truehd");
                    bool isDolby = isAtmos || isTrueHd || codec.Contains("ac3") || codec.Contains("eac3");

                    var matchedAudioClips = clips.Where(c =>
                    {
                        var name = (c.Name + " " + (c.Path ?? string.Empty)).ToLowerInvariant();

                        if (isAtmos && (name.Contains("atmos") || name.Contains("dolby atmos"))) return true;
                        if (isDts && (name.Contains("dts") || name.Contains("dts-x") || name.Contains("dts:x") || name.Contains("dts-hd"))) return true;
                        if (isDolby && (name.Contains("dolby") || name.Contains("surround"))) return true;
                        if (codec.Contains("aac") && name.Contains("stereo")) return true;

                        return false;
                    }).ToList();

                    return matchedAudioClips.Count > 0 ? matchedAudioClips : clips;

                case "rating":
                    if (string.IsNullOrEmpty(movie.OfficialRating))
                    {
                        return clips;
                    }

                    var ratingClips = clips.Where(c =>
                        string.Equals(c.OfficialRating, movie.OfficialRating, StringComparison.OrdinalIgnoreCase) ||
                        (c.Path != null && c.Path.Contains(movie.OfficialRating, StringComparison.OrdinalIgnoreCase))
                    ).ToList();

                    return ratingClips.Count > 0 ? ratingClips : clips;

                default:
                    return clips;
            }
        }

        public IEnumerable<string> GetAllIntroFiles()
        {
            return Array.Empty<string>();
        }
    }
}