using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace tfgbot
{
    class Role
    {
        [BsonId]
        public MongoDB.Bson.ObjectId _id { get; set; }

        [BsonElement("messageid")]
        public string MessageID { get; set; }

        [BsonElement("roleid")]
        public string RoleID { get; set; }

        [BsonElement("emoji")]
        public string Emoji { get; set; }

        [BsonElement("removalid")]
        public string RemovalRoleID { get; set; }
    }
}
