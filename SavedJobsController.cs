using System.Security.Claims;
using AIRecruitment.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace AIRecruitment.Presentation.Controllers;

// ============== 
// TV2 — Candidate can save/unsave published jobs and view personal saved list.
// ==============
[ApiController,Route("api/saved-jobs"),Authorize(Roles="Candidate")]
public sealed class SavedJobsController(ISavedJobService service):ControllerBase
{
 [HttpGet] public async Task<IActionResult> Get(CancellationToken ct)=>Ok(await service.GetMineAsync(UserId(),ct));
 [HttpPost("{jobId:int}")] public async Task<IActionResult> Save(int jobId,CancellationToken ct)=>await service.SaveAsync(UserId(),jobId,ct)?Ok(new{jobId,saved=true}):NotFound(new{message="Không tìm thấy tin tuyển dụng đang mở."});
 [HttpDelete("{jobId:int}")] public async Task<IActionResult> Remove(int jobId,CancellationToken ct)=>await service.RemoveAsync(UserId(),jobId,ct)?NoContent():NotFound();
 private int UserId()=>int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
