using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.Repositories
{
    public interface ICartItemRepository : IRepository<CartItem>
    {
        public Task LoadNavigationProperties(CartItem cartItem);
        public Task<IEnumerable<CartItem>> GetByUserIdAsync(int userId);
        public Task DeleteByUserIdAsync(int userId);
        Task<int> CountActiveCartItemsByEventAsync(int userId, int eventId,int productId);

        
    }
}
