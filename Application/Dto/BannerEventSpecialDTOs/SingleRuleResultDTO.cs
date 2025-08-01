using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dto.BannerEventSpecialDTOs
{
    public class SingleRuleResultDTO
    {
        public bool IsValid { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
