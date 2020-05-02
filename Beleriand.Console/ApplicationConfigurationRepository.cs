using System.Collections.Generic;
using System.Linq;

namespace Beleriand.Console
{
    public class ApplicationConfigurationRepository
    {
        public static List<ApplicationConfiguration> database = new List<ApplicationConfiguration>
        {
            new ApplicationConfiguration("MaximumPageNumber", "12"),
            new ApplicationConfiguration("MinimumPageNumber", "1")
        };

        public List<ApplicationConfiguration> GetByKeys(List<string> keys)
        {
            return database.Where(a => keys.Contains(a.Id.ToString())).ToList();
        }
    }
}