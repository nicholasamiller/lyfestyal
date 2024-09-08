using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Franz
{
    public class Rule
    {
        [JsonPropertyName("rule")]
        public string RuleTextFile { get; set; }

        [JsonPropertyName("source")]
        public string RuleSourceReference { get; set; }

        public Rule(string ruleTextFile, string ruleSourceReference)
        {
            RuleTextFile = ruleTextFile;
            RuleSourceReference = ruleSourceReference;
        }
    }
}
