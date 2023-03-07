using System;
using System.Linq;

namespace BeSwarm.CoreWebApi.Services.ConfigLoader
{
	public static class ConfigFactory
	{
		//
		// load config from definition file config
		//
		public static string GetConfiguration(string fromconfig)
		{

			var inter = AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes())
				.Where(x => typeof(IConfigLoader).IsAssignableFrom(x) && !x.IsInterface && !x.IsAbstract);
			string configcontent = null;

			foreach (var item in inter)
			{
				IConfigLoader instance = (IConfigLoader)Activator.CreateInstance(item);
				configcontent = instance?.GetConfig(fromconfig).Result;
				if (configcontent != null)return configcontent;
			}
			return null;
		}


	}
}
