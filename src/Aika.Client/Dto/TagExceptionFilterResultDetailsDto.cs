using System;
using System.Collections.Generic;
using System.Text;

namespace Aika.Client.Dto {
    public class TagExceptionFilterResultDetailsDto {
        public bool Rejected { get; set; }

        public string Reason { get; set; }

        public TagValueDto LastExceptionValue { get; set; }

        public TagValueFilterSettingsDto Settings { get; set; }

        public TagExceptionLimitSetDto Limits { get; set; }
    }


    public class TagExceptionLimitSetDto {
        
        public double Minimum { get; set; }

        public double Maximum { get; set; }

    }
}
