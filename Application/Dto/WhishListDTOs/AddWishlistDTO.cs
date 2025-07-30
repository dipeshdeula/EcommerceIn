using Application.Dto.ProductDTOs;
using Application.Dto.UserDTOs;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dto.WhishListDTOs
{
    public class AddWishlistDTO
    {
       
        public int UserId { get; set; }
        public int ProductId { get; set; }
       
    }
}
