using Elasticsearch.Net;

using Newtonsoft.Json.Linq;

using System.Collections;
using System.Collections.Generic;
using System.Xml.Linq;

namespace BeSwarm.CoreWebApi.Services.DataBase
{
	public class DBStorageBridge : IDBStorageBridge
	{
		private IEnumerable<IDBStorageEngine> engines;
		public DBStorageBridge(IEnumerable<IDBStorageEngine> _engines)
		{
			engines = _engines;
		}

		async Task<ResultAction> IDBStorageBridge.AddIfNotExist<T>(T node, Filters query, string sessionid)
		{
			ResultAction res; ;
			ResultAction<IDBStorageEngine> resdbengine = await GetStorageEngine(sessionid);
			if (!resdbengine.IsOk)
			{
				res = new();
				res.CopyStatusFrom(resdbengine);
				return res;
			}

			return await resdbengine.datas.AddIfNotExist(node, query,sessionid);
		}
        async Task<ResultAction> IDBStorageBridge.Add<T>(T node,string sessionid)
        {
            ResultAction res; ;
            ResultAction<IDBStorageEngine> resdbengine = await GetStorageEngine(sessionid);
            if (!resdbengine.IsOk)
            {
                res = new();
                res.CopyStatusFrom(resdbengine);
                return res;
            }

            return await resdbengine.datas.Add(node,sessionid);
        }

        async Task<ResultAction> IDBStorageBridge.Update<T>(T node, Filters query, string sessionid)
		{
			ResultAction res; ;
			ResultAction<IDBStorageEngine> resdbengine = await GetStorageEngine(sessionid);
			if (!resdbengine.IsOk)
			{
				res = new();
				res.CopyStatusFrom(resdbengine);
				return res;
			}

			return await resdbengine.datas.Update(node, query, sessionid);
		}
        async Task<ResultAction> IDBStorageBridge.AddOrUpdate<T>(T node, Filters query, string sessionid)
        {
            ResultAction res; ;
            ResultAction<IDBStorageEngine> resdbengine = await GetStorageEngine(sessionid);
            if (!resdbengine.IsOk)
            {
                res = new();
                res.CopyStatusFrom(resdbengine);
                return res;
            }

            return await resdbengine.datas.AddOrUpdate(node, query, sessionid);
        }

        async Task<ResultAction> IDBStorageBridge.Delete(string collectionname, Filters query, string sessionid)
		{
			ResultAction res; ;
			ResultAction<IDBStorageEngine> resdbengine = await GetStorageEngine(sessionid);
			if (!resdbengine.IsOk)
			{
				res = new();
				res.CopyStatusFrom(resdbengine);
				return res;
			}
			return await resdbengine.datas.Delete(collectionname, query, sessionid);
		}

		async Task<ResultAction> IDBStorageBridge.ExecuteQuery(string query, string sessionid, bool strict)
		{
			ResultAction res; ;
			ResultAction<IDBStorageEngine> resdbengine = await GetStorageEngine(sessionid);
			if (!resdbengine.IsOk)
			{
				res = new();
				res.CopyStatusFrom(resdbengine);
				return res;
			}
			return await resdbengine.datas.ExecuteQuery(query,sessionid,strict);
		}

		async Task<ResultAction> IDBStorageBridge.ExecuteSQLScripts(string dir, ILogger logger, string sessionid)
		{
			ResultAction res; ;
			ResultAction<IDBStorageEngine> resdbengine = await GetStorageEngine(sessionid);
			if (!resdbengine.IsOk)
			{
				res = new();
				res.CopyStatusFrom(resdbengine);
				return res;
			}

			return await resdbengine.datas.ExecuteSQLScripts(dir, logger, sessionid);
		}

		
        async Task<ResultAction<List<T>>> IDBStorageBridge.GetItems<T>(T node, Filters query, string sessionid)
        {
            ResultAction<List<T>> res;
            ResultAction<IDBStorageEngine> resdbengine = await GetStorageEngine(sessionid);
            if (!resdbengine.IsOk)
            {
                res = new();
                res.CopyStatusFrom(resdbengine);
                return res;
            }

            return await resdbengine.datas.GetItems<T>(node, query, sessionid);

        }
        async Task<ResultAction<T>> IDBStorageBridge.GetItem<T>(T node, Filters query, string sessionid)
		{
			ResultAction<T> res;
			ResultAction<IDBStorageEngine> resdbengine = await GetStorageEngine(sessionid);
			if (!resdbengine.IsOk)
			{
				res = new();
				res.CopyStatusFrom(resdbengine);
				return res;
			}
			return await resdbengine.datas.GetItem<T>(node, query, sessionid);
		}

		async Task<ResultAction<long>> IDBStorageBridge.GetCount(string collectionname, Filters query, string sessionid)
		{
			ResultAction<long> res=new(); 
			ResultAction<IDBStorageEngine> resdbengine = await GetStorageEngine(sessionid);
			if (!resdbengine.IsOk)
			{
				res.CopyStatusFrom(resdbengine);
				return res;
			}

			return await resdbengine.datas.GetCount(collectionname,query,sessionid);
		}

		private async Task<ResultAction<IDBStorageEngine>> GetStorageEngine(string sessionid)
		{
			ResultAction<IDBStorageEngine> res = new();

			foreach (var item in engines)
			{

				try
				{

					if (item.IsForMe(sessionid))
					{
						res.datas = item;
						return res;
					}
				}
				catch (Exception e)
				{
					res.SetError(new(e.Message), StatusAction.logicalerror);

				}
			}
			res.SetError(new($"unable to find dbengine  for database: {sessionid} in registered dbengines"), StatusAction.logicalerror);
			return res;


		}

		async Task<ResultAction> IDBStorageBridge.Exists(string collectionname, Filters query, string sessionid)
		{
			ResultAction res ;
			ResultAction<IDBStorageEngine> resdbengine = await GetStorageEngine(sessionid);
			if (!resdbengine.IsOk)
			{
				res = new();
				res.CopyStatusFrom(resdbengine);
				return res;
			}

			return await resdbengine.datas.Exists(collectionname, query, sessionid);
		}

		async Task<ResultAction> IDBStorageBridge.Truncate(string collectionname, string sessionid)
		{
			ResultAction res ;
			ResultAction<IDBStorageEngine> resdbengine = await GetStorageEngine(sessionid);
			if (!resdbengine.IsOk)
			{
				res = new();
				res.CopyStatusFrom(resdbengine);
				return res;
			}

			return await resdbengine.datas.Truncate(collectionname, sessionid);

		}

        async Task<ResultAction> IDBStorageBridge.IncrementField(string field, int increment, string collectionname, Filters query, string sessionid)
        {
            ResultAction res;
            ResultAction<IDBStorageEngine> resdbengine = await GetStorageEngine(sessionid);
            if (!resdbengine.IsOk)
            {
                res = new();
                res.CopyStatusFrom(resdbengine);
                return res;
            }
			return await resdbengine.datas.IncrementField(field,increment,collectionname, query, sessionid);
        }

        async Task<IDBStorageEngine> IDBStorageBridge.BeginTransaction(string sessionid)
        {
            ResultAction<IDBStorageEngine> resdbengine = await GetStorageEngine(sessionid);
            if (!resdbengine.IsOk)
            {
                return null;
            }
            return await resdbengine.datas.BeginTransaction(sessionid);

        }

       
    }
}
