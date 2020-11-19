using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace tfgbot
{
    internal class Role
    {
        [BsonId]
        // ReSharper disable once UnusedMember.Global
        public MongoDB.Bson.ObjectId Id { get; set; }

        [BsonElement("messageid")]
        public string MessageId { get; set; }

        [BsonElement("roleid")]
        public string RoleId { get; set; }

        [BsonElement("emoji")]
        public string Emoji { get; set; }

        [BsonElement("removalid")]
        public string RemovalRoleId { get; set; }
    }
}
