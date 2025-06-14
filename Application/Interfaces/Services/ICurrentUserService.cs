using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.Services
{
    public interface ICurrentUserService
    {
        public string? UserId { get; }
        public string? UserEmail { get; }
        public string? UserName { get; }
        public string? GetUserIp { get; }
        public bool IsAuthenticated { get; }

    }
}
