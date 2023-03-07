namespace BeSwarm.CoreWebApi.Services.DataBase
{
	//
	// collection name (table name)
	//
	[System.AttributeUsage(System.AttributeTargets.Class)]
	public class CollectionName : Attribute
	{
		public string Description { get; set; }

		public CollectionName(string description)
		{
			Description = description;
		}
		public static string GetName(object o)
		{
			string collectionname = "";
			var attributes = o.GetType().GetCustomAttributes(false);
			foreach (var attribute in attributes)
			{
				CollectionName collattr = attribute as CollectionName;
				if (collattr != null)
				{
					collectionname = collattr.Description;
					break;
				}
			}
			if (collectionname == "")
			{
				throw (new Exception(string.Format(@"error:GetName function.object {0} has no attribute CollectionName(""...."")", o.GetType())));
			}
			return collectionname;
		}
	}
}
