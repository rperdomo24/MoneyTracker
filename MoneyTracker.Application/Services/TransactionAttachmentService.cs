using Microsoft.Extensions.Logging;
using MoneyTracker.Application.Common;
using MoneyTracker.Application.DTOs.Transactions;
using MoneyTracker.Application.Interfaces;
using MoneyTracker.Domain.Entities;
using MoneyTracker.Domain.Interfaces;

namespace MoneyTracker.Application.Services;

public class TransactionAttachmentService : ITransactionAttachmentService
{
    private const int MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB
    private const int MaxAttachmentsPerTransaction = 5;

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/gif", "image/webp", "image/bmp",
        "application/pdf"
    };

    private readonly ITransactionAttachmentRepository _repository;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<TransactionAttachmentService> _logger;

    public TransactionAttachmentService(
        ITransactionAttachmentRepository repository,
        ITenantContext tenantContext,
        ILogger<TransactionAttachmentService> logger)
    {
        _repository = repository;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<OperationResult<List<TransactionAttachmentDto>>> GetByTransactionIdAsync(int transactionId)
    {
        try
        {
            var attachments = await _repository.GetByTransactionIdAsync(transactionId);
            var dtos = attachments.Select(MapToDto).ToList();
            return OperationResult<List<TransactionAttachmentDto>>.Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting attachments for transaction {TransactionId}", transactionId);
            return OperationResult<List<TransactionAttachmentDto>>.Fail("Failed to load attachments");
        }
    }

    public async Task<OperationResult<TransactionAttachmentDto>> AddAsync(
        int transactionId, string fileName, string? contentType, byte[] content)
    {
        try
        {
            if (content.Length > MaxFileSizeBytes)
                return OperationResult<TransactionAttachmentDto>.Fail("File exceeds maximum size of 10 MB");

            if (contentType != null && !AllowedContentTypes.Contains(contentType))
                return OperationResult<TransactionAttachmentDto>.Fail("File type not allowed. Use images or PDF.");

            var existing = await _repository.GetByTransactionIdAsync(transactionId);
            if (existing.Count >= MaxAttachmentsPerTransaction)
                return OperationResult<TransactionAttachmentDto>.Fail($"Maximum {MaxAttachmentsPerTransaction} attachments per transaction");

            var tenantId = _tenantContext.TenantId ?? Guid.Empty;
            var attachment = new TransactionAttachment
            {
                TransactionId = transactionId,
                TenantId = tenantId,
                FileName = fileName,
                ContentType = contentType,
                FileSize = content.Length,
                FileContent = content,
                UploadedDate = DateTime.UtcNow
            };

            var saved = await _repository.AddAsync(attachment);
            return OperationResult<TransactionAttachmentDto>.Ok(MapToDto(saved));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding attachment to transaction {TransactionId}", transactionId);
            return OperationResult<TransactionAttachmentDto>.Fail("Failed to save attachment");
        }
    }

    public async Task<OperationResult> DeleteAsync(int attachmentId)
    {
        try
        {
            var attachment = await _repository.GetByIdAsync(attachmentId);
            if (attachment == null)
                return OperationResult.Fail("Attachment not found");

            await _repository.DeleteAsync(attachment);
            return OperationResult.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting attachment {AttachmentId}", attachmentId);
            return OperationResult.Fail("Failed to delete attachment");
        }
    }

    private static TransactionAttachmentDto MapToDto(TransactionAttachment a) => new()
    {
        Id = a.Id,
        TransactionId = a.TransactionId,
        FileName = a.FileName,
        ContentType = a.ContentType,
        FileSize = a.FileSize,
        UploadedDate = a.UploadedDate,
        FileContent = a.FileContent
    };
}
