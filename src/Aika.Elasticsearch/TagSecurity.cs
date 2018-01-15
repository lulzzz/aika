using System;
using System.Collections.Generic;
using System.Text;

namespace Aika.Elasticsearch
{
    public class TagSecurity {

        public string Policy { get; set; }

        public TagSecurityEntry[] Allow { get; set; }

        public TagSecurityEntry[] Deny { get; set; }

    }

    public class TagSecurityEntry {

        public string ClaimType { get; set; }

        public string Value { get; set; }
    }

}
