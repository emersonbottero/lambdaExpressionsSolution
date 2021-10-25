using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace lambdaExpressions
{
    internal class User
    {
        [JsonPropertyName("sys_id")]
        public Guid Id { get; set; }
        [JsonPropertyName("u_age")]
        public int Age { get; set; }
        public string Name { get; set; }
    }
}
