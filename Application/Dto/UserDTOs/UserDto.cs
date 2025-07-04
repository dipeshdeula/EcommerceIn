﻿using Application.Dto.AddressDTOs;
using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dto.UserDTOs
{
    public class UserDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Contact { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string ImageUrl { get; set; } = string.Empty;
        public bool IsDeleted { get; set; } = false;

        public UserRoles Role { get; set; }

        public ICollection<AddressDTO> Addresses { get; set; } = new List<AddressDTO>();
    }

}
