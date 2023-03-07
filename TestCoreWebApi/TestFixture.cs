using BeSwarm.CoreWebApi.Services.DataBase;
using BeSwarm.CoreWebApi;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;

namespace TestCoreWebApi
{
	public class TestFixture : IDisposable
	{
		public ServiceProvider provider;
		
		public TestFixture()
		{
			Init();
		}

		private void Init()
		{
			var builder = WebApplication.CreateBuilder();
			builder.Services.AddCoreWebApiServices(@"D:\Developpement\beswarm\Common\CoreWebApi\TestCoreWebApi\config.json");
			provider = CoreEnvironment.services.BuildServiceProvider();


		}
		public void Dispose()
		{
		}
	}
}
