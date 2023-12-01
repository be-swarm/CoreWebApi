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
using Elasticsearch.Net.Specification.SnapshotApi;
using MongoDB.Bson;

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
            public string updated { get; set; }


        }
        Model model = new Model();
		public TestDbStorageEngine(TestFixture fixture)
		{
			_fixture = fixture;
			engine=_fixture.provider.GetRequiredService<IDBStorageBridge>();
			model.id = 1;
			model.name = "test";
            model.updated = "test";

        }
        [Theory, Priority(1)]
        [InlineData("testmongodb")]
        public async void TestAddIfNotExistAdded(string sessionid)
        {
            Filters filters = new();
            filters.Add("id", "=", 1);
            filters.Add("name", "=", "test");
            await engine.Truncate(CollectionName.GetName(model), sessionid); 
			var res = await engine.AddIfNotExist(model, filters, sessionid);
            var res2 = await engine.GetItems<Model>(CollectionName.GetName(model), filters, sessionid);
            Assert.Equal(StatusAction.ok, res.status);
            Assert.Equal(StatusAction.ok, res2.status);
            Assert.Single(res2.datas);
        }
        [Theory, Priority(2)]
        [InlineData("testmongodb")]
        public async void TestAddIfNotExistNotAdded(string sessionid)
        {
            Filters filters = new();
            filters.Add("id", "=", 1);
            filters.Add("name", "=", "test");
            var res = await engine.AddIfNotExist(model, filters, sessionid);
            var res2 = await engine.GetItems<Model>(CollectionName.GetName(model), filters, sessionid);
            Assert.Equal(StatusAction.ok, res.status);
            Assert.Equal(StatusAction.ok, res2.status);
            Assert.Single(res2.datas);
        }
        [Theory, Priority(3)]
		[InlineData("testmongodb")]
		public async void TestUpdateOK(string sessionid)
		{
		
			Filters filters = new();
            filters.Add("id", "=", 1);
            model.updated = "updated";
			var res = await engine.Update(model, filters, sessionid);
            var res2 = await engine.GetItem<Model>(CollectionName.GetName(model), filters, sessionid);
            Assert.Equal(StatusAction.ok, res.status);
            Assert.Equal(StatusAction.ok, res2.status);
            Assert.Equal(model.updated, res2.datas.updated);
        }
        [Theory, Priority(5)]
        [InlineData("testmongodb")]
        public async void TestUpdateNotFound(string sessionid)
        {
            Filters filters = new();
            filters.Add("id", "=", -1);
            var res = await engine.Update(model, filters, sessionid);
            filters = new();
            var res2 = await engine.GetItems<Model>(CollectionName.GetName(model), filters, sessionid);
            Assert.Equal(StatusAction.ok, res.status);
            Assert.Equal(StatusAction.ok, res2.status);
            Assert.Single(res2.datas);
        }
        [Theory, Priority(6)]
        [InlineData("testmongodb")]
        public async void TestAddOrUpdate(string sessionid)
        {
            int id = -100;
            Filters filters = new();
            filters.Add("id", "=",id );
            Model m = new() { id = id, name = "test3" };
            var res = await engine.AddOrUpdate(m, filters, sessionid);
            var res2 = await engine.GetItem<Model>(CollectionName.GetName(model), filters, sessionid);
            Assert.Equal(StatusAction.ok, res.status);
            Assert.Equal(StatusAction.ok, res2.status);
            Assert.Equal(m.name, res2.datas.name);
            m.name = "test4";
            res = await engine.AddOrUpdate(m, filters, sessionid);
            res2 = await engine.GetItem<Model>(CollectionName.GetName(model), filters, sessionid);
            Assert.Equal(StatusAction.ok, res.status);
            Assert.Equal(StatusAction.ok, res2.status);
            Assert.Equal(m.name, res2.datas.name);
        }


        [Theory, Priority(10)]
		[InlineData("testmongodb")]
		public async void TestGetOne(string sessionid)
		{
			
			Filters filters = new();
			filters.Add("id", "=", 1);
		    var res = await engine.GetItem<Model>(CollectionName.GetName(model),filters, sessionid);
			Assert.Equal(StatusAction.ok, res.status);
			Assert.Equal(model.id, res.datas.id);
			Assert.Equal(model.name, res.datas.name);
		}
		[Theory, Priority(11)]
		[InlineData("testmongodb")]
		public async void TestGetOneNotFound(string sessionid)
		{

			Filters filters = new();
			filters.Add("id", "=", 2);
			var res = await engine.GetItem<Model>(CollectionName.GetName(model), filters, sessionid);
			Assert.Equal(StatusAction.notfound, res.status);
		}
		[Theory, Priority(13)]
		[InlineData("testmongodb")]
		public async void TestGetList(string sessionid)
		{

			Filters filters = new();
			filters.Add("id", "=", 1);
			var res = await engine.GetItems<Model>(CollectionName.GetName(model), filters, sessionid);
			Assert.Equal(StatusAction.ok, res.status);
			Assert.Single(res.datas);
			Assert.Equal(model.id, res.datas[0].id);
			Assert.Equal(model.name, res.datas[0].name);
		}
		[Theory, Priority(14)]
		[InlineData("testmongodb")]
		public async void TestGetList2(string sessionid)
		{
			model.id = 2;
			model.name = "test";
			Filters filters = new();
			filters.Add("id", "=", 2);
			var resadd = await engine.AddIfNotExist(model, filters, sessionid);
			Assert.Equal(StatusAction.ok, resadd.status);
			Filters filters2 = new();
			filters2.Add("name", "=", "test");
			var res = await engine.GetItems<Model>(CollectionName.GetName(model), filters2, sessionid);
			Assert.Equal(StatusAction.ok, res.status);
			Assert.Equal(2,res.datas.Count);
			Assert.Equal(1, res.datas[0].id);
			Assert.Equal(model.name, res.datas[0].name);
			Assert.Equal(2, res.datas[1].id);
			Assert.Equal(model.name, res.datas[1].name);

		}
		[Theory, Priority(15)]
		[InlineData("testmongodb")]
		public async void TestGetListNotFound(string sessionid)
		{

			Filters filters = new();
			filters.Add("id", "=", 3);
			var res = await engine.GetItems<Model>(CollectionName.GetName(model), filters, sessionid);
			Assert.Equal(StatusAction.notfound, res.status);
		}

        [Theory, Priority(16)]
        [InlineData("testmongodb")]
        public async void TestAddMultiple(string sessionid)
        {

          
            await engine.Truncate(CollectionName.GetName(model), sessionid);
            await engine.Add(model, sessionid);
            await engine.Add(model, sessionid);
            var res = await engine.GetItems<Model>(CollectionName.GetName(model), new(), sessionid);
            Assert.Equal(StatusAction.ok, res.status);
            Assert.Equal(2, res.datas.Count);
        }



        [Theory, Priority(20)]
		[InlineData("testmongodb")]
		public async void TestTransactionOK(string sessionid)
		{

			Filters filters = new();
			filters.Add("id", "=", 1);
			filters.Add("name", "=", "test");
            await engine.Truncate(CollectionName.GetName(model), sessionid);

            using (var transaction = await  engine.BeginTransaction(sessionid))
			{
                await engine.AddIfNotExist(model, filters, sessionid);
				model.id = 2;
				model.name = "test2";
                Filters filters2 = new();
                filters2.Add("id", "=", 2);
                filters.Add("name", "=", "test2");
                await engine.AddIfNotExist(model, filters2, sessionid);
                await transaction.CommitTransaction();
            }
            Filters filters3 = new();
            var res = await engine.GetCount(CollectionName.GetName(model),filters3, sessionid);
			Assert.Equal(2, res.datas);
		}
        [Theory, Priority(21)]
        [InlineData("testmongodb")]
        public async void TestTransactionAbort(string sessionid)
        {

            Filters filters = new();
            filters.Add("id", "=", 1);
            filters.Add("name", "=", "test");
            await engine.Truncate(CollectionName.GetName(model), sessionid);

            using (var transaction = await engine.BeginTransaction(sessionid))
            {
                await engine.AddIfNotExist(model, filters, sessionid);
                model.id = 2;
                model.name = "test2";
                Filters filters2 = new();
                filters2.Add("id", "=", 2);
                filters.Add("name", "=", "test2");
                await engine.AddIfNotExist(model, filters2, sessionid);
                await transaction.AbortTransaction();
            }
            Filters filters3 = new();
            var res = await engine.GetCount(CollectionName.GetName(model), filters3, sessionid);
            Assert.Equal(0, res.datas);
        }

    }
}