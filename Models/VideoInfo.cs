namespace YTStudioDownloader.Models;

public sealed class VideoInfo
{
    public string Title { get; init; } = "";
    public string Channel { get; init; } = "";
    public double Duration { get; init; }
    public string Thumbnail { get; init; } = "";
    public List<int> Heights { get; init; } = [];
}
