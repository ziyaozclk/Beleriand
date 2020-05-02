using System.Collections.Generic;
using Beleriand.WebAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newt.WebAPI.Models;

namespace Beleriand.WebAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ApplicationConfigurationController : ControllerBase
    {
        private readonly ILogger<ApplicationConfigurationController> _logger;
        private readonly IApplicationConfigurationService _applicationConfigurationService;

        public ApplicationConfigurationController(IApplicationConfigurationService applicationConfigurationService,
            ILogger<ApplicationConfigurationController> logger)
        {
            _logger = logger;
            _applicationConfigurationService = applicationConfigurationService;
        }

        [HttpPost]
        public ApplicationConfiguration Create(ApplicationConfigurationRequestModel createModel)
        {
            return _applicationConfigurationService.Create(createModel);
        }

        [HttpPut]
        public ApplicationConfiguration Update(ApplicationConfigurationRequestModel updateModel)
        {
            return _applicationConfigurationService.Update(updateModel);
        }

        [HttpGet("id/{id}")]
        public ApplicationConfiguration Get(string id)
        {
            return _applicationConfigurationService.GetById(id);
        }

        [HttpGet]
        public IEnumerable<ApplicationConfiguration> GetAll()
        {
            return _applicationConfigurationService.GetAll();
        }

        [HttpDelete("id/{id}")]
        public void Delete(string id)
        {
            _applicationConfigurationService.Delete(id);
        }

        [HttpGet("invalidateAll")]
        public void InvalidateAll()
        {
            _applicationConfigurationService.InvalidateCache();
        }
    }
}