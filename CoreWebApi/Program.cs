
using BeSwarm.CoreWebApi;

namespace CoreWebApi
{
	public class Program
	{
		public static void Main(string[] args)
		{

			var config = new ConfigurationBuilder()
				.AddCommandLine(args)
				.Build();
			string fromcommandline = config.GetValue<string>("config");
			string fromenv = Environment.GetEnvironmentVariable("ENV_CONFIG");
			if (fromenv != null)
			{
				Console.WriteLine("Config from ENV_CONFIG env variable");
			}
			else
			{
				Console.WriteLine("Config from command ligne prameter --config");
			}

			var builder = WebApplication.CreateBuilder(args);

			builder.Services.AddCoreWebApiServices(fromenv ?? fromcommandline);
			var app = builder.Build();
			app.ConfigureCoreWebApiApp();
			app.Run(CoreEnvironment.env.listen);
		}
	}
}