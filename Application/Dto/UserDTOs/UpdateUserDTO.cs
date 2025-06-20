using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dto.UserDTOs
{
    public class UpdateUserDTO
    {
      public string? Name { get; set; }
      public string? Email { get; set; }
      public string? Password { get; set; }
      public string? Contact { get; set; }
    }
}
