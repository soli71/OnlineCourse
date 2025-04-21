using Microsoft.AspNetCore.Mvc;
using OnlineCourse.Controllers.Site;

namespace OnlineCourse.Controllers.Panel;

public class BaseController : ControllerBase
{

    protected IActionResult OkB(object value)
    {
        return base.Ok(new ApiResult(true, "", value,200));
    }

    protected IActionResult OkB()
    {
        return base.Ok(new ApiResult(true, "", null,200));
    }

    protected IActionResult BadRequestB(string message)
    {
        return base.BadRequest(new ApiResult(false, message, null,400));
    }

    protected IActionResult NotFoundB(string message)
    {
        return base.NotFound(new ApiResult(false, message, null,404));
    }



}
