using Application.Common;
using Application.Dto.StoreDTOs;
using Application.Extension;
using Application.Interfaces.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.StoreFeat.Queries
{
    public record GetAllStoreQuery (int PageNumber,int PageSize) : IRequest<Result<IEnumerable<StoreDTO>>>;

    public class GetAllStoreQueryHandler : IRequestHandler<GetAllStoreQuery, Result<IEnumerable<StoreDTO>>>
    {
        private readonly IStoreRepository _storeRepository;
        private readonly ILogger<GetAllStoreQuery> _logger;
        public GetAllStoreQueryHandler(IStoreRepository storeRepository,ILogger<GetAllStoreQuery> logger)
        {
            _storeRepository = storeRepository;
            _logger = logger;
            
        }
        public async  Task<Result<IEnumerable<StoreDTO>>> Handle(GetAllStoreQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Fetching all store info with pagination");

            var stores = await _storeRepository.GetAllAsync(
                predicate : null,
                orderBy: query => query.OrderByDescending(store => store.Id),
                skip: (request.PageNumber - 1) * request.PageSize,
                take: request.PageSize,
                includeDeleted:false,
                includeProperties:"Address"
                );
            var storeDTOs = stores.Select(s => s.ToDTO()).ToList();
            return Result<IEnumerable<StoreDTO>>.Success(storeDTOs, "Stores fetched successfully");
        }
    }
}
