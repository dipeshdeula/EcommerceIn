﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class Billing
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int PaymentId { get; set; }
        public int OrderId { get; set; }
        public int CompanyInfoId { get; set; }
        public DateTime BillingDate { get; set; } = DateTime.UtcNow;
        public bool IsDeleted { get; set; } = false;

        public User? User { get; set; }
        public PaymentRequest? PaymentRequest { get; set; }
        public Order? Order { get; set; }
        public CompanyInfo? CompanyInfo { get; set; }
        public ICollection<BillingItem> Items { get; set; } = new List<BillingItem>();

    }
}
