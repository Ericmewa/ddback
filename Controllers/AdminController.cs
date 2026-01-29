using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using NCBA.DCL.DTOs;
using NCBA.DCL.Services;

namespace NCBA.DCL.Controllers
{
    [ApiController]
    [Route("api/admin")]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;

        public AdminController(IAdminService adminService)
        {
            _adminService = adminService;
        }

        // POST: /api/admin/auth/register
        [HttpPost("auth/register")]
        public async Task<IActionResult> RegisterAdmin([FromBody] RegisterAdminDto dto)
        {
            var result = await _adminService.RegisterAdminAsync(dto);
            return StatusCode(result.StatusCode, result.Body);
        }

        // POST: /api/admin/auth/login
        [HttpPost("auth/login")]
        public async Task<IActionResult> LoginAdmin([FromBody] LoginAdminDto dto)
        {
            var result = await _adminService.LoginAdminAsync(dto);
            return StatusCode(result.StatusCode, result.Body);
        }

        // POST: /api/admin/create
        [HttpPost("create")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserDto dto)
        {
            var result = await _adminService.CreateUserAsync(dto);
            return StatusCode(result.StatusCode, result.Body);
        }

        // PUT: /api/admin/toggle/{id}
        [HttpPut("toggle/{id}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> ToggleActive(string id)
        {
            var result = await _adminService.ToggleActiveAsync(id);
            return StatusCode(result.StatusCode, result.Body);
        }

        // PUT: /api/admin/archive/{id}
        [HttpPut("archive/{id}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> ArchiveUser(string id)
        {
            var result = await _adminService.ArchiveUserAsync(id);
            return StatusCode(result.StatusCode, result.Body);
        }

        // PUT: /api/admin/transfer/{id}
        [HttpPut("transfer/{id}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> TransferRole(string id, [FromBody] TransferRoleDto dto)
        {
            var result = await _adminService.TransferRoleAsync(id, dto);
            return StatusCode(result.StatusCode, result.Body);
        }

        // POST: /api/admin/reassign/{id}
        [HttpPost("reassign/{id}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> ReassignTasks(string id, [FromBody] ReassignTasksDto dto)
        {
            var result = await _adminService.ReassignTasksAsync(id, dto);
            return StatusCode(result.StatusCode, result.Body);
        }
    }
}
