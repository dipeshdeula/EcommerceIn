using Application.Interfaces.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Persistence.Repositories;

public class NotificationRepository : Repository<Notification>, INotificationRepository
{
    private readonly MainDbContext _context;

    public NotificationRepository(MainDbContext dbcontext) : base(dbcontext)
    {
        _context = dbcontext;

    }

}