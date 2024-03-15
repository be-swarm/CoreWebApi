using System;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace BeSwarm.CoreWebApi.Services.DataBase
{

   public class Filter
    {
        public string Operator { get; set; }
        public object Value { get; set; }

        public Filter(string o, object n)
        {
            Operator = o;
            Value = n;
        }
    }

   public class Filters
   {
       public Dictionary<string, List<Filter>> filters = new();

       public void Add(string _colname, string _operator, object _value)
       {
           var exist=filters.ContainsKey(_colname);
           if (exist)
           {
               filters[_colname].Add(new Filter(_operator, _value));
           }
           else
           {
               filters.Add(_colname, new List<Filter> { new Filter(_operator, _value) });
           }
           
       }
    }
   
    public interface IDBStorageEngine:IDisposable
    {


        string GetType();
		bool IsForMe(string session);
        Task<IDBStorageEngine> BeginTransaction(string sessionid);
        Task<ResultAction> CommitTransaction();
        Task<ResultAction> AbortTransaction();
        Task<ResultAction> Update<T>(T node, Filters query,string sessionid);
        Task<ResultAction> AddOrUpdate<T>(T node, Filters query, string sessionid);
        Task<ResultAction> AddIfNotExist<T>(T node, Filters query, string sessionid);
        Task<ResultAction> Add<T>(T node, string sessionid);
        Task<ResultAction<List<T>>> GetItems<T>(T node, Filters Query, string sessionid);
        Task<ResultAction<T>> GetItem<T>(T node, Filters Query, string sessionid);
     	Task<ResultAction> Exists(string collectionname,Filters query, string sessionid);
        Task<ResultAction<long>> GetCount(string collectionname,Filters query, string sessionid);
        Task<ResultAction> Delete(string collectionname,Filters query, string sessionid);
        Task<ResultAction> Truncate(string collectionname,string sessionid);
        Task<ResultAction> ExecuteSQLScripts(string dir,ILogger logger, string sessionid);
        Task<ResultAction> ExecuteQuery(string query, string sessionid, bool strict = true);
        Task<ResultAction> IncrementField(string field, int increment, string collectionname, Filters query, string sessionid);


    }
}
