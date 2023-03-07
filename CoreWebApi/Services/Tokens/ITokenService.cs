
using BeSwarm.WebApi.Core;
using System.Threading.Tasks;

namespace BeSwarm.CoreWebApi.Services.Tokens
{

	public class TokenResult
	{
		public string token_type { get; set; }
		public string id_token { get; set; }
		public string expires_in { get; set; }
		public string expires_on { get; set; }
	                                   
		public string GetAuthorization()
		{
			return token_type + " " + id_token;
		}

	}
	public interface ITokenService
    {
        Task<ResultAction<Dictionary<string,string>>> GetToken(string token);
		Task<ResultAction<TokenResult>> CreateToken(Dictionary<string,string> claims,int ttl);

	}
}
