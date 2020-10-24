using System;
using PipServices3.Commons.Data;

namespace PipServices3.Postgres.Persistence
{
    public class Dummy : IStringIdentifiable
    {
        public string Id { get; set; }
        public string Key { get; set; }
        public string Content { get; set; }
        public DateTime CreateTimeUtc { get; set; }
    }
}
