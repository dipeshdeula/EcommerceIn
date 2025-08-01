using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Enums.BannerEventSpecial
{
    public enum RuleType
    {
        Category = 0, // Electronics category
        SubCategory = 1, // Smartphones subCategory
        SubSubCategory = 2, // iphone models
        Product = 3, // Specific product like iPhone 14
        Brand = 4, //Apple,Samsung brand
        PriceRange = 5, // Price range like $100 - $500    
        Geography =  6, // Hetauda, Pokhara
        PaymentMethod = 7, // Credit Card, Cash on Delivery,digital wallet
        All = 8// No restrictions, applies to all products
    }
}
