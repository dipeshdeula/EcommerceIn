using Application.Dto;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Extension
{
    public static class MappingExtensions
    {
        public static UserDTO ToDTO(this User user)
        {
            return new UserDTO
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Contact = user.Contact,
                CreatedAt = user.CreatedAt,
                ImageUrl = user.ImageUrl,
                IsDeleted = user.IsDeleted,
                Addresses = user.Addresses.Select(a => a.ToDTO()).ToList()
            };
        }

        public static AddressDTO ToDTO(this Address address)
        {
            return new AddressDTO
            {
                Id = address.Id,
                Label = address.Label,
                Street = address.Street,
                City = address.City,
                Province = address.Province,
                PostalCode = address.PostalCode,
                Latitude = address.Latitude,
                Longitude = address.Longitude,
                IsDefault = address.IsDefault
            };
        }
    }
}
