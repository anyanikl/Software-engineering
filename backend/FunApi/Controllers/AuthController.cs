using FunApi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FunApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly FunDBcontext _context;

        public AuthController(FunDBcontext context)
        {
            _context = context;
        }


    }
}
