using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class BaseController : ControllerBase
	{
		[NonAction]
		protected string GetUserId()
		{
			var userId = HttpContext.User.Claims.FirstOrDefault(x => x.Type == "user_id")?.Value ?? "";
			return userId;
		}
		
		[NonAction]
		protected string GetUserGoogleToken()
		{
			var googleToken = HttpContext.User.Claims.FirstOrDefault(x => x.Type == "google_token")?.Value ?? "";
			return googleToken;
		}
	}
}
