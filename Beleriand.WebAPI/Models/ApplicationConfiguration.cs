using System;

namespace Newt.WebAPI.Models
{
    public class ApplicationConfiguration
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }

        public ApplicationConfiguration(string name, string value)
        {
            Id = Guid.NewGuid();
            Name = name;
            Value = value;
        }

        public void Update(string value)
        {
            Value = value;
        }
    }
}