using Core.Entities.IdentityEntities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Interfaces.TokenValidationInterface
{
	public interface ITokenService
	{
		string CreateToken(AppUser user);
	}
}
