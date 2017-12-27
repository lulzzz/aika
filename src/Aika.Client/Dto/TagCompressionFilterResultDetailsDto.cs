using System;
using System.Collections.Generic;
using System.Text;

namespace Aika.Client.Dto {
    public class TagCompressionFilterResultDetailsDto {

        public bool Rejected { get; set; }

        public string Reason { get; set; }

        public TagValueDto LastArchivedValue { get; set; }

        public TagValueDto LastReceivedValue { get; set; }

        public TagValueFilterSettingsDto Settings { get; set; }

        public TagCompressionLimitsDto Limits { get; set; }

    }


    public class TagCompressionLimitsDto {

        public TagCompressionLimitSetDto Base { get; set; }

        public TagCompressionLimitSetDto Incoming { get; set; }

        public TagCompressionLimitSetDto Updated { get; set; }

    }


    public class TagCompressionLimitSetDto {

        public DateTime UtcSampleTime { get; set; }

        public double Minimum { get; set; }

        public double Maximum { get; set; }

    }

}
