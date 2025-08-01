using Domain.Entities.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dto.BannerEventSpecialDTOs
{
    public class RuleEvaluationResultDTO
    {
        public bool IsValid { get; set; }
        public List<string> Messages { get; set; } = new();
        public List<EventRule> FailedRules { get; set; } = new();
        public string SummaryMessage => IsValid ? "All rules passed" : "Some rules failed";

    }
}
