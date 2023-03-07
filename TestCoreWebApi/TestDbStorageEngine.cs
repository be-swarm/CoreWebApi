using BeSwarm.CoreWebApi.Services.ConfigLoader;
using BeSwarm.CoreWebApi;
using BeSwarm.CoreWebApi.Services.DataBase;
using BeSwarm.WebApi.Core.DBStorage;
using BeSwarm.CoreWebApi.Services.Errors;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Priority;
using Newtonsoft.Json.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
using static TestCoreWebApi.TestDbStorageEngine;

namespace TestCoreWebApi
{
	[TestCaseOrderer(PriorityOrderer.Name, PriorityOrderer.Assembly)]

	public class TestDbStorageEngine : IClassFixture<TestFixture>
	{
		private SessionsMongoDB session=new();
		private readonly TestFixture _fixture;
		private IDBStorageBridge engine;
		[CollectionName("test")]
		public class Model
		{
			public long id { get; set; }
			public string name { get; set; }
		
		}
		Model model = new Model();
		public TestDbStorageEngine(TestFixture fixture)
		{
			_fixture = fixture;
			engine=_fixture.provider.GetRequiredService<IDBStorageBridge>();
			model.id = 1;
			model.name = "test";
		}
		[Theory, Priority(1)]
		[InlineData("testmongodb")]
		public async void TestAddOrUpdate(string sessionid)
		{
		
			Filters filters = new();
			await engine.Truncate(CollectionName.GetName(model),sessionid);
			var res = await engine.AddOrUpdateNode(model, filters, sessionid);
			Assert.Equal(StatusAction.ok, res.status);
	    }
		[Theory, Priority(2)]
		[InlineData("testmongodb")]
		public async void TestAddIfNotExist(string sessionid)
		{
			Filters filters = new();
			filters.Add("id", "=", 1);
			filters.Add("name", "=", "test");
			var res = await engine.AddIfNotExistNode(model, filters, sessionid);
			Assert.Equal(StatusAction.logicalerror, res.status);
		}
		[Theory, Priority(2)]
		[InlineData("testmongodb")]
		public async void TestGetOne(string sessionid)
		{
			
			Filters filters = new();
			filters.Add("id", "=", 1);
			filters.Add("name", "=", "test");
			var res = await engine.GetNode<Model>(CollectionName.GetName(model),filters, sessionid);
			Assert.Equal(StatusAction.ok, res.status);
			Assert.Equal(model.id, res.datas.id);
			Assert.Equal(model.name, res.datas.name);
		}
		[Theory, Priority(3)]
		[InlineData("testmongodb")]
		public async void TestGetOneNotFount(string sessionid)
		{

			Filters filters = new();
			filters.Add("id", "=", 2);
			filters.Add("name", "=", "test");
			var res = await engine.GetNode<Model>(CollectionName.GetName(model), filters, sessionid);
			Assert.Equal(StatusAction.notfound, res.status);
		}
		[Theory, Priority(4)]
		[InlineData("testmongodb")]
		public async void TestGetList(string sessionid)
		{

			Filters filters = new();
			filters.Add("id", "=", 1);
			filters.Add("name", "=", "test");
			var res = await engine.GetNodes<Model>(CollectionName.GetName(model), filters, sessionid);
			Assert.Equal(StatusAction.ok, res.status);
			Assert.Single(res.datas);
			Assert.Equal(model.id, res.datas[0].id);
			Assert.Equal(model.name, res.datas[0].name);
		}
		[Theory, Priority(4)]
		[InlineData("testmongodb")]
		public async void TestGetList2(string sessionid)
		{
			model.id = 2;
			model.name = "test";
			Filters filters = new();
			filters.Add("id", "=", 2);
			filters.Add("name", "=", "test");
			var resadd = await engine.AddOrUpdateNode(model, filters, sessionid);
			Assert.Equal(StatusAction.ok, resadd.status);
			Filters filters2 = new();
			filters2.Add("name", "=", "test");
			var res = await engine.GetNodes<Model>(CollectionName.GetName(model), filters2, sessionid);
			Assert.Equal(StatusAction.ok, res.status);
			Assert.Equal(2,res.datas.Count);
			Assert.Equal(1, res.datas[0].id);
			Assert.Equal(model.name, res.datas[0].name);
			Assert.Equal(2, res.datas[1].id);
			Assert.Equal(model.name, res.datas[1].name);

		}
		[Theory, Priority(5)]
		[InlineData("testmongodb")]
		public async void TestGetListNotFound(string sessionid)
		{

			Filters filters = new();
			filters.Add("id", "=", 3);
			filters.Add("name", "=", "test");
			var res = await engine.GetNodes<Model>(CollectionName.GetName(model), filters, sessionid);
			Assert.Equal(StatusAction.notfound, res.status);
		}
		[Theory, Priority(5)]
		[InlineData("testmongodb")]
		public async void TestGetCount(string sessionid)
		{

			Filters filters = new();
			filters.Add("id", "=", 1);
			filters.Add("name", "=", "test");
			var res = await engine.GetNodesCount(CollectionName.GetName(model),filters, sessionid);
			Assert.Equal(1, res.datas);
		}
	}
}