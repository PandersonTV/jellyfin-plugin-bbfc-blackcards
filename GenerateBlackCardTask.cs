using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;
using Jellyfin.Data.Enums;

namespace Jellyfin.Plugin.BBFCBlackCards
{
    public class GenerateBlackCardsTask : IScheduledTask, IConfigurableScheduledTask
    {
        private readonly ILibraryManager _libraryManager;
        private readonly ILogger<GenerateBlackCardsTask> _logger;

        public GenerateBlackCardsTask(ILibraryManager libraryManager, ILogger<GenerateBlackCardsTask> logger)
        {
            _libraryManager = libraryManager;
            _logger = logger;
        }

        public string Name => "Generate BBFC Black Cards";
        public string Key => "GenerateBBFCBlackCardsTask";
        public string Description => "Scans all movies and generates 5-second BBFC classification title cards into their extras folders.";
        public string Category => "BBFC Blackcards";
        public bool IsHidden => false;
        public bool IsEnabled => true;
        public bool IsLogged => true;

        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            return new[]
            {
                new TaskTriggerInfo
                {
                    Type = TaskTriggerInfoType.DailyTrigger,
                    TimeOfDayTicks = TimeSpan.FromHours(3).Ticks
                }
            };
        }

        public Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
        {
            _logger.LogInformation("[BBFC Generator] Starting movie classification scan...");

            try
            {
                var config = Plugin.Instance?.Configuration ?? new PluginConfiguration();

                // Strictly query only Movie items to avoid deserializing unindexed/foreign DB items
                var query = new InternalItemsQuery
                {
                    IncludeItemTypes = new[] { BaseItemKind.Movie },
                    IsVirtualItem = false,
                    Recursive = true
                };

                var movies = _libraryManager.GetItemList(query).OfType<Movie>().ToList();
                int total = movies.Count;

                _logger.LogInformation("[BBFC Generator] Found {Count} movies to inspect.", total);

                if (total == 0)
                {
                    _logger.LogWarning("[BBFC Generator] No movies found in any library.");
                    progress.Report(100);
                    return Task.CompletedTask;
                }

                int successCount = 0;

                for (int i = 0; i < total; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var movie = movies[i];
                    string? moviePath = movie.Path;

                    if (string.IsNullOrEmpty(moviePath) || !File.Exists(moviePath))
                    {
                        progress.Report((double)(i + 1) / total * 100);
                        continue;
                    }

                    string? folder = Path.GetDirectoryName(moviePath);
                    if (string.IsNullOrEmpty(folder))
                    {
                        progress.Report((double)(i + 1) / total * 100);
                        continue;
                    }

                    string targetCardPath = Path.Combine(folder, "extras", "blackcard.mp4");

                    if (File.Exists(targetCardPath) && !config.OverwriteExisting)
                    {
                        progress.Report((double)(i + 1) / total * 100);
                        continue;
                    }

                    string rating = movie.OfficialRating ?? "UNRATED";
                    string advice = GetAdvice(rating);

                    _logger.LogInformation("[BBFC Generator] Generating card for '{Movie}' [{Rating}]", movie.Name, rating);

                    bool ok = TitleCardGenerator.Generate(movie.Name, rating, advice, targetCardPath, _logger);
                    if (ok) successCount++;

                    progress.Report((double)(i + 1) / total * 100);
                }

                _logger.LogInformation("[BBFC Generator] Completed successfully. Generated {Count} title cards.", successCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[BBFC Generator] Unexpected failure during scan.");
                throw;
            }

            return Task.CompletedTask;
        }

        private static string GetAdvice(string rating)
        {
            string clean = (rating ?? "").ToUpperInvariant();
            if (clean.Contains("U")) return "contains very mild threat, violence";
            if (clean.Contains("PG")) return "contains mild violence, threat, language";
            if (clean.Contains("12")) return "contains moderate violence, threat";
            if (clean.Contains("15")) return "contains strong violence, language";
            if (clean.Contains("18")) return "contains strong violence, gore, language";
            return "contains classification advisory material";
        }
    }
}