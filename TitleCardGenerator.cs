using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.BBFCBlackCards
{
    public class RatingConfig
    {
        public string TemplateFileName { get; set; } = string.Empty;
        public string TitleColor { get; set; } = "white";
        public string AdviceColor { get; set; } = "yellow";
        public int TitleFontSize { get; set; } = 56;
        public int AdviceFontSize { get; set; } = 28;
    }

    public static class TitleCardGenerator
    {
        private const string FontFileName = "CynthoSlabPro-Regular.otf";
        private const int ContentLeftMargin = 760;
        private const int TitleBaselineY = 480;
        private const int AdviceFixedY = 525;
        private const int LineSpacing = 12;

        private static readonly Dictionary<string, RatingConfig> Profiles = new(StringComparer.OrdinalIgnoreCase)
        {
            ["U"] = new RatingConfig { TemplateFileName = "blackcard_u.png", AdviceColor = "#2ECC71" },
            ["PG"] = new RatingConfig { TemplateFileName = "blackcard_pg.png", AdviceColor = "#F1C40F" },
            ["12A"] = new RatingConfig { TemplateFileName = "blackcard_12a.png", AdviceColor = "#E67E22" },
            ["12"] = new RatingConfig { TemplateFileName = "blackcard_12a.png", AdviceColor = "#E67E22" },
            ["15"] = new RatingConfig { TemplateFileName = "blackcard_15.png", AdviceColor = "#eb5791" },
            ["18"] = new RatingConfig { TemplateFileName = "blackcard_18.png", AdviceColor = "#E74C3C" },
            ["UNRATED"] = new RatingConfig { TemplateFileName = "blackcard_15.png", AdviceColor = "white" }
        };

        public static RatingConfig GetProfile(string rating)
        {
            string clean = (rating ?? "UNRATED").ToUpperInvariant()
                .Replace("GB-", "")
                .Replace("BBFC ", "")
                .Replace("UK:", "")
                .Replace("RATED ", "")
                .Trim();

            if (clean.Contains("12A") || clean == "12") return Profiles["12A"];
            if (clean.Contains("15")) return Profiles["15"];
            if (clean.Contains("18")) return Profiles["18"];
            if (clean.Contains("PG")) return Profiles["PG"];
            if (clean.Contains("U")) return Profiles["U"];

            return Profiles.TryGetValue(clean, out var profile) ? profile : Profiles["UNRATED"];
        }

        private static (string FormattedTitle, int FontSize, int TitleY) FormatBottomAnchoredTitle(string title, int baseFontSize, int titleBaselineY, int lineSpacing)
        {
            int length = title.Length;
            string wrappedText;
            int fontSize;

            if (length <= 22)
            {
                wrappedText = title;
                fontSize = baseFontSize;
            }
            else if (length <= 34)
            {
                wrappedText = title;
                fontSize = 42;
            }
            else
            {
                wrappedText = WrapWords(title, maxCharsPerLine: 26);
                fontSize = 38;
            }

            int lineCount = wrappedText.Split('\n').Length;
            int totalBlockHeight = (lineCount * fontSize) + ((lineCount - 1) * lineSpacing);
            int calculatedTitleY = titleBaselineY - totalBlockHeight;

            return (wrappedText, fontSize, calculatedTitleY);
        }

        private static string WrapWords(string text, int maxCharsPerLine)
        {
            string[] words = text.Split(' ');
            var sb = new StringBuilder();
            int currentLineLength = 0;

            foreach (var word in words)
            {
                if (currentLineLength + word.Length + 1 > maxCharsPerLine && currentLineLength > 0)
                {
                    sb.Append("\n");
                    currentLineLength = 0;
                }
                else if (currentLineLength > 0)
                {
                    sb.Append(" ");
                    currentLineLength++;
                }

                sb.Append(word);
                currentLineLength += word.Length;
            }

            return sb.ToString();
        }

        private static string SanitizeForFfmpeg(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;

            return input
                .Replace("'", "’")          // Typographical curly apostrophe avoids parser breaks
                .Replace("\"", "”")
                .Replace("\\", "\\\\")
                .Replace(":", @"\:")
                .Replace("%", @"\%");
        }

        public static bool Generate(string movieTitle, string officialRating, string reasonText, string outputPath, ILogger logger)
        {
            var config = Plugin.Instance?.Configuration ?? new PluginConfiguration();
            var profile = GetProfile(officialRating);

            string pluginDir = Plugin.Instance?.PluginFolder ?? @"C:\ProgramData\Jellyfin\Server\plugins\BBFCBlackCards";
            string assetsPath = Path.Combine(pluginDir, "assets");
            string templatePath = Path.Combine(assetsPath, profile.TemplateFileName);
            string fontPath = Path.Combine(assetsPath, FontFileName);

            if (!File.Exists(templatePath))
            {
                logger.LogError("[BBFC Generator] Template missing at: {Path} for movie '{Movie}'", templatePath, movieTitle);
                return false;
            }

            string? outDir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(outDir))
            {
                Directory.CreateDirectory(outDir);
            }

            string uppercaseTitle = movieTitle.ToUpperInvariant();
            var (formattedTitle, dynamicFontSize, calculatedTitleY) = 
                FormatBottomAnchoredTitle(uppercaseTitle, profile.TitleFontSize, TitleBaselineY, LineSpacing);

            string safeTitle = SanitizeForFfmpeg(formattedTitle);
            string safeReason = SanitizeForFfmpeg(reasonText);

            string ffmpegFont = fontPath.Replace("\\", "/").Replace(":", @"\:");
            string ffmpegImage = templatePath.Replace("\\", "/");

            string filter = 
                $"drawtext=fontfile='{ffmpegFont}':text='{safeTitle}':fontcolor={profile.TitleColor}:fontsize={dynamicFontSize}:line_spacing={LineSpacing}:x={ContentLeftMargin}:y={calculatedTitleY}," +
                $"drawtext=fontfile='{ffmpegFont}':text='{safeReason}':fontcolor={profile.AdviceColor}:fontsize={profile.AdviceFontSize}:x={ContentLeftMargin}:y={AdviceFixedY}";

            string arguments = 
                $"-y " +
                $"-loop 1 -t 5 -framerate 24 -i \"{ffmpegImage}\" " +
                $"-f lavfi -t 5 -i \"anullsrc=channel_layout=stereo:sample_rate=48000\" " +
                $"-vf \"{filter}\" " +
                $"-c:v libx264 -preset ultrafast -crf 18 -pix_fmt yuv420p " +
                $"-c:a aac -b:a 192k -shortest " +
                $"\"{outputPath}\"";

            var startInfo = new ProcessStartInfo
            {
                FileName = config.FfmpegPath,
                Arguments = arguments,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardError = true,
                StandardErrorEncoding = Encoding.UTF8
            };

            try
            {
                using var process = new Process { StartInfo = startInfo };
                process.Start();

                string errorLog = process.StandardError.ReadToEnd();
                process.WaitForExit(8000);

                if (process.ExitCode == 0 && File.Exists(outputPath))
                {
                    return true;
                }

                logger.LogError("[BBFC Generator] FFmpeg failed for '{Movie}'. Details: {Error}", movieTitle, errorLog);
                return false;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[BBFC Generator] Exception rendering card for '{Movie}'", movieTitle);
                return false;
            }
        }
    }
}