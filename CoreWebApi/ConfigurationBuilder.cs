using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

using Microsoft.Extensions.Logging;



using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BeSwarm.CoreWebApi;

public static class ConfigBuilder
{
	// log loaded config
	public static void Show(ILogger logger, object obj, string prefix = "")
	{
		if (obj == null)
		{
			logger.LogInformation("<null>");
			return;
		}
		Type objType = obj.GetType();
		PropertyInfo[] properties = objType.GetProperties();
		if (obj == null) return;
		foreach (PropertyInfo property in properties)
		{
			bool needrecurse = true;
			var pv = property.GetValue(obj, null);
			var display = "<null>";
			if (pv is { }) display = pv.ToString();
			var attributes = property.GetCustomAttributes(false);
			foreach (var attribute in attributes)
			{
				if (attribute as Hidden is { })
				{
					display = "***********";
					break;
				}
			}
			if (property.PropertyType.Name == "List`1")
			{
				needrecurse = false;
				List<string> l1 = property.GetValue(obj, null) as List<string>;
				if (l1 is { })
				{
					logger.LogInformation(prefix + property.Name + ":" + l1.Count);
					int i = 0;
					foreach (var item in l1)
					{
						logger.LogInformation(prefix + property.Name + $"[{++i}]:" + item);
					}
				}
				else
				{
					int i = 0;
					IList ilist = property.GetValue(obj, null) as IList;
					logger.LogInformation(prefix + property.Name + ":" + ilist.Count);
					foreach (var item in ilist)
					{
						ConfigBuilder.Show(logger, item, prefix + property.Name + $"[{++i}]:" + ".");
					}
				}
			}
			if (property.PropertyType.IsPrimitive == true || property.PropertyType.Name == "String")
			{
				needrecurse = false;
				logger.LogInformation(prefix + property.Name + ":" + display);
			}
			if (needrecurse)
			{
				logger.LogInformation(prefix + property.Name);
				ConfigBuilder.Show(logger, property.GetValue(obj, null), prefix + property.Name + ".");
			}
		}
	}
	public static ResultAction<T> BuildConfiguration<T>(string section, string configfilecontent, ILogger logger)
	{
		ResultAction<T> status = new ResultAction<T>();
		try
		{
			var jsonObject = JsonConvert.DeserializeObject<JObject>(configfilecontent);
			var scoreToken = jsonObject.SelectToken(section);
			if (scoreToken == null)
			{
				status.SetError(new InternalError($"section: {section} not found in config file: {configfilecontent} "), StatusAction.notfound);
				return status;
			}
			status.datas = JsonConvert.DeserializeObject<T>(scoreToken.ToString());
			ResultAction validate = TryValidate.Validate(status.datas, false);
			if (!validate.IsOk)
			{
				status.SetError(new InternalError($"bad config file: section:{section}. error:{validate.error.Description}"), StatusAction.logicalerror);
				logger.LogError(status.error.Description);
			}
			else
			{
				logger.LogInformation($"");
				logger.LogInformation($"-------confguration:{section}");
				Show(logger, status.datas);
				logger.LogInformation("----------------------------------------------------");
				logger.LogInformation($"");
			}
		}
		catch (Exception e)
		{

			status.SetError(new InternalError($"bad config file: {configfilecontent}. error:{e.Message}"), StatusAction.logicalerror);
			logger.LogError(status.error.Description);
		}
		return status;
	}
}
