namespace MoneyTracker.Application.DTOs.Transactions;

public class TransactionAttachmentDto
{
    public int Id { get; set; }
    public int TransactionId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string? ContentType { get; set; }
    public int FileSize { get; set; }
    public DateTime UploadedDate { get; set; }
    public byte[] FileContent { get; set; } = Array.Empty<byte>();
}
