namespace GlassDesk.Platform;

public sealed record NetworkSpeedSnapshot(bool Connected, string AdapterName, double? DownloadBytesPerSecond, double? UploadBytesPerSecond);
