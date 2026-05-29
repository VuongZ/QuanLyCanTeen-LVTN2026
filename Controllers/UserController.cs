using LuanVanTotNghiep.DTOs;
using LuanVanTotNghiep.Models.Entities;
using LuanVanTotNghiep.Repositories;
using LuanVanTotNghiep.Services;
using Microsoft.AspNetCore.Mvc;

namespace LuanVanTotNghiep.Controllers;
    [ApiController]
     [Route("api/[controller]")] 
public class UserController (UserService userService,
    RoleService roleService,
    BranchService branchService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAllUser()
    {
        var users    = await userService.GetAllUser();
        var roles    = await roleService.GetAllRole();
       var result = new UserPageDataDto
    {
        Users = users.Select(u => new UserDto
        {
            Id         = u.Id,
            Username   = u.Username,
            FullName   = u.FullName,
            BranchId   = u.BranchId,
            BranchName = u.Branch?.Name,
            RoleId     = u.RoleId,
            RoleName   = u.Role?.RoleName,
            HireDate   = u.HireDate
        }),
        Roles    = roles.Select(r => new RoleDto
        {
            Id          = r.Id,
            RoleName    = r.RoleName,
            Description = r.Description,
            HourlyWage  = r.HourlyWage,
            SeniorWage  = r.SeniorWage
        }),
  
    };
        return Ok(result);
    }
    [HttpGet("{id}")]
       public async Task<IActionResult> GetUserById(int id)
    {
        var user= await userService.GettUserbyId(id);
        if(user == null)
            return NotFound();
        return Ok(user);
    }
     [HttpPost]
        public async Task<IActionResult> AddUser(NsUser user)
    {
         await userService.AddUser(user);
        return Ok(user);
    }
      [HttpPut("{id}")]
      public async Task<IActionResult> UpdateUser(int id,NsUser user)
    {
        if(id != user.Id)
            return BadRequest("ID Khong Ton Tai");
            await userService.UpdateUser(user);
      
            return NoContent();
    }
    [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSinhVien(int id)
        {
           await  userService.DeleteUser(id);
            return NoContent();
        }

}