﻿using System.Collections.Generic;
using System.Linq;
using EnterpriseWebLibrary;
using EnterpriseWebLibrary.EnterpriseWebFramework;
using EwlRealWorld.Library;
using EwlRealWorld.Library.DataAccess.CommandConditions;
using EwlRealWorld.Library.DataAccess.Modification;
using EwlRealWorld.Library.DataAccess.Retrieval;
using EwlRealWorld.Library.DataAccess.TableRetrieval;
using EwlRealWorld.Website.Pages;

namespace EwlRealWorld.Website {
	internal static class AppStatics {
		internal static IReadOnlyCollection<FlowComponent> GetArticleDisplay(
			ArticlesRetrieval.Row article, Dictionary<int, UsersTableRetrieval.Row> usersById, ILookup<int, ArticleTagsTableRetrieval.Row> tagsByArticleId,
			ILookup<int, FavoritesTableRetrieval.Row> favoritesByArticleId ) {
			var components = new List<FlowComponent>();
			components.Add(
				new GenericFlowContainer(
					GetAuthorDisplay( article, usersById[ article.AuthorId ] ).Append( getFavoriteActionComponent( article, favoritesByArticleId ) ).Materialize(),
					classes: ElementClasses.ArticleListAuthorAndFavorite ) );
			components.Add( new Section( article.Title, new Paragraph( article.Description.ToComponents() ).ToCollection() ) );

			TagsTableRetrieval.GetRows(); // prime cache for GetTagDisplay
			components.Add(
				new GenericFlowContainer(
					new GenericPhrasingContainer( "Read more...".ToComponents() ).ToCollection()
						.Concat( GetTagDisplay( article.ArticleId, tagsByArticleId[ article.ArticleId ] ) )
						.Materialize(),
					classes: ElementClasses.ArticleListDetail ) );

			return components;
		}

		internal static IReadOnlyCollection<FlowComponent> GetAuthorDisplay( ArticlesRetrieval.Row article, UsersTableRetrieval.Row author ) =>
			new GenericFlowContainer(
				new EwfHyperlink(
						Profile.GetInfo( article.AuthorId ),
						new ImageHyperlinkStyle(
							new ExternalResourceInfo(
								author.ProfilePictureUrl.Any() ? author.ProfilePictureUrl : "https://static.productionready.io/images/smiley-cyrus.jpg" ),
							"" ) ).ToCollection<PhrasingComponent>()
					.Append(
						new GenericPhrasingContainer(
							new EwfHyperlink( Profile.GetInfo( article.AuthorId ), new StandardHyperlinkStyle( author.Username ) ).ToCollection<PhrasingComponent>()
								.Append( new LineBreak() )
								.Append(
									new GenericPhrasingContainer( article.CreationDateAndTime.ToDayMonthYearString( false ).ToComponents(), classes: ElementClasses.Date ) )
								.Materialize() ) )
					.Materialize(),
				classes: ElementClasses.Author ).ToCollection();

		private static PhrasingComponent getFavoriteActionComponent(
			ArticlesRetrieval.Row article, ILookup<int, FavoritesTableRetrieval.Row> favoritesByArticleId ) {
			var count = favoritesByArticleId[ article.ArticleId ].Count().ToString();

			if( AppTools.User == null )
				return new EwfHyperlink( User.GetInfo(), new StandardHyperlinkStyle( count, icon: new ActionComponentIcon( new FontAwesomeIcon( "fa-heart-o" ) ) ) );

			var rs = new UpdateRegionSet();
			EwfButton button;
			if( FavoritesTableRetrieval.GetRowMatchingPk( AppTools.User.UserId, article.ArticleId, returnNullIfNoMatch: true ) == null )
				button = new EwfButton(
					new StandardButtonStyle( count, icon: new ActionComponentIcon( new FontAwesomeIcon( "fa-heart-o" ) ) ),
					behavior: new PostBackBehavior(
						postBack: PostBack.CreateIntermediate(
							rs.ToCollection(),
							id: "favorite",
							firstModificationMethod: () => FavoritesModification.InsertRow( AppTools.User.UserId, article.ArticleId ) ) ) );
			else
				button = new EwfButton(
					new StandardButtonStyle( count, icon: new ActionComponentIcon( new FontAwesomeIcon( "fa-heart" ) ) ),
					behavior: new PostBackBehavior(
						postBack: PostBack.CreateIntermediate(
							rs.ToCollection(),
							id: "unfavorite",
							firstModificationMethod: () => FavoritesModification.DeleteRows(
								new FavoritesTableEqualityConditions.UserId( AppTools.User.UserId ),
								new FavoritesTableEqualityConditions.ArticleId( article.ArticleId ) ) ) ) );
			return new PhrasingIdContainer( button.ToCollection(), updateRegionSets: rs.ToCollection() );
		}

		internal static IReadOnlyCollection<FlowComponent> GetTagDisplay( int articleId, IEnumerable<ArticleTagsTableRetrieval.Row> tags ) =>
			new LineList(
				tags.OrderByTagId().Select( i => (LineListItem)TagsTableRetrieval.GetRowMatchingId( i.TagId ).TagName.ToComponents().ToComponentListItem() ),
				generalSetup: new ComponentListSetup( classes: ElementClasses.Tag ) ).ToCollection();
	}
}