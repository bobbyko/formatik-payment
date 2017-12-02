using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using Stripe;

namespace Octagon.Formatik.Payment
{
    [Route("")]
    public class PaymentController : Controller
    {
        private readonly StripeSettings stripeSettings;
        private readonly FormatikSettings formatikSettings;

        private readonly ILogger<PaymentController> logger;

        public PaymentController(IOptions<StripeSettings> stripeConfig, IOptions<FormatikSettings> formatikConfig, ILogger<PaymentController> logger)
        {
            this.stripeSettings = stripeConfig.Value;
            this.formatikSettings = formatikConfig.Value;
            this.logger = logger;
        }

        [HttpPost]
        public Payment Charge(string stripeEmail, string stripeToken)
        {
            // check for active tokens
            var db = Common.GetDB(formatikSettings.DbConnection);

            var existingPayment = db
                .GetCollection<Payment>("Payments")
                .Find(Builders<Payment>.Filter.And(
                    Builders<Payment>.Filter.Eq(p => p.Email, stripeEmail.ToLower()),
                    Builders<Payment>.Filter.Gte(p => p.Expires, DateTime.Now)))
                .Limit(1)
                .First();

            if (existingPayment == null)
            {
                var customers = new StripeCustomerService();
                var charges = new StripeChargeService();

                var customer = customers.Create(new StripeCustomerCreateOptions
                {
                    Email = stripeEmail,
                    SourceToken = stripeToken
                });

                var charge = charges.Create(new StripeChargeCreateOptions
                {
                    Amount = 100,
                    Description = "3 day unlimited Formatik",
                    Currency = "usd",
                    CustomerId = customer.Id
                });

                if (charge != null)
                {
                    var payment = new Payment() {
                        _id = ObjectId.GenerateNewId(),
                        Email = stripeEmail,
                        Expires = DateTime.Now.AddDays(3),
                        Created = DateTime.Now
                    };

                    db
                        .GetCollection<Payment>("Payments")
                        .InsertOne(payment);

                    return payment;
                }
                else
                    return Payment.GetError(ErrorCode.PaymentError, "Failed to process payment");
            }
            else
            {
                return new Payment() {
                    _id = existingPayment._id,
                    Expires = existingPayment.Expires,
                    ErrorCode = ErrorCode.DuplicatePayment.ToString(),
                    Error = "Duplicate Payment"
                };
            }
        }
    }
}
