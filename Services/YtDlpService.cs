using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using YTStudioDownloader.Models;

namespace YTStudioDownloader.Services;

public sealed class YtDlpService
{
    private readonly ToolManager _tools;
    public YtDlpService(ToolManager tools) => _tools = tools;

    public async Task<VideoInfo> AnalyzeAsync(string url, IProgress<string>? log, CancellationToken ct)
    {
        if (_tools.YtDlp == null) await _tools.EnsureAsync(ct);
        var result = await RunAsync(_tools.YtDlp!, new[]
        {
            "--dump-single-json","--no-warnings","--skip-download",url
        }, log, ct);

        if (result.Code != 0) throw new InvalidOperationException(result.Err);

        using var doc = JsonDocument.Parse(result.Out);
        var r = doc.RootElement;
        var heights = new List<int>();
        if (r.TryGetProperty("formats", out var formats) && formats.ValueKind == JsonValueKind.Array)
        {
            foreach (var f in formats.EnumerateArray())
            {
                if (f.TryGetProperty("height", out var h) && h.TryGetInt32(out var v))
                    heights.Add(v);
            }
        }

        return new VideoInfo
        {
            Title = r.TryGetProperty("title", out var t) ? t.GetString() ?? "" : "",
            Channel = r.TryGetProperty("channel", out var c) ? c.GetString() ?? "" :
                      (r.TryGetProperty("uploader", out var u) ? u.GetString() ?? "" : ""),
            Duration = r.TryGetProperty("duration", out var d) && d.TryGetDouble(out var dv) ? dv : 0,
            Thumbnail = r.TryGetProperty("thumbnail", out var th) ? th.GetString() ?? "" : "",
            Heights = heights.Distinct().OrderByDescending(x => x).ToList()
        };
    }

    public async Task DownloadAsync(DownloadJob job, IProgress<double>? progress, IProgress<string>? log, CancellationToken ct)
    {
        if (_tools.YtDlp == null || (_tools.Ffmpeg == null && job.Mode != DownloadMode.Audio))
            await _tools.EnsureAsync(ct);

        Directory.CreateDirectory(job.Folder);

        if (job.Mode == DownloadMode.Clips)
        {
            for (int i = 0; i < job.Clips.Count; i++)
            {
                ct.ThrowIfCancellationRequested();
                var c = job.Clips[i];
                var args = BuildArgs(job, c, i + 1);
                await RunDownloadAsync(args, i, job.Clips.Count, progress, log, ct);
            }
            progress?.Report(100);
        }
        else
        {
            await RunDownloadAsync(BuildArgs(job, null, 0), 0, 1, progress, log, ct);
            progress?.Report(100);
        }
    }

    private string[] BuildArgs(DownloadJob job, ClipRange? clip, int index)
    {
        var a = new List<string> { "--newline", "--no-warnings", "-P", job.Folder };
        if (job.Mode == DownloadMode.Playlist)
        {
            a.AddRange(new[] { "--yes-playlist", "-o",
                Path.Combine(job.Folder, "%(playlist_index)03d - %(title)s.%(ext)s") });
        }
        else
        {
            var name = clip == null ? job.FileName : $"{clip.Name}_{index:000}";
            a.AddRange(new[] { "-o", Path.Combine(job.Folder, name + ".%(ext)s"), "--no-playlist" });
        }

        if (job.Mode == DownloadMode.Audio)
        {
            a.AddRange(new[] { "-x", "--audio-format", job.Format });
        }
        else
        {
            var f = job.Quality == "best"
                ? "bv*[ext=mp4]+ba[ext=m4a]/b[ext=mp4]/bv*+ba/b"
                : $"bv*[height<={job.Quality}]+ba/b";
            a.AddRange(new[] { "-f", f, "--merge-output-format", job.Format });
        }

        if (clip != null)
            a.AddRange(new[] { "--download-sections", $"*{clip.Start}-{clip.End}", "--force-keyframes-at-cuts" });

        a.Add(job.Url);
        return a.ToArray();
    }

    private async Task RunDownloadAsync(string[] args, int index, int count,
        IProgress<double>? progress, IProgress<string>? log, CancellationToken ct)
    {
        var result = await RunAsync(_tools.YtDlp!, args, new Progress<string>(line =>
        {
            log?.Report(line);
            var m = Regex.Match(line, @"(\d+(?:\.\d+)?)%");
            if (m.Success && double.TryParse(m.Groups[1].Value,
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var p))
                progress?.Report(((index * 100) + p) / count);
        }), ct);

        if (result.Code != 0) throw new InvalidOperationException(result.Err);
    }

    private static async Task<(int Code, string Out, string Err)> RunAsync(
        string exe, IEnumerable<string> args, IProgress<string>? log, CancellationToken ct)
    {
        var psi = new ProcessStartInfo
        {
            FileName = exe,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = Path.GetDirectoryName(exe) ?? AppContext.BaseDirectory
        };
        foreach (var arg in args) psi.ArgumentList.Add(arg);

        using var p = new Process { StartInfo = psi };
        var output = new StringBuilder();
        var error = new StringBuilder();

        p.OutputDataReceived += (_, e) => { if (e.Data != null) { output.AppendLine(e.Data); log?.Report(e.Data); } };
        p.ErrorDataReceived += (_, e) => { if (e.Data != null) { error.AppendLine(e.Data); log?.Report(e.Data); } };

        if (!p.Start()) throw new InvalidOperationException("تعذر تشغيل العملية.");
        p.BeginOutputReadLine();
        p.BeginErrorReadLine();

        await p.WaitForExitAsync(ct);
        return (p.ExitCode, output.ToString(), error.ToString());
    }
}
