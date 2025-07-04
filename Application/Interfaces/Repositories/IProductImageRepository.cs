﻿using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.Repositories
{
    public interface IProductImageRepository : IRepository<ProductImage>
    {
        Task AddRangeAsync(IEnumerable<ProductImage> productImages, CancellationToken cancellationToken = default);
    }
}
