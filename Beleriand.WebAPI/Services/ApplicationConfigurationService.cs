using System;
using System.Collections.Generic;
using Beleriand.Core;
using Newt.WebAPI.Models;

namespace Beleriand.WebAPI.Services
{
    public class ApplicationConfigurationService : IApplicationConfigurationService
    {
        private readonly CacheBase _applicationConfigurationCache;

        private static List<ApplicationConfiguration> _database = new List<ApplicationConfiguration>();

        public ApplicationConfigurationService(MultiLevelCacheManager multiLevelCacheManager)
        {
            _applicationConfigurationCache = multiLevelCacheManager.GetCache("ApplicationConfiguration");
        }

        public ApplicationConfiguration Create(ApplicationConfigurationRequestModel requestModel)
        {
            var applicationConfiguration = new ApplicationConfiguration(requestModel.Name, requestModel.Value);

            _database.Add(applicationConfiguration);

            _applicationConfigurationCache.Set(applicationConfiguration.Id.ToString(), applicationConfiguration);

            return applicationConfiguration;
        }

        public ApplicationConfiguration Update(ApplicationConfigurationRequestModel requestModel)
        {
            var findItem = _database.Find(a => a.Id.Equals(requestModel.Id));

            if (findItem != null)
            {
                findItem.Update(requestModel.Value);
                _applicationConfigurationCache.Set(findItem.Id.ToString(), findItem);

                return findItem;
            }
            throw new Exception("Not Found !!!");
        }

        public IEnumerable<ApplicationConfiguration> GetAll()
        {
            return _database;
        }

        public ApplicationConfiguration GetById(string id)
        {
            return _applicationConfigurationCache.Get(id, s => _database.Find(a => a.Id.Equals(Guid.Parse(s))));
        }

        public void Delete(string id)
        {
            _applicationConfigurationCache.Remove(id);

            var index = _database.FindIndex(a => a.Id == Guid.Parse(id));
            if (index != -1)
            {
                _database.RemoveAt(index);
            }
        }

        public void InvalidateCache()
        {
            _applicationConfigurationCache.Clear();
        }
    }
}