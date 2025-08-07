using Application.Common;
using Application.Dto.LocationDTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.Services
{
    public interface IIPLocationService
    {
        Task<Result<IPLocationDTO>> GetLocationFromIPAsync(string ipAddress, CancellationToken cancellationToken = default);
        Task<Result<bool>> IsIPFromNepalAsync(string ipAddress, CancellationToken cancellationToken = default);
    }
}
