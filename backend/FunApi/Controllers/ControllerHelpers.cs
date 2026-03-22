using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FunApi.Controllers
{
    internal static class ControllerHelpers
    {
        public static int? GetCurrentUserId(ControllerBase controller)
        {
            var claim = controller.User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? controller.User.FindFirstValue("Id");

            return int.TryParse(claim, out var userId) ? userId : null;
        }
    }
}
