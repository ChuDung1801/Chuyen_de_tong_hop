using System.Security.Claims;
using AIRecruitment.Application.DTOs;
using AIRecruitment.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace AIRecruitment.Presentation.Controllers;

// ============== 
// TV3 — Public search + recruiter/admin CRUD with ownership validation in service layer.
// ==============
[ApiController, Route("api/jobs")]
public sealed class JobsController(ISprint3JobService service) : ControllerBase
{
 [HttpGet] public async Task<IActionResult> Search([FromQuery]string? search,[FromQuery]string? location,[FromQuery]string? employmentType,[FromQuery]string? skill,[FromQuery]int page=1,[FromQuery]int pageSize=10,CancellationToken ct=default) => Ok(await service.SearchAsync(search,location,employmentType,skill,page,pageSize,ct));
 [HttpGet("{id:int}")] public async Task<IActionResult> Get(int id,CancellationToken ct){var j=await service.GetPublicAsync(id,ct);return j is null?NotFound():Ok(j);}
 [HttpGet("mine"),Authorize(Roles="Recruiter,Admin")] public async Task<IActionResult> Mine(CancellationToken ct)=>Ok(await service.GetMineAsync(UserId(),IsAdmin(),ct));
 [HttpPost,Authorize(Roles="Recruiter,Admin")] public async Task<IActionResult> Create([FromBody]CreateRecruitmentJobRequest request,CancellationToken ct){try{var j=await service.CreateAsync(UserId(),request,ct);return CreatedAtAction(nameof(Get),new{id=j.Id},j);}catch(ArgumentException e){return BadRequest(new{message=e.Message});}catch(UnauthorizedAccessException e){return ForbidWithMessage(e.Message);}}
 [HttpPut("{id:int}"),Authorize(Roles="Recruiter,Admin")] public async Task<IActionResult> Update(int id,[FromBody]UpdateRecruitmentJobRequest request,CancellationToken ct){try{var j=await service.UpdateAsync(UserId(),IsAdmin(),id,request,ct);return j is null?NotFound():Ok(j);}catch(ArgumentException e){return BadRequest(new{message=e.Message});}catch(UnauthorizedAccessException e){return ForbidWithMessage(e.Message);}}
 [HttpDelete("{id:int}"),Authorize(Roles="Recruiter,Admin")] public async Task<IActionResult> Delete(int id,CancellationToken ct){try{return await service.DeleteAsync(UserId(),IsAdmin(),id,ct)?NoContent():NotFound();}catch(UnauthorizedAccessException e){return ForbidWithMessage(e.Message);}}
 private int UserId()=>int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!); private bool IsAdmin()=>User.IsInRole("Admin");
 private ObjectResult ForbidWithMessage(string message)=>StatusCode(StatusCodes.Status403Forbidden,new{message});
}
