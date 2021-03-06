using System.Collections.Generic;
using System.Linq;
using EnterpriseWebLibrary;

namespace EwlRealWorld.Library.DataAccess.TableRetrieval {
	partial class TagsTableRetrieval {
		public static Row MatchingName( this IEnumerable<Row> rows, string name ) => rows.SingleOrDefault( i => i.TagName.EqualsIgnoreCase( name ) );
	}
}