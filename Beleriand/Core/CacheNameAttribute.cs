using System;

namespace Beleriand.Core
{
    public class CacheNameAttribute : Attribute
    {
        public string Name { get; set; }
    }
}