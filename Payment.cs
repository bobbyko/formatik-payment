using System;
using System.Runtime.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Octagon.Formatik.Payment
{
    public class Payment
    {
        [IgnoreDataMember]
        public ObjectId _id { get; set; }

        [BsonIgnore]
        public string Token { get { return _id != ObjectId.Empty ? _id.ToString() : null; } }

        [IgnoreDataMember]
        public string Email { get; set; }

        public DateTime Expires { get; set; }
        [IgnoreDataMember]
        public DateTime Created { get; set; }

        [BsonIgnore]

        public string Status { get; set; } = "OK";

        [BsonIgnore]
        public string Error { get; set; }

        [BsonIgnore]
        public string ErrorCode { get; set; }

        public static Payment GetError(ErrorCode code, string error)
        {
            return new Payment()
            {
                Status = "Error",
                Error = error,
                ErrorCode = code.ToString()
            };
        }
    }
}