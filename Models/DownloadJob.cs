namespace YTStudioDownloader.Models;

public enum DownloadMode { Video, Clips, Playlist, Audio }
public enum JobState { Queued, Running, Completed, Failed, Cancelled }

public sealed class ClipRange
{
    public string Start { get; set; } = "00:00:00";
    public string End { get; set; } = "00:00:30";
    public string Name { get; set; } = "Clip";
}

public sealed class DownloadJob
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Url { get; init; } = "";
    public DownloadMode Mode { get; init; }
    public string Quality { get; init; } = "best";
    public string Format { get; init; } = "mp4";
    public string Folder { get; init; } = "";
    public string FileName { get; init; } = "%(title)s";
    public List<ClipRange> Clips { get; init; } = [];
    public JobState State { get; set; } = JobState.Queued;
    public double Progress { get; set; }
    public string LastError { get; set; } = "";
}
