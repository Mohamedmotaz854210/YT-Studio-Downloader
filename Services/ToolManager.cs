using System.IO.Compression;
using System.Net.Http;

namespace YTStudioDownloader.Services;

public sealed class ToolManager
{
    public string RuntimeDir { get; } = Path.Combine(AppContext.BaseDirectory, "runtime");

    public ToolManager() => Directory.CreateDirectory(RuntimeDir);

    public string? Find(string name)
    {
        var local = Path.Combine(RuntimeDir, name);
        if (File.Exists(local)) return local;

        foreach (var dir in (Environment.GetEnvironmentVariable("PATH") ?? "")
                 .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            var candidate = Path.Combine(dir.Trim('"'), name);
            if (File.Exists(candidate)) return candidate;
        }
        return null;
    }

    public string? YtDlp => Find("yt-dlp.exe");
    public string? Ffmpeg => Find("ffmpeg.exe");
    public string? Ffprobe => Find("ffprobe.exe");

    public async Task EnsureAsync(CancellationToken ct)
    {
        using var http = new HttpClient();
        http.DefaultRequestHeaders.UserAgent.ParseAdd("YT-Studio-Downloader/1.0");

        if (YtDlp == null)
        {
            var p = Path.Combine(RuntimeDir, "yt-dlp.exe");
            await using var s = await http.GetStreamAsync(
                "https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe", ct);
            await using var f = File.Create(p);
            await s.CopyToAsync(f, ct);
        }

        if (Ffmpeg == null || Ffprobe == null)
        {
            var zip = Path.Combine(RuntimeDir, "ffmpeg.zip");
            var tmp = Path.Combine(RuntimeDir, "_ffmpeg");
            await using (var s = await http.GetStreamAsync(
                "https://github.com/BtbN/FFmpeg-Builds/releases/download/latest/ffmpeg-master-latest-win64-gpl.zip", ct))
            await using (var f = File.Create(zip))
                await s.CopyToAsync(f, ct);

            if (Directory.Exists(tmp)) Directory.Delete(tmp, true);
            ZipFile.ExtractToDirectory(zip, tmp);

            var ff = Directory.GetFiles(tmp, "ffmpeg.exe", SearchOption.AllDirectories).FirstOrDefault();
            var fp = Directory.GetFiles(tmp, "ffprobe.exe", SearchOption.AllDirectories).FirstOrDefault();
            if (ff == null || fp == null) throw new InvalidOperationException("تعذر العثور على FFmpeg داخل الحزمة.");

            File.Copy(ff, Path.Combine(RuntimeDir, "ffmpeg.exe"), true);
            File.Copy(fp, Path.Combine(RuntimeDir, "ffprobe.exe"), true);

            File.Delete(zip);
            Directory.Delete(tmp, true);
        }
    }
}
