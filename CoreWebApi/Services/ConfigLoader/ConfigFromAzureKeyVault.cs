using System;
using System.IO;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

namespace BeSwarm.CoreWebApi.Services.ConfigLoader
{
	// Get config from AZURE KeyVault
	public class ConfigFromAzureKeyVault : IConfigLoader
	{

		//
		// Summary
		// configspec:AZKV=keyvault uri,keyvault id,client id, client secret,key
		//
		public async Task<string> GetConfig(string configspec)
		{
		    string[] parts = configspec.Split("=");
			if (parts.Length != 2) return null;
			if (parts[0] != "AZKV") return null;
			string[] param = parts[1].Split(",");
			if (param.Length != 5) return null;
			try
			{
				var client = new SecretClient(new Uri(param[0]),
				new ClientSecretCredential(tenantId: param[1], clientId: param[2], clientSecret: param[3]));
				var secret=client.GetSecret(param[4]);
				return secret.Value.Value;
			}
			catch (Exception e)
			{
				
			}
			return null;
		}
	}
}
