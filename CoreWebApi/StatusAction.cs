
using Microsoft.AspNetCore.Http;

using System;
using System.Collections.Generic;

namespace BeSwarm.CoreWebApi;

public static class StatusAction
{
	public const int ok = StatusCodes.Status200OK;
	public const int notfound = StatusCodes.Status404NotFound;
	public const int internalerror = StatusCodes.Status500InternalServerError;
	public const int logicalerror = StatusCodes.Status400BadRequest;
	public const int unauthorized = StatusCodes.Status401Unauthorized;
	public const int forbidden = StatusCodes.Status403Forbidden;


}
public class InternalError
{
	public int ErrorCode { get; set; } = 0;
	public string Description { get => DescriptionLang["EN"]; }

	public Dictionary<string, string> DescriptionLang { get; set; } = new();
	public string Function { get; set; } = "";
	public InternalError(string _descriptionEN, int _errorcode = 0, string _function = null)
	{
		DescriptionLang["EN"] = _descriptionEN;
		ErrorCode = _errorcode;
		Function = _function;
	}
	public void SetDescriptionLang(string lang, string _description)
	{
		DescriptionLang[lang] = _description;
	}
	public string GetDescriptionInLang(string lang)
	{
		string ret = "";
		if (!DescriptionLang.TryGetValue(lang, out ret))
		{
			ret = Description;
		}
		return ret;

	}
}
public interface IResultAction
{
	int status { get; set; }
	InternalError error { get; set; }
}
public class ResultAction<T> : IResultAction
{
	public T datas { get; set; }
	public int status { get; set; }
	public InternalError error { get; set; } = new InternalError("");

	public ResultAction()
	{
		status = StatusAction.ok;
		Type t = typeof(T);
		string name = t.Name;
		try
		{
			if (t.Name != "String" && t.Name != "Object" && !t.IsInterface) datas = (T)Activator.CreateInstance(typeof(T));
			else datas = default(T);

		}
		catch (Exception e)
		{

		}
	}

	public void SetError(InternalError error, int status)
	{
		this.error = error;
		this.status = status;
	}
	public void CopyStatusFrom(IResultAction src)
	{
		status = src.status;
		error = src.error;
	}
	[System.Text.Json.Serialization.JsonIgnore] public bool IsOk { get { if (status == StatusAction.ok) return true; else return false; } }
	[System.Text.Json.Serialization.JsonIgnore] public bool IsNotFound { get { if (status == StatusAction.notfound) return true; else return false; } }
	[System.Text.Json.Serialization.JsonIgnore] public bool IsError { get { if (status == StatusAction.internalerror || status == StatusAction.logicalerror) return true; else return false; } }

}
public class ResultAction : IResultAction
{
	public int status { get; set; }
	public InternalError error { get; set; } = new InternalError("");

	public ResultAction()
	{
		status = StatusAction.ok;
	}

	public void SetError(InternalError error, int status)
	{
		this.error = error;
		this.status = status;
	}
	public void CopyStatusFrom(IResultAction src)
	{
		status = src.status;
		error = src.error;
	}
	[System.Text.Json.Serialization.JsonIgnore] public bool IsOk { get { if (status == StatusAction.ok) return true; else return false; } }
	[System.Text.Json.Serialization.JsonIgnore] public bool IsNotFound { get { if (status == StatusAction.notfound) return true; else return false; } }
	[System.Text.Json.Serialization.JsonIgnore] public bool IsError { get { if (status == StatusAction.internalerror || status == StatusAction.logicalerror) return true; else return false; } }

}
