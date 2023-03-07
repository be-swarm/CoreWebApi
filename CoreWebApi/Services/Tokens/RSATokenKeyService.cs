using System;

using System.Collections.Generic;
using BeSwarm.CoreWebApi.Services.ConfigLoader;
using Newtonsoft.Json;

namespace BeSwarm.CoreWebApi.Services.Tokens
{

    public class TokenRSAKey
    {
        public string indice { get; set; }
        public string key { get; set; }
    }
	public class TokenRSAKeys
	{
		public TokenRSAKey privatekey { get; set; }
		public List<TokenRSAKey> publickeys { get; set; }

		public TokenRSAKeys(string configfile)
		{
			if (configfile is null) return;   
			var configcontent = ConfigFactory.GetConfiguration(configfile);
			if (configcontent is null)
			{
				throw new System.Exception("token keys file not found or incorrect");
			}

			try
			{
				var config = JsonConvert.DeserializeObject<TokenRSAKeys>(configcontent);
				privatekey = config.privatekey;
				publickeys = config.publickeys;
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
				throw;
			}
		}
		public TokenRSAKey GetCurrentPrivateKey()
		{
			return privatekey;
		}

		public string GetPublicKey(string indice)
		{
			foreach (var item in publickeys)
			{
				if (item.indice == indice) return item.key;
			}
			return null;
		}

	}

}
