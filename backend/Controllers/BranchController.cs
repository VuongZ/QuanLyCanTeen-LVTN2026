using LuanVanTotNghiep.backend.Models.Entities;
using LuanVanTotNghiep.Services;
using Microsoft.AspNetCore.Mvc;

namespace LuanVanTotNghiep.Controllers;

    [ApiController]
    [Route("api/[controller]")]
    public class BranchController (BranchService service) : ControllerBase
    {

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var branches = await service.GettAllBranchAsync();
            return Ok(branches);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var branch = await service.getBranchByIdAsync(id);
            if(branch == null) return NotFound();
            return Ok(branch);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] DmBranch branch)
        {
            await service.AddBranchAsync(branch);
            return Ok();
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] DmBranch branchInput)
        {
            var existingBranch = await service.getBranchByIdAsync(id);
            if(existingBranch == null) return NotFound();
            await service.UpdateBranchAsync(id, branchInput);
            return Ok();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var isDeleted = await service.DeletebranchAsync(id);
            if(!isDeleted)return NotFound();
            return Ok();
        }
    }

