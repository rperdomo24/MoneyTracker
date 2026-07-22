using MoneyTracker.Domain.Enums.Ai;
using MoneyTracker.Domain.Interfaces;

namespace MoneyTracker.Domain.Entities
{
    public class AiTrainingData : ITenantOwned
    {
        public int Id { get; set; }
        public Guid TenantId { get; set; }
        public DateTime CalledAtUtc { get; set; } = DateTime.UtcNow;
        public AiServiceType ServiceType { get; set; }
        public string Model { get; set; } = string.Empty;
        public AiInputType InputType { get; set; }
        public string RawInput { get; set; } = string.Empty;
        public AiOutputType OutputType { get; set; }
        public string RawOutput { get; set; } = string.Empty;
        public int InputTokens { get; set; }
        public int OutputTokens { get; set; }
        public AiUserFeedback? UserFeedback { get; set; }
        public DateTime? FeedbackAt { get; set; }
    }
}
