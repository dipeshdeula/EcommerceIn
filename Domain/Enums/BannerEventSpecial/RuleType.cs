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
        PriceRange = 4, // Price range like $100 - $500    
        Geography =  5, // Hetauda, Pokhara
        PaymentMethod = 6, // Credit Card, Cash on Delivery,digital wallet
        All = 7 // No restrictions, applies to all products
    }
}
