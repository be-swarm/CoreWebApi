using System.IO;
using System.Threading.Tasks;

namespace BeSwarm.CoreWebApi.Services.ConfigLoader
{
	// Get config from file
	public class ConfigFromFile : IConfigLoader
	{

		//
		// Summary
		// configspec:<file>
		//
		public async Task<string> GetConfig(string configspec)
		{
			string[] parts = configspec.Split("=");
			if (parts.Length !=1) return null;
			if (!File.Exists(configspec))
			{
				return null;
			}
			return await File.ReadAllTextAsync(configspec);
		}
	}
}
