using Domain.Entities.Common;
using System.Text.Json.Serialization;

namespace Application.Dto.BannerEventSpecialDTOs
{
    public class RuleEvaluationResultDTO
    {
        //  Basic validation properties
        [JsonPropertyName("isValid")]
        public bool IsValid { get; set; }

        [JsonPropertyName("isEligible")]
        public bool IsEligible { get; set; }

        [JsonPropertyName("messages")]
        public List<string> Messages { get; set; } = new();

        //  Rule evaluation results
        [JsonPropertyName("appliedRules")]
        public List<AppliedRuleDTO> AppliedRules { get; set; } = new();

        [JsonPropertyName("failedRules")]
        public List<FailedRuleDTO> FailedRules { get; set; } = new();

        //  Discount calculation
        [JsonPropertyName("calculatedDiscount")]
        public decimal CalculatedDiscount { get; set; }

        [JsonPropertyName("formattedDiscount")]
        public string FormattedDiscount { get; set; } = string.Empty;

        [JsonPropertyName("finalDiscount")]
        public decimal FinalDiscount { get; set; }

        //  Performance metrics
        [JsonPropertyName("processingTimeMs")]
        public long ProcessingTimeMs { get; set; }

        [JsonPropertyName("rulesEvaluated")]
        public int RulesEvaluated { get; set; }

        //  Summary information
        [JsonPropertyName("summaryMessage")]
        public string SummaryMessage => IsEligible ? "All rules passed - Discount applied" : "Some rules failed - No discount";

        [JsonPropertyName("evaluationDetails")]
        public object EvaluationDetails => new
        {
            totalRulesEvaluated = RulesEvaluated,
            rulesApplied = AppliedRules.Count,
            rulesFailed = FailedRules.Count,
            processingTime = $"{ProcessingTimeMs}ms",
            eligibilityStatus = IsEligible ? "ELIGIBLE" : "NOT_ELIGIBLE",
            discountStatus = CalculatedDiscount > 0 ? "DISCOUNT_AVAILABLE" : "NO_DISCOUNT"
        };
    }

    //  DTO for successfully applied rules
    public class AppliedRuleDTO
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("priority")]
        public int Priority { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("targetValue")]
        public string TargetValue { get; set; } = string.Empty;

        [JsonPropertyName("discountValue")]
        public decimal DiscountValue { get; set; }

        [JsonPropertyName("maxDiscount")]
        public decimal? MaxDiscount { get; set; }

        [JsonPropertyName("minOrderValue")]
        public decimal? MinOrderValue { get; set; }

        [JsonPropertyName("formattedDiscount")]
        public string FormattedDiscount { get; set; } = string.Empty;

        [JsonPropertyName("appliedAt")]
        public DateTime AppliedAt { get; set; } = DateTime.UtcNow;

        [JsonPropertyName("ruleDescription")]
        public string RuleDescription { get; set; } = string.Empty;
    }

    //  DTO for failed rules with failure reasons
    public class FailedRuleDTO
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("priority")]
        public int Priority { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("targetValue")]
        public string TargetValue { get; set; } = string.Empty;

        [JsonPropertyName("minOrderValue")]
        public decimal? MinOrderValue { get; set; }

        [JsonPropertyName("failureReason")]
        public string FailureReason { get; set; } = string.Empty;

        [JsonPropertyName("failedAt")]
        public DateTime FailedAt { get; set; } = DateTime.UtcNow;

        [JsonPropertyName("suggestionToFix")]
        public string SuggestionToFix { get; set; } = string.Empty;

        [JsonPropertyName("requiredAction")]
        public string RequiredAction { get; set; } = string.Empty;
    }
}