using System;

namespace Newt.WebAPI.Models
{
    public class ApplicationConfigurationRequestModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }
    }
}