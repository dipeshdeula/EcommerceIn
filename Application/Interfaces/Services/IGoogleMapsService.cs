using Application.Common;
using Application.Dto.LocationDTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.Services
{
    public interface IGoogleMapsService
    {
        Task<Result<LocationRequestDTO>> ReverseGeocodeAsync(double latitude, double longitude, CancellationToken cancellationToken = default);
        Task<Result<LocationRequestDTO>> ForwardGeocodeAsync(string address, CancellationToken cancellationToken = default);
        Task<Result<double>> CalculateDistanceAsync(double lat1, double lon1, double lat2, double lon2, CancellationToken cancellationToken = default);
        Task<Result<bool>> ValidateAddressAsync(string address, CancellationToken cancellationToken = default);
    }
}
