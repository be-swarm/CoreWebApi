using BeSwarm.Validator;

using System.Collections;
using System.Reflection;

namespace BeSwarm.CoreWebApi.Services
{
    public static class ToolBox
    {
      
        public static Dictionary<string,string> Dump(object obj, string Prefix = "")
        {
            Dictionary<string, string> result = new();
            if (obj == null) return new();
            Type objType = obj.GetType();
            var properties = obj.GetType().GetProperties().Where(prop => prop.CanRead && prop.GetIndexParameters().Length == 0).ToList();
            if (properties is { })
            {

                foreach (PropertyInfo property in properties)
                {
                    string Name = "";
                    if (Prefix != "") Name = Prefix + "." + property.Name.ToUpper();
                    else Name = property.Name.ToUpper();
                   
                    object? value = property.GetValue(obj, null);
                    if (property.PropertyType.IsValueType == true || property.PropertyType == typeof(string))
                    {

                        result[Name]=value?.ToString()??"";
                    }
                    else
                    {
                        var asEnumerable = value as IEnumerable;
                        if (asEnumerable is { })
                        {   // recursive on child items 
                            int i = 0;
                            foreach (var item in asEnumerable)
                            {
                                if (Prefix != "") Name = string.Format($"{Prefix}.{property.Name}[{i}]").ToUpper();
                                else Name = string.Format($"{property.Name}[{i}]").ToUpper();
                                result = result.Concat(Dump(item,Name)).ToDictionary(x => x.Key, x => x.Value);
                                i++;
                            }
                        }
                        else
                        {   // recursive object
                            if (Prefix != "") Name = string.Format($"{Prefix}.{property.Name}").ToUpper(); 
                            else Name = string.Format($"{property.Name}").ToUpper(); ;
                            result = result.Concat(Dump(value, Name)).ToDictionary(x => x.Key, x => x.Value);
                        }
                    }

                }
            }
            return result;
        }
    }
}
