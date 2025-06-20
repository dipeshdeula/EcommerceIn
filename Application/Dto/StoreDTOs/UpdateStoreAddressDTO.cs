﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dto.StoreDTOs
{
    public class UpdateStoreAddressDTO
    {
       public string? Street { get; set; }
       public string? City { get; set; }
       public string? Province { get; set; } 
       public string? PostalCode { get; set; }
       public double? Latitude { get; set; }
       public double? Longitude { get; set; }
    }
}
