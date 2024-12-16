using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using OnlineCourse.Extensions;

namespace OnlineCourse.Controllers.Panel
{

    [Route("api/panel/[controller]")]
    [Authorize(Roles = "Admin")]
    [ApiController]
    public class CacheController : BaseController
    {
       private readonly IOutputCacheStore _outputCacheStore;
        public CacheController(IOutputCacheStore outputCacheStore)
        {
            _outputCacheStore = outputCacheStore;
        }
        [HttpDelete]
        public async Task<IActionResult> ClearCacheAsync(CancellationToken cancellationToken)
        {
           await _outputCacheStore.EvictByTagAsync(CacheTag.General, cancellationToken);
            return OkB();
        }
    }
}
