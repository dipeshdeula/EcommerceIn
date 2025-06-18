using Application.Interfaces.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Persistence.Repositories
{
    public class CompanyInfoRepository : Repository<CompanyInfo>, ICompanyInfoRepository
    {
        private readonly MainDbContext _context;
        private readonly IFileServices _fileService;
        public CompanyInfoRepository(MainDbContext context, IFileServices fileService) : base(context)
        {
            _context = context;
            _fileService = fileService;
            
        }
    }
}
