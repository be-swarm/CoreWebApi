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
   
   

 
    public interface IDBStorageEngine
    {


        string GetType();
		bool IsForMe(string session);
		/// <summary>
		/// Add a node using partition key and clustering key
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="node"></param>
		/// <returns></returns>
		Task<ResultAction> AddOrUpdateNode<T>(T node, Filters query,string sessionid);
        Task<ResultAction> AddIfNotExistNode<T>(T node, Filters query, string sessionid);
        Task<ResultAction<List<T>>> GetNodes<T>(string collectionname,Filters Query, string sessionid);
        Task<ResultAction<T>> GetNode<T>(string collectionname, Filters Query, string sessionid);
		Task<ResultAction> NodesExists(string collectionname,Filters query, string sessionid);
        Task<ResultAction<long>> GetNodesCount(string collectionname,Filters query, string sessionid);
        Task<ResultAction> DeleteNodes(string collectionname,Filters query, string sessionid);
        Task<ResultAction> Truncate(string collectionname,string sessionid);
        Task<ResultAction> ExecuteSQLScripts(string dir,ILogger logger, string sessionid);
        Task<ResultAction> ExecuteQuery(string query, string sessionid, bool strict = true);

	}
}
