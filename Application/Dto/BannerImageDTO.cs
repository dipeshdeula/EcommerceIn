﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dto
{
    public class BannerImageDTO
    {
        public int Id { get; set; }
        public string ImageUrl { get; set; } = string.Empty;

        public int BannerEventId { get; set; }
    }
}
