using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Enums
{
    public enum PaymentMethodType
    {
        [Description("Digital Payment Methods")]
        DigitalPayments = 1,

        [Description("Cash On Delivery")]
        COD = 2, // Cash on Delivery
        
    }

    public enum DigitalPaymentProvider
    {
        [Description("eSewa Digital Payment")]
        Esewa = 1,

        [Description("Khalti DigitalPayment")]
        Khalti = 2,

        [Description("IME Pay Digital Payment")]
        ImePay = 3
    }

    public enum CODPaymentType
    {
        [Description("Standard Cash on Delivery")]
        Standard = 1,

        [Description("Card Payment on Delivery")]
        CardOnDelivery = 2
    }
}
