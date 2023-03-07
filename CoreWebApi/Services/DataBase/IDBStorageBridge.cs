using System;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace BeSwarm.CoreWebApi.Services.DataBase
{

   

 
    public interface IDBStorageBridge
    {


		Task<ResultAction> AddOrUpdateNode<T>(T node, Filters query, string sessionid);
		Task<ResultAction> AddIfNotExistNode<T>(T node, Filters query, string sessionid);
		Task<ResultAction<List<T>>> GetNodes<T>(string collectionname, Filters Query, string sessionid);
		Task<ResultAction<T>> GetNode<T>(string collectionname, Filters Query, string sessionid);
		Task<ResultAction> NodesExists(string collectionname, Filters query, string sessionid);
		Task<ResultAction<long>> GetNodesCount(string collectionname, Filters query, string sessionid);
		Task<ResultAction> DeleteNodes(string collectionname, Filters query, string sessionid);
		Task<ResultAction> Truncate(string collectionname, string sessionid);
		Task<ResultAction> ExecuteSQLScripts(string dir, ILogger logger, string sessionid);
		Task<ResultAction> ExecuteQuery(string query, string sessionid, bool strict = true);

	}
}
