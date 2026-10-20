namespace AiCoder.Models;

public sealed class PendingAttachment
{
    public string FileName { get; set; } = "";
    public byte[]? Bytes { get; set; }
    public string? MimeType { get; set; }
    public bool IsImage { get; set; }

    public ImageSource? Thumbnail
    {
        get
        {
            if (!IsImage || Bytes is null || Bytes.Length == 0) return null;
            return ImageSource.FromStream(() => new MemoryStream(Bytes));
        }
    }
}
