using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;
using BeSwarm.CoreWebApi;
using BeSwarm.CoreWebApi.Services.DataBase;
using BeSwarm.CoreWebApi.Services.Errors;
using System.Collections;

namespace BeSwarm.WebApi.Core.DBStorage
{
    public class ConfigMongoDB
    {
        [Len(1, -1)] public string host { get; set; } = "";
        public int port { get; set; }
        [Len(1, -1)] public string schema { get; set; } = "";
        [Hidden] public string user { get; set; } = "";
        [Hidden] public string password { get; set; } = "";
        public bool ssl { get; set; } = false;
    }

    public class SessionsMongoDB
    {
        public Dictionary<string, IMongoDatabase> sessions = new();

        public ResultAction AddSession(ConfigMongoDB config, string _id)
        {
            ResultAction res = new();
            try
            {
                IMongoDatabase db;
                MongoClient client;


                if (!string.IsNullOrEmpty(config.user) && !string.IsNullOrEmpty(config.password))
                {
                    MongoClientSettings settings = new();
                    settings.Server = new(config.host, config.port);
                    settings.UseTls = false;
                    settings.UseSsl = config.ssl;
                    settings.SslSettings = new SslSettings();
                    settings.SslSettings.EnabledSslProtocols = SslProtocols.Tls12;
                    MongoIdentity identity = new MongoInternalIdentity("admin", config.user);
                    MongoIdentityEvidence evidence = new PasswordEvidence(config.password);
                    settings.Credential = new MongoCredential("SCRAM-SHA-1", identity, evidence);
                    client = new MongoClient(settings);
                }
                else
                {
	                MongoClientSettings settings;
                    if(config.port!=-1) settings = MongoClientSettings.FromConnectionString($"{config.host}:{config.port}");
                    else settings = MongoClientSettings.FromConnectionString($"{config.host}");
                    client = new MongoClient(settings);
                }

                db = client.GetDatabase(config.schema);
                // validate conn settings;ection string is ok 
                IMongoCollection<BsonDocument> collection = db.GetCollection<BsonDocument>("test");
                var found = collection.FindSync(new BsonDocument()).ToListAsync();
                sessions[_id] = db;
            }
            catch (Exception e)
            {
                res.SetError(new InternalError(e.Message), StatusAction.internalerror);
            }

            return res;
        }
        public ResultAction<IMongoDatabase> GetSession(string id)
        {
	        ResultAction<IMongoDatabase> res = new();
	        IMongoDatabase session;
	        if (!sessions.TryGetValue(id,out session))
	        {
		        res.SetError(new($"GetSession  id:{id} not found in mongodb databases managed by this service"), StatusAction.logicalerror);
		        return res;
	        }
	        res.datas = session;
	        return res;
        }
    }

    public class DBStorageEngineMongoDB : IDBStorageEngine
    {
        IDispatchCriticalInternalError dispatch_error;
       
        public SessionsMongoDB sessions;

        public DBStorageEngineMongoDB(SessionsMongoDB _sessions, IDispatchCriticalInternalError _dispatch_error)
        {
            dispatch_error = _dispatch_error;
            sessions = _sessions;
        }

        public string GetType()
        {
            return "mongodb";
        }
        public bool IsForMe(string sessionid)
        {
	        if (sessions.GetSession(sessionid) is { }) return true;
	        return false;
        }


		public async Task<ResultAction> AddOrUpdateNode<T>(T node, Filters query, string sessionid)
        {
            ResultAction result = new();
            try
            {
                ResultAction<IMongoDatabase> session = sessions.GetSession(sessionid);
                result.CopyStatusFrom(session);
                if (!result.IsOk) return result;
				IMongoCollection<BsonDocument> collection =session.datas.GetCollection<BsonDocument>(CollectionName.GetName(node));
                ResultAction<BsonDocument> doc = await GetBsonDocument(node);
                if (doc.IsOk == false)
                {
                    result.CopyStatusFrom(doc);
                    return result;
                }

				var filter = GetQueryFromFilters(query);
				BsonDocument res = await collection.FindOneAndUpdateAsync(filter, doc.datas);
                if (res == null) await collection.InsertOneAsync(doc.datas);
            }
            catch (Exception e)
            {
                string id = await dispatch_error.Dispatch(e, "");
                result.SetError(new InternalError($"internalerror:{id.ToString()}"), StatusAction.internalerror);
            }

            return result;
        }

        public async Task<ResultAction> AddIfNotExistNode<T>(T node, Filters query, string sessionid)
        {
            ResultAction result = new();
            try
            {
				ResultAction<IMongoDatabase> session = sessions.GetSession(sessionid);
				result.CopyStatusFrom(session);
				if (!result.IsOk) return result;
				IMongoCollection<BsonDocument> collection =session.datas.GetCollection<BsonDocument>(CollectionName.GetName(node));
				var filter = GetQueryFromFilters(query);
				var found = await collection.FindSync(filter).ToListAsync();
                if (found.Count == 1)
                {
                    result.SetError(new InternalError("already exist", -1), StatusAction.logicalerror);
                    return result;
                }

                ResultAction<BsonDocument> doc = await GetBsonDocument(node);
                if (!doc.IsOk)
                {
                    result.CopyStatusFrom(doc);
                    return result;
                }

                await collection.InsertOneAsync(doc.datas);
            }
            catch (Exception e)
            {
                string id = await dispatch_error.Dispatch(e);
                result.SetError(new InternalError($"internalerror:{id.ToString()}"), StatusAction.internalerror);
            }

            return result;
        }
        public async Task<ResultAction<List<T>>> GetNodes<T>(string collectionname,Filters query, string sessionid)
        {
	        ResultAction<List<T>> result = new();
	        ResultAction<T> res;
	        try
	        {
		        ResultAction<IMongoDatabase> session = sessions.GetSession(sessionid);
		        result.CopyStatusFrom(session);
		        if (!result.IsOk) return result;

		        IMongoCollection<BsonDocument> collection = session.datas.GetCollection<BsonDocument>(collectionname);
				var filter = GetQueryFromFilters(query);
				var found = await collection.FindSync(filter).ToListAsync();
		        if (found.Count() >= 1)
		        {
			        foreach (var item in found)
			        {
				        res = await GetNodeFromBSonDocument<T>(item);
				        if (!res.IsOk)
				        {
					        result.CopyStatusFrom(res);
					        result.datas.Clear();
					        return result;
				        }
				        else result.datas.Add(res.datas);
			        }
		        }
		        else
		        {
			        result.status = StatusAction.notfound;
		        }
	        }
	        catch (Exception e)
	        {
		        string id = await dispatch_error.Dispatch(e);
		        result.SetError(new InternalError($"internalerror:{id.ToString()}"), StatusAction.internalerror);
	        }

	        return result;
        }
        public async Task<ResultAction<T>> GetNode<T>(string collectionname, Filters query, string sessionid)
        {
	        ResultAction<T> result=new();
	        try
	        {
		        ResultAction<IMongoDatabase> session = sessions.GetSession(sessionid);
		        result.CopyStatusFrom(session);
		        if (!result.IsOk) return result;

		        IMongoCollection<BsonDocument> collection = session.datas.GetCollection<BsonDocument>(collectionname);
		        var filter = GetQueryFromFilters(query);
		        var found = await collection.FindSync(filter).ToListAsync();
		        if (found.Count() == 1)
		        {
			         result = await GetNodeFromBSonDocument<T>(found[0]);
		        }
		        else
		        {
			        result.status = StatusAction.notfound;
		        }
	        }
	        catch (Exception e)
	        {
		        string id = await dispatch_error.Dispatch(e);
		        result.SetError(new InternalError($"internalerror:{id.ToString()}"), StatusAction.internalerror);
	        }

	        return result;
        }

		public async Task<ResultAction> NodesExists(string collectionname, Filters query, string sessionid)
        {
	        ResultAction result = new();
	        try
	        {
		        ResultAction<IMongoDatabase> session = sessions.GetSession(sessionid);
		        result.CopyStatusFrom(session);
		        if (!result.IsOk) return result;

		        IMongoCollection<BsonDocument> collection = session.datas.GetCollection<BsonDocument>(collectionname);
				var filter = GetQueryFromFilters(query);
				var found = await collection.FindSync(filter).ToListAsync();
		        if (found.Count() == 0)
		        {
			        result.status = StatusAction.notfound;
		        }
	        }
	        catch (Exception e)
	        {
		        string id = await dispatch_error.Dispatch(e);
		        result.SetError(new InternalError($"internalerror:{id.ToString()}"), StatusAction.internalerror);
	        }

	        return result;
        }
        public async Task<ResultAction<long>> GetNodesCount(string collectionname, Filters query, string sessionid)
        {
	        ResultAction<long> result = new();
	        try
	        {
		        ResultAction<IMongoDatabase> session = sessions.GetSession(sessionid);
		        result.CopyStatusFrom(session);
		        if (!result.IsOk) return result;

		        IMongoCollection<BsonDocument> collection = session.datas.GetCollection<BsonDocument>(collectionname);
				var filter = GetQueryFromFilters(query);
				result.datas = await collection.CountDocumentsAsync(filter);
	        }
	        catch (Exception e)
	        {
		        string id = await dispatch_error.Dispatch(e);
		        result.SetError(new InternalError($"internalerror:{id.ToString()}"), StatusAction.internalerror);
	        }

	        return result;
        }


		public async Task<ResultAction> DeleteNodes(string collectionname, Filters query, string sessionid)
        {
            ResultAction result = new();
            try
            {
				ResultAction<IMongoDatabase> session = sessions.GetSession(sessionid);
				result.CopyStatusFrom(session);
				if (!result.IsOk) return result;
				IMongoCollection<BsonDocument> collection =session.datas.GetCollection<BsonDocument>(collectionname);
				var filter = GetQueryFromFilters(query);
				await collection.DeleteOneAsync(filter);
            }
            catch (Exception e)
            {
                string id = await dispatch_error.Dispatch(e);
                result.SetError(new InternalError($"internalerror:{id.ToString()}"), StatusAction.internalerror);
            }

            return result;
        }
        
        private BsonDocument GetQueryFromFilters(Filters Query)
        {
            BsonDocument find = null;
            if (Query is { })
            {  find=new();
                foreach (var item in Query.filters)
                {
                    BsonDocument query = new();
                    foreach (var item2 in item.Value)
                    {
                        string op = "";
                        switch (item2.Operator)
                        {
                            case "=":
                                op = "$eq";
                                break;
                            case ">=":
                                op = "$gte";
                                break;
                            case "<=":
                                op = "$lte";
                                break;
                            case "<":
                                op = "$lt";
                                break;
                            case ">":
                                op = "$gt";
                                break;
                            case "<>":
                                op = "$ne";
                                break;
                        }

                        System.TypeCode typeCode = Type.GetTypeCode(item2.Value.GetType());
                        switch (typeCode)
                        {
                            case TypeCode.DateTime:
                                query.AddRange(new BsonDocument(op, (DateTime) item2.Value));
                                break;
                            case TypeCode.String:
                                query.AddRange(new BsonDocument(op, item2.Value.ToString()));
                                break;
                            case TypeCode.Boolean:
                                query.AddRange(new BsonDocument(op, (bool)item2.Value));
                                break;
                            case TypeCode.Int32:
	                            query.AddRange(new BsonDocument(op, (int)item2.Value));
	                            break;
							default:
                                query.AddRange(new BsonDocument(op, item2.Value.ToString()));
                                break;
                        }
                    }
                    find.AddRange((new BsonDocument(item.Key, query)));
                }
            }
            return find;
        }

        async Task<ResultAction<T>> GetNodeFromBSonDocument<T>(BsonDocument node)
        {
            //string Json = "{";
            string t = "";
            StringBuilder Json = new StringBuilder();
            Json.Append("{");
            ResultAction<T> res = new ResultAction<T>();
            T obj = (T) Activator.CreateInstance(typeof(T));
            var properties = obj.GetType().GetProperties();

            foreach (var property in properties)
            {
                var attributes = property.GetCustomAttributes(false);
                try
                {
                    BsonElement o;
                    node.TryGetElement(property.Name, out o);
                    if (o.Name is { } && o.Value as BsonNull == null)
                    {
        
                        if (Json.Length != 1) Json.Append(",");
                        string type = property.PropertyType.Name;
                        t = node[property.Name].ToString();

                        if (t == null) t = "";

                        System.TypeCode typeCode = Type.GetTypeCode(property.PropertyType);
                        switch (typeCode)
                        {
                            case TypeCode.Single:
                                Json.Append(property.Name + ":" + t.Replace(",", "."));
                                break;
                            case TypeCode.Double:
                                Json.Append(property.Name + ":" + t.Replace(",", "."));
                                break;
                            case TypeCode.Decimal:
                                Json.Append(property.Name + ":" + t.Replace(",", "."));
                                break;

                            case TypeCode.DateTime:
                                Json.Append(property.Name + ":\"" + t + "\"");
                                break;
                            case TypeCode.String:
                                Json.Append(property.Name + ":\"" + t.Replace("\"", "\\\"") + "\"");
                                break;
                            default:
                                Json.Append(property.Name + ":" + t);
                                break;
                        }
                    }
                }
                catch (Exception e)
                {
                    string id = await dispatch_error.Dispatch(e, "GetNodeFromBSonDocument");
                    res.SetError(new InternalError($"internalerror:{id.ToString()}"), StatusAction.internalerror);
                }
            }

            Json.Append("}");
            string result = Json.ToString();
            try
            {
                res.datas = JsonConvert.DeserializeObject<T>(Json.ToString(), new JsonSerializerSettings
                {
                    DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                });
            }
            catch (Exception e)
            {
                string id = await dispatch_error.Dispatch(e, "GetNodeFromBSonDocument");
                res.SetError(new InternalError($"internalerror:{id.ToString()}"), StatusAction.internalerror);
                return res;
            }

            return res;
        }


        async Task<ResultAction<BsonDocument>> GetBsonDocument(object o)
        {
            ResultAction<BsonDocument> res = new();
            var properties = o.GetType().GetProperties();
            var doc = new BsonDocument();
            ResultAction<string> statusencrypt = new();
            try
            {
                foreach (var property in properties)
                {
                    if (property.GetValue(o, null) != null)
                    {
                        var attributes = property.GetCustomAttributes(false);
                        
                        string data = "";
                        System.TypeCode typeCode = Type.GetTypeCode(property.PropertyType);
                        switch (typeCode)
                        {
                            case TypeCode.DateTime:
                                DateTime d = DateTime.SpecifyKind((DateTime) property.GetValue(o, null), DateTimeKind.Utc);
                                doc.Add(property.Name, d);
                                break;
                            case TypeCode.String:
                                doc.Add(property.Name, property.GetValue(o, null).ToString());
                                break;
                            default:
                                    BsonDocument sdoc2 = BsonDocument.Parse(
                                        $"{{ {property.Name}:{JsonConvert.SerializeObject(property.GetValue(o, null), new StringDecimalConverter())}}}");
                                    doc.AddRange(sdoc2);
                          
                                break;
                        }
                    }
                    else
                        doc.Add(property.Name, BsonNull.Value);
                }
                res.datas = doc;
            }
            catch (Exception e)
            {
                string id = await dispatch_error.Dispatch(e, "SerializeNode");
                res.SetError(new InternalError($"internalerror:{id.ToString()}"), StatusAction.internalerror);
                return res;
            }

            return res;
        }

     

        public async Task<ResultAction> ExecuteQuery(string query, string sessionid, bool strict = true)
        {
            ResultAction result = new();
            try
            {
                BsonDocument key;
                BsonDocument value;
                string[] sp = query.Split('|');
				ResultAction<IMongoDatabase> session = sessions.GetSession(sessionid);
				result.CopyStatusFrom(session);
                if (!result.IsOk) return result;

				IMongoCollection<BsonDocument> collection = session.datas.GetCollection<BsonDocument>(sp[1]);
                switch (sp[0])
                {
                    case "delete":
                        key = BsonDocument.Parse($"{{ {sp[2]}}}");
                        value = BsonDocument.Parse($"{{{sp[2]},{sp[3]}}}");
                        await collection.DeleteManyAsync(key);
                        break;
                    case "addorupdate":
                        key = BsonDocument.Parse($"{{ {sp[2]}}}");
                        value = BsonDocument.Parse($"{{{sp[2]},{sp[3]}}}");
                        BsonDocument exist = await collection.FindOneAndUpdateAsync(key, value);
                        if (exist == null) await collection.InsertOneAsync(value);
                        break;
                    case "createindex":
                        var cmdStr =
                            $"{{ createIndexes: '{sp[1]}', indexes: [ {{ key: {{{sp[3]}}}, name: '{sp[2]}', unique: true }} ] }}";
                        var cmd = BsonDocument.Parse(cmdStr);
                        var resultd = session.datas.RunCommand<BsonDocument>(cmd);
                        break;
                    case "command":
                        var cmdd = BsonDocument.Parse(sp[1]);
                        var resultd2 = session.datas.RunCommand<BsonDocument>(cmdd);
                        break;
                }
            }
            catch (Exception e)
            {
                result.SetError(new(e.Message), StatusAction.internalerror);
            }

            return result;
        }

        public async Task<ResultAction> Truncate(string collectionname, string sessionid)
        {
            ResultAction result = new();
            try
            {
				ResultAction<IMongoDatabase> session = sessions.GetSession(sessionid);
				result.CopyStatusFrom(session);
				if (!result.IsOk) return result;

				await session.datas.DropCollectionAsync(collectionname);
            }
            catch (Exception e)
            {
                string id = await dispatch_error.Dispatch(e);
                result.SetError(new InternalError($"internalerror:{id.ToString()}"), StatusAction.internalerror);
            }

            return result;
        }

        public async Task<ResultAction> ExecuteSQLScripts(string dir, ILogger logger, string sessionid)
        {
            ResultAction res = new();
            string[] files = null;
            var filePath = Path.GetDirectoryName(typeof(DBStorageEngineMongoDB).GetTypeInfo().Assembly.Location) +
                           CoreEnvironment.dirseparator + dir;
            try
            {
                files = Directory.GetFiles(filePath);
            }
            catch (Exception e)
            {
                return res;
            }

            foreach (var session in sessions.sessions)
            {
               
                foreach (var file in files)
                {
                    logger.LogInformation($"Processing mongodb majschemadb script:{file} db:->{session.Key}");

                    string[] lines = File.ReadAllLines(file);
                    foreach (var line in lines)
                    {
                        string squery = line.TrimStart();
                        if (!squery.StartsWith("//") && squery.Length != 0) //not a comment or emptry
                        {
                            ResultAction exe = await ExecuteQuery(squery, sessionid);
                            if (exe.IsOk) Console.Write(".");
                            else Console.Write("!");
                        }
                    }
                }

                Console.WriteLine();
            }

            return res;
        }
    }
}