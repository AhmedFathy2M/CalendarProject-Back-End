using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Core.Entities.IdentityEntities;
using Core.Interfaces.TokenValidationInterface;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Data.Repositories.Services
{
	public class TokenService:ITokenService
	{
		private readonly IConfiguration _config;
		private readonly SymmetricSecurityKey _key;
		public TokenService(IConfiguration config)
		{
			_config = config;
			_key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Token:Key"]));
		}

		public string CreateToken(AppUser user)
		{
			// first are the claims
			var claims = new List<Claim>
			{
			   new Claim (ClaimTypes.Email, user.Email),
			   new Claim ("user_id", user.Id),
			   new Claim (ClaimTypes.GivenName, user.DisplayName)
			};
			
			if (user.GoogleRefreshToken is not null)
				claims.Add(new Claim("google_token", user.GoogleRefreshToken));

			var creds = new SigningCredentials(_key, SecurityAlgorithms.HmacSha512Signature);

			var tokenDescriptor = new SecurityTokenDescriptor()
			{
				Subject = new ClaimsIdentity(claims),
				Expires = DateTime.Now.AddDays(7),
				SigningCredentials = creds,
				Issuer = _config["Token:Issuer"]
			};

			var tokenHandler = new JwtSecurityTokenHandler();
			var token = tokenHandler.CreateToken(tokenDescriptor);
			return tokenHandler.WriteToken(token);

		}
	}
}
