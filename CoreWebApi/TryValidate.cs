using Newtonsoft.Json;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace BeSwarm.CoreWebApi;

public class ErrorValidate
{
	public string field { get; set; }
	public string attribute { get; set; }
	public int min { get; set; }
	public int max { get; set; }

	public ErrorValidate(string _field, string _attr, int _min, int _max)
	{
		field = _field;
		attribute = _attr;
		min = _min;
		max = _max;
	}
}

public static class TryValidate
{
	public static ResultAction Validate(object obj, bool strict = true)
	{
		ResultAction res = new();
		if (obj == null)
		{
			res.SetError(new InternalError("Validated object must be not null"), StatusAction.logicalerror);
			return res;
		}

		List<string> propertiesparsed = new();
		List<ErrorValidate> errors = ValidateObject(propertiesparsed, obj, "");
		if (errors.Count != 0)
		{
			string entext = "";
			string frtext = "";
			foreach (var item in errors)
			{
				entext += $" Field:{item.field}";
				frtext += $" Champ:{item.field}";
				if (item.attribute == "Len")
				{
					entext += " Length";
					frtext += " Longueur";
				}

				if (item.min != -1)
				{
					entext += $"  minimum :{item.min}";
					frtext += $"  minimum :{item.min}";
				}

				if (item.max != -1)
				{
					entext += $"  maximum :{item.max}";
					frtext += $"  maximum :{item.max}";
				}
			}

			res.SetError(new InternalError(entext), StatusAction.logicalerror);
			res.error.SetDescriptionLang("FR", frtext);
			if (strict == true) throw new System.ArgumentException(JsonConvert.SerializeObject(res.error));
		}

		return res;
	}

	public static ResultAction<List<string>> ValidateDetailled(object obj, bool strict = true)
	{
		ResultAction<List<string>> res = new();
		if (obj == null)
		{
			res.SetError(new InternalError("Validated object must be not null"), StatusAction.logicalerror);
			return res;
		}

		List<string> propertiesparsed = new();
		List<ErrorValidate> errors = ValidateObject(propertiesparsed, obj, "");
		res.datas = propertiesparsed;
		if (errors.Count != 0)
		{
			if (strict == true) throw new System.ArgumentException(JsonConvert.SerializeObject(errors));
			res.SetError(new InternalError(JsonConvert.SerializeObject(errors)), StatusAction.logicalerror);
		}

		return res;
	}

	public static List<ErrorValidate> ValidateObject(List<string> propertiesparsed, object obj, string prefix = "")
	{
		List<ErrorValidate> ret = new();
		Type objType = obj.GetType();
		PropertyInfo[] properties = objType.GetProperties();
		string nomvar;
		foreach (PropertyInfo property in properties)
		{
			if (prefix != "") nomvar = prefix + "." + property.Name;
			else nomvar = property.Name;
			propertiesparsed.Add(nomvar);
			if (property.PropertyType.IsPrimitive == false)
			{
				object propValue = property.GetValue(obj, null);
				bool needrecurse = true;
				var attributes = property.GetCustomAttributes(false);
				bool errdim = false;
				bool errlen = false;
				bool errnull = false;
				Len len = null;
				NotNull notnull = null;
				Dim dim = null;
				// get necessary attributes
				foreach (var attribute in attributes)
				{
					if (attribute as NotNull is { }) notnull = attribute as NotNull;
					if (attribute as Len is { }) len = attribute as Len;
					if (attribute as Dim is { }) dim = attribute as Dim;
				}

				if (notnull is { } && propValue == null)
				{
					errnull = true;
				}
				else
				{
					switch (property.PropertyType.Name)
					{
						case "DateTime":
							needrecurse = false;
							break;
						case "Decimal":
							if (dim is { })
							{
								if (dim.min != -1 &&
									(Decimal)property.GetValue(obj, null) < dim.min)
								{
									ret.Add(new($"{nomvar}", $"Dim", dim.min, dim.max));
								}
								if (dim.max != -1 &&
									(Decimal)property.GetValue(obj, null) > dim.max)
								{
									ret.Add(new($"{nomvar}", $"Dim", dim.min, dim.max));
								}
							}
							needrecurse = false;
							break;
						case "String":
							needrecurse = false;
							if (dim is { })
							{
								// unsupported attribute
								throw new System.ArgumentException(
									$"Attribute [Dim] is not supported on {nomvar} use Len[...]");
							}

							// not set ?
							// set default to limit
							if (len == null)
							{
								len = new(-1, 16384); // default max chars
							}

							// required but null ?
							if (len.min != -1 && propValue == null)
							{
								errlen = true;
							}

							if (propValue != null && len.min != -1 && propValue.ToString().Length < len.min)
							{
								errlen = true;
							}

							if (propValue != null && len.max != -1 && propValue.ToString().Length > len.max)
							{
								errlen = true;
							}

							break;
						case "Dictionary`2":
							IDictionary idic = propValue as IDictionary;
							needrecurse = false;
							if (len is { })
							{
								// unsupported attribute
								throw new System.ArgumentException(
									$"Attribute [Len] is not supported on {nomvar} use Dim[...]");
							}

							// not set ?
							// set default to limit
							if (dim == null)
							{
								dim = new(-1, 100); // default max 100 items
							}

							if (idic == null && dim.min != -1)
							{
								errdim = true;
							}

							if (idic != null && dim.min != -1 && idic.Count < dim.min)
							{
								errdim = true;
							}

							if (idic != null && dim.max != -1 && idic.Count > dim.max)
							{
								errdim = true;
							}

							break;
						case "List`1":
							IList ilist = propValue as IList;
							needrecurse = false;
							if (len is { })
							{
								// unsupported attribute
								throw new System.ArgumentException(
									$"Attribute [Len] is not supported on {nomvar} use Dim[...]");
							}

							// not set ?
							// set default to limit
							if (dim == null)
							{
								dim = new(-1, 100); // default max 100 items
							}

							if (ilist == null && dim.min != -1)
							{
								errdim = true;
							}

							if (ilist != null && dim.min != -1 && ilist.Count < dim.min)
							{
								errdim = true;
							}

							if (ilist != null && dim.max != -1 && ilist.Count > dim.max)
							{
								errdim = true;
							}

							// control list elements
							if (ilist is { } && !errdim)
							{
								int i = 1;
								foreach (var item in ilist)
								{
									ret.AddRange(ValidateObject(propertiesparsed, item, $"{nomvar}[{i}]"));
									i++;
								}
							}

							break;
					}
				}

				if (errlen && len is { })
				{
					ret.Add(new($"{nomvar}", $"Len", len.min, len.max));
					needrecurse = false;
				}

				if (errdim && dim is { })
				{
					ret.Add(new($"{nomvar}", "Dim", dim.min, dim.max));
					needrecurse = false;
				}

				if (errnull && notnull is { })
				{
					ret.Add(new($"{nomvar}", "NotNull", 0, 0));
					needrecurse = false;
				}

				if (needrecurse == true && property.PropertyType.IsPrimitive == false && propValue != null)
				{
					// is a classs ?
					ret.AddRange(ValidateObject(propertiesparsed, propValue, $"{nomvar}"));
				}
			}
			else
			{
				var attributes = property.GetCustomAttributes(false);
				foreach (var attribute in attributes)
				{
					Dim d = attribute as Dim;
					Len len = attribute as Len;
					NotNull notnull = attribute as NotNull;

					if (len is { })
					{
						// unsupported attribute
						throw new System.ArgumentException(
							$"Attribute [Len...J is not supported on {nomvar} use Dim[...]");
					}

					if (notnull is { })
					{
						// unsupported attribute
						throw new System.ArgumentException(
							$"Attribute [NotNull] is not supported on {nomvar} use Dim[...]");
					}

					if (d is { })
					{
						if (property.PropertyType.Name == "Int32" && d.min != -1 &&
							(int)property.GetValue(obj, null) < d.min)
						{
							ret.Add(new($"{nomvar}", $"Dim", d.min, d.max));
						}

						if (property.PropertyType.Name == "Int32" && d.max != -1 &&
							(int)property.GetValue(obj, null) > d.max)
						{
							ret.Add(new($"{nomvar}", $"Dim", d.min, d.max));
						}

						if (property.PropertyType.Name == "Int64" && d.min != -1 &&
							(long)property.GetValue(obj, null) < d.min)
						{
							ret.Add(new($"{nomvar}", $"Dim", d.min, d.max));
						}

						if (property.PropertyType.Name == "Int64" && d.max != -1 &&
							(long)property.GetValue(obj, null) > d.max)
						{
							ret.Add(new($"{nomvar}", $"Dim", d.min, d.max));
						}
					}
				}
			}
		}

		return ret;
	}
}
