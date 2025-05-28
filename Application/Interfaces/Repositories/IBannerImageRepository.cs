using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.Repositories
{
    public interface IBannerImageRepository : IRepository<BannerImage>
    {
        Task AddRangeAsync(IEnumerable<BannerImage> productImages, CancellationToken cancellationToken = default);

    }
}
