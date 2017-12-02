using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Octagon.Formatik.Payment
{
    public class PaymentException
    {
        public readonly string Application = "Payment";

        public ObjectId _id { get; set; }
        public string Method { get; set; }
        public string Request { get; set; }
        public string Body { get; set; }
        public string Headers { get; set; }
        public string UserAddress { get; set; }
        public string Exception { get; set; }
        public string StackTrace { get; set; }
        public DateTime Timestamp { get; set; }
        public string Process { get; set; }
    }
}