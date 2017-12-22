using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aika;

namespace Aika.AspNetCore.Models.Query {
    public class TagDefinitionDto {

        public string Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public string Units { get; set; }

        public TagDataType DataType { get; set; }

        public string StateSet { get; set; }


        public TagDefinitionDto() { }


        internal TagDefinitionDto(TagDefinition tagDefinition) {
            if (tagDefinition == null) {
                throw new ArgumentNullException(nameof(tagDefinition));
            }

            Id = tagDefinition.Id;
            Name = tagDefinition.Name;
            Description = tagDefinition.Description;
            Units = tagDefinition.Units;
            DataType = tagDefinition.DataType;
            StateSet = tagDefinition.StateSet;
        }

    }
}
