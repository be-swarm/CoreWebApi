using System.Threading.Tasks;

namespace BeSwarm.CoreWebApi.Services.ConfigLoader
{
	public interface IConfigLoader
	{
		Task<string> GetConfig(string configspec);
	}
}
