using FunApi.Interfaces;
using FunDto.Models.Contracts.Config;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FunApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous]
    public class ConfigController : ControllerBase
    {
        private readonly IAppConfigService _service;

        public ConfigController(IAppConfigService service)
        {
            _service = service;
        }

        [HttpGet]
        [IgnoreAntiforgeryToken]
        public async Task<ActionResult<AppConfigDto>> Get(CancellationToken cancellationToken)
        {
            return Ok(await _service.GetAsync(cancellationToken));
        }
    }
}
