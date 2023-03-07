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

		async Task<ResultAction> IDBStorageBridge.AddIfNotExistNode<T>(T node, Filters query, string sessionid)
		{
			ResultAction res; ;
			ResultAction<IDBStorageEngine> resdbengine = await GetStorageEngine(sessionid);
			if (!resdbengine.IsOk)
			{
				res = new();
				res.CopyStatusFrom(resdbengine);
				return res;
			}

			return await resdbengine.datas.AddIfNotExistNode(node, query,sessionid);
		}

		async Task<ResultAction> IDBStorageBridge.AddOrUpdateNode<T>(T node, Filters query, string sessionid)
		{
			ResultAction res; ;
			ResultAction<IDBStorageEngine> resdbengine = await GetStorageEngine(sessionid);
			if (!resdbengine.IsOk)
			{
				res = new();
				res.CopyStatusFrom(resdbengine);
				return res;
			}

			return await resdbengine.datas.AddOrUpdateNode(node, query, sessionid);
		}

		async Task<ResultAction> IDBStorageBridge.DeleteNodes(string collectionname, Filters query, string sessionid)
		{
			ResultAction res; ;
			ResultAction<IDBStorageEngine> resdbengine = await GetStorageEngine(sessionid);
			if (!resdbengine.IsOk)
			{
				res = new();
				res.CopyStatusFrom(resdbengine);
				return res;
			}
			return await resdbengine.datas.DeleteNodes(collectionname, query, sessionid);
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

		async Task<ResultAction<List<T>>> IDBStorageBridge.GetNodes<T>(string collectionname, Filters query, string sessionid)
		{
			ResultAction<List<T>> res;
			ResultAction<IDBStorageEngine> resdbengine = await GetStorageEngine(sessionid);
			if (!resdbengine.IsOk)
			{
				res = new();
				res.CopyStatusFrom(resdbengine);
				return res;
			}

			return await resdbengine.datas.GetNodes<T>(collectionname, query, sessionid);

		}
		async Task<ResultAction<T>> IDBStorageBridge.GetNode<T>(string collectionname, Filters query, string sessionid)
		{
			ResultAction<T> res;
			ResultAction<IDBStorageEngine> resdbengine = await GetStorageEngine(sessionid);
			if (!resdbengine.IsOk)
			{
				res = new();
				res.CopyStatusFrom(resdbengine);
				return res;
			}
			return await resdbengine.datas.GetNode<T>(collectionname, query, sessionid);
		}

		async Task<ResultAction<long>> IDBStorageBridge.GetNodesCount(string collectionname, Filters query, string sessionid)
		{
			ResultAction<long> res=new(); 
			ResultAction<IDBStorageEngine> resdbengine = await GetStorageEngine(sessionid);
			if (!resdbengine.IsOk)
			{
				res.CopyStatusFrom(resdbengine);
				return res;
			}

			return await resdbengine.datas.GetNodesCount(collectionname,query,sessionid);
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

		async Task<ResultAction> IDBStorageBridge.NodesExists(string collectionname, Filters query, string sessionid)
		{
			ResultAction res ;
			ResultAction<IDBStorageEngine> resdbengine = await GetStorageEngine(sessionid);
			if (!resdbengine.IsOk)
			{
				res = new();
				res.CopyStatusFrom(resdbengine);
				return res;
			}

			return await resdbengine.datas.NodesExists(collectionname, query, sessionid);
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
	}
}
