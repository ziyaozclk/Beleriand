using System.Collections.Generic;
using Newt.WebAPI.Models;

namespace Beleriand.WebAPI.Services
{
    public interface IApplicationConfigurationService
    {
        ApplicationConfiguration Create(ApplicationConfigurationRequestModel requestModel);
        ApplicationConfiguration Update(ApplicationConfigurationRequestModel requestModel);
        IEnumerable<ApplicationConfiguration> GetAll();
        ApplicationConfiguration GetById(string id);
        void Delete(string id);
        void InvalidateCache();
    }
}