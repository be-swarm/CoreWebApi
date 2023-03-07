
using System;
using System.Reflection;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Newtonsoft.Json;
using System.Linq;

using System.Collections.Generic;

namespace BeSwarm.CoreWebApi.Services.Swagger
{

	public class AddSwaggerAttributes : ISchemaFilter
	{
		public void Apply(OpenApiSchema schema, SchemaFilterContext context)
		{

			Type p = context.Type;
			PropertyInfo[] properties = p.GetProperties();

			foreach (PropertyInfo property in properties)
			{
				Len len = null;
				NotNull notnull = null;
				Dim dim = null;
				Description desc = null;
				var attributes = property.GetCustomAttributes(false);
				// extract required attributes
				foreach (var attr in attributes)
				{
					if (attr as NotNull is { }) notnull = attr as NotNull;
					if (attr as Len is { }) len = attr as Len;
					if (attr as Dim is { }) dim = attr as Dim;
					if (attr as Description is { }) desc = attr as Description;
				}
				var propertyNameInCamelCasing = char.ToLowerInvariant(property.Name[0]) + property.Name.Substring(1);
				if (notnull != null || (dim != null && dim.min != -1) || (len != null && len.min != -1))  // required
				{
					schema.Required.Add(propertyNameInCamelCasing);
					schema.Properties[propertyNameInCamelCasing].Nullable = false;
				}
				if (desc != null) schema.Properties[propertyNameInCamelCasing].Description = desc.description;
				if (property.PropertyType.IsPrimitive == false)
				{
					if (dim != null && dim.min != -1) schema.Properties[propertyNameInCamelCasing].MinItems = dim.min;
					if (dim != null && dim.max != -1) schema.Properties[propertyNameInCamelCasing].MaxItems = dim.max;
				}
				else
				{
					if (dim != null && dim.min != -1) schema.Properties[propertyNameInCamelCasing].Minimum = dim.min;
					if (dim != null && dim.max != -1) schema.Properties[propertyNameInCamelCasing].Maximum = dim.max;
				}
				if (len != null && len.min != -1) schema.Properties[propertyNameInCamelCasing].MinLength = len.min;
				if (len != null && len.max != -1) schema.Properties[propertyNameInCamelCasing].MaxLength = len.max;
				if (property.GetCustomAttribute(typeof(JsonIgnoreAttribute)) != null)
				{
					schema.Properties.Remove(propertyNameInCamelCasing);
				}

			}


		}

	}
	public class RemoveVersionParameterFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            if (operation.Parameters.Count > 0)
            {
                foreach (var item in operation.Parameters)
                {
                    if (item.Name == "version")
                    {
                        operation.Parameters.Remove(item);
                        break;
                    }
                }
               // var versionParameter = operation.Parameters.Single(p => p.Name == "version");
               // if (versionParameter is { }) operation.Parameters.Remove(versionParameter);
            }
        }
    }
    public class ReplaceVersionWithExactValueInPathFilter : IDocumentFilter
    {
        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            var paths = new OpenApiPaths();
            foreach (var path in swaggerDoc.Paths)
            {
                paths.Add(path.Key.Replace("v{version}", swaggerDoc.Info.Version), path.Value);
            }
            swaggerDoc.Paths = paths;
        }
    }
}
