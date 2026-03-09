using Microsoft.EntityFrameworkCore;
using MoneyTracker.Application.Common;
using MoneyTracker.Application.DTOs.Auth;
using MoneyTracker.Application.Interfaces;
using MoneyTracker.Domain.Entities;
using MoneyTracker.Infrastructure.Persistence;
using System.Security.Cryptography;
using System.Text;

namespace MoneyTracker.Infrastructure.Services
{
    public class VerificationCodeService : IVerificationCodeService
    {
        private readonly MoneyTrackerDbContext _dbContext;

        public VerificationCodeService(MoneyTrackerDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<OperationResult<VerificationCodeIssueDto>> IssueCodeAsync(
            Guid userId,
            Guid tenantId,
            string purpose,
            int expiryMinutes,
            int resendCooldownSeconds,
            int maxAttempts = 3)
        {
            var now = DateTime.UtcNow;
            var latestActive = await _dbContext.UserVerificationCodes
                .Where(x => x.UserId == userId &&
                            x.TenantId == tenantId &&
                            x.Purpose == purpose &&
                            x.InvalidatedAtUtc == null &&
                            x.ConsumedAtUtc == null &&
                            x.ExpiresAtUtc > now)
                .OrderByDescending(x => x.CreatedAtUtc)
                .FirstOrDefaultAsync();

            if (latestActive is not null && latestActive.ResendAvailableAtUtc > now)
            {
                var seconds = (int)Math.Ceiling((latestActive.ResendAvailableAtUtc - now).TotalSeconds);
                return OperationResult<VerificationCodeIssueDto>.Fail($"Please wait {seconds} seconds before requesting another code.");
            }

            if (latestActive is not null)
            {
                latestActive.InvalidatedAtUtc = now;
            }

            var code = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
            var entry = new UserVerificationCode
            {
                UserId = userId,
                TenantId = tenantId,
                Purpose = purpose,
                CodeHash = HashCode(code),
                CreatedAtUtc = now,
                ExpiresAtUtc = now.AddMinutes(expiryMinutes),
                ResendAvailableAtUtc = now.AddSeconds(resendCooldownSeconds),
                MaxAttempts = maxAttempts
            };

            _dbContext.UserVerificationCodes.Add(entry);
            _dbContext.UserVerificationCodeAudits.Add(new UserVerificationCodeAudit
            {
                VerificationCodeId = entry.Id,
                UserId = userId,
                TenantId = tenantId,
                Purpose = purpose,
                EventType = "issued",
                Success = true,
                AttemptCount = 0,
                EventAtUtc = now,
                Message = "Verification code issued."
            });
            await _dbContext.SaveChangesAsync();

            return OperationResult<VerificationCodeIssueDto>.Ok(new VerificationCodeIssueDto
            {
                Code = code,
                ExpiresAtUtc = entry.ExpiresAtUtc,
                ResendAvailableAtUtc = entry.ResendAvailableAtUtc
            }, "Verification code issued.");
        }

        public async Task<OperationResult> VerifyCodeAsync(
            Guid userId,
            Guid tenantId,
            string purpose,
            string code)
        {
            var now = DateTime.UtcNow;
            var record = await _dbContext.UserVerificationCodes
                .Where(x => x.UserId == userId &&
                            x.TenantId == tenantId &&
                            x.Purpose == purpose &&
                            x.InvalidatedAtUtc == null &&
                            x.ConsumedAtUtc == null)
                .OrderByDescending(x => x.CreatedAtUtc)
                .FirstOrDefaultAsync();

            if (record is null)
            {
                _dbContext.UserVerificationCodeAudits.Add(new UserVerificationCodeAudit
                {
                    VerificationCodeId = null,
                    UserId = userId,
                    TenantId = tenantId,
                    Purpose = purpose,
                    EventType = "verify",
                    Success = false,
                    AttemptCount = 0,
                    EventAtUtc = now,
                    Message = "No active verification code was found."
                });
                await _dbContext.SaveChangesAsync();
                return OperationResult.Fail("No verification code request was found.");
            }

            if (record.ExpiresAtUtc <= now)
            {
                record.InvalidatedAtUtc = now;
                _dbContext.UserVerificationCodeAudits.Add(new UserVerificationCodeAudit
                {
                    VerificationCodeId = record.Id,
                    UserId = userId,
                    TenantId = tenantId,
                    Purpose = purpose,
                    EventType = "verify",
                    Success = false,
                    AttemptCount = record.AttemptCount,
                    EventAtUtc = now,
                    Message = "Verification code expired."
                });
                await _dbContext.SaveChangesAsync();
                return OperationResult.Fail("Verification code expired.");
            }

            if (!string.Equals(record.CodeHash, HashCode(code), StringComparison.Ordinal))
            {
                record.AttemptCount += 1;
                if (record.AttemptCount >= record.MaxAttempts)
                {
                    record.InvalidatedAtUtc = now;
                }

                _dbContext.UserVerificationCodeAudits.Add(new UserVerificationCodeAudit
                {
                    VerificationCodeId = record.Id,
                    UserId = userId,
                    TenantId = tenantId,
                    Purpose = purpose,
                    EventType = "verify",
                    Success = false,
                    AttemptCount = record.AttemptCount,
                    EventAtUtc = now,
                    Message = "Invalid verification code."
                });
                await _dbContext.SaveChangesAsync();
                return OperationResult.Fail("Invalid verification code.");
            }

            record.ConsumedAtUtc = now;
            _dbContext.UserVerificationCodeAudits.Add(new UserVerificationCodeAudit
            {
                VerificationCodeId = record.Id,
                UserId = userId,
                TenantId = tenantId,
                Purpose = purpose,
                EventType = "verify",
                Success = true,
                AttemptCount = record.AttemptCount,
                EventAtUtc = now,
                Message = "Verification code validated."
            });
            await _dbContext.SaveChangesAsync();
            return OperationResult.Ok("Verification code validated.");
        }

        private static string HashCode(string code)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(code));
            return Convert.ToHexString(bytes);
        }
    }
}
