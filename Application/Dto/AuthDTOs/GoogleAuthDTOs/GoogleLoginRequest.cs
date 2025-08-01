using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dto.AuthDTOs.GoogleAuthDTOs
{
    public class GoogleLoginRequest
    {
        public string IdToken { get; set; } = string.Empty;
        public string ClientType { get; set; } = "web"; // web, android, ios
    }
}
