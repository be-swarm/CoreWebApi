using System;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace BeSwarm.CoreWebApi.Services.DataBase
{


    

    public interface IDBStorageBridge
    {

        Task<IDBStorageEngine> BeginTransaction(string sessionid);
        Task<ResultAction> Update<T>(T node, Filters query, string sessionid);
        Task<ResultAction> AddOrUpdate<T>(T node, Filters query, string sessionid);
        Task<ResultAction> AddIfNotExist<T>(T node, Filters query, string sessionid);
        Task<ResultAction> Add<T>(T node, string sessionid);
         Task<ResultAction<List<T>>> GetItems<T>(T node, Filters Query, string sessionid);
        Task<ResultAction<T>> GetItem<T>(T node, Filters Query, string sessionid);
        Task<ResultAction> Exists(string collectionname, Filters query, string sessionid);
        Task<ResultAction<long>> GetCount(string collectionname, Filters query, string sessionid);
        Task<ResultAction> Delete(string collectionname, Filters query, string sessionid);
        Task<ResultAction> Truncate(string collectionname, string sessionid);
        Task<ResultAction> ExecuteSQLScripts(string dir, ILogger logger, string sessionid);
        Task<ResultAction> ExecuteQuery(string query, string sessionid, bool strict = true);
        Task<ResultAction> IncrementField(string field, int increment, string collectionname, Filters query, string sessionid);
    }
}
