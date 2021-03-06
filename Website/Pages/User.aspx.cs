using System;
using System.Collections.Generic;
using System.Linq;
using EnterpriseWebLibrary;
using EnterpriseWebLibrary.Encryption;
using EnterpriseWebLibrary.EnterpriseWebFramework;
using EnterpriseWebLibrary.EnterpriseWebFramework.Ui;
using EnterpriseWebLibrary.EnterpriseWebFramework.UserManagement;
using EwlRealWorld.Library.DataAccess;
using EwlRealWorld.Library.DataAccess.Modification;
using EwlRealWorld.Library.DataAccess.TableRetrieval;

namespace EwlRealWorld.Website.Pages {
	partial class User: EwfPage {
		partial class Info {
			public override string ResourceName => AppTools.User != null ? "Your Settings" : "Sign up";
		}

		protected override void loadData() {
			if( AppTools.User == null )
				ph.AddControlsReturnThis(
					new EwfHyperlink(
							EnterpriseWebLibrary.EnterpriseWebFramework.EwlRealWorld.Website.UserManagement.LogIn.GetInfo( Home.GetInfo().GetUrl() ),
							new StandardHyperlinkStyle( "Have an account?" ) ).ToCollection()
						.GetControls() );

			var mod = getMod();
			var password = new DataValue<string> { Value = "" };
			Tuple<IReadOnlyCollection<EtherealComponent>, Action<int>> logInHiddenFieldsAndMethod = null;
			FormState.ExecuteWithDataModificationsAndDefaultAction(
				PostBack.CreateFull(
						firstModificationMethod: () => {
							if( AppTools.User == null )
								mod.UserId = MainSequence.GetNextValue();
							if( password.Value.Any() ) {
								var passwordSalter = new Password( password.Value );
								mod.Salt = passwordSalter.Salt;
								mod.SaltedPassword = passwordSalter.ComputeSaltedHash();
							}
							mod.Execute();

							logInHiddenFieldsAndMethod?.Item2( mod.UserId );
						},
						actionGetter: () => new PostBackAction( logInHiddenFieldsAndMethod != null ? (PageInfo)Home.GetInfo() : Profile.GetInfo( AppTools.User.UserId ) ) )
					.ToCollection(),
				() => {
					ph.AddControlsReturnThis( getFormItemStack( mod, password ).ToCollection().GetControls() );
					EwfUiStatics.SetContentFootActions( new ButtonSetup( AppTools.User != null ? "Update Settings" : "Sign up" ).ToCollection() );

					if( AppTools.User == null ) {
						logInHiddenFieldsAndMethod = FormsAuthStatics.GetLogInHiddenFieldsAndSpecifiedUserLogInMethod();
						logInHiddenFieldsAndMethod.Item1.AddEtherealControls( ph );
					}
				} );
		}

		private UsersModification getMod() {
			if( AppTools.User != null )
				return UsersTableRetrieval.GetRowMatchingId( AppTools.User.UserId ).ToModification();

			var mod = UsersModification.CreateForInsert();
			mod.ProfilePictureUrl = "";
			mod.ShortBio = "";
			return mod;
		}

		private FlowComponent getFormItemStack( UsersModification mod, DataValue<string> password ) {
			var stack = FormItemList.CreateStack();
			if( AppTools.User != null )
				stack.AddFormItems( mod.GetProfilePictureUrlUrlControlFormItem( true, label: "URL of profile picture".ToComponents() ) );
			stack.AddFormItems( mod.GetUsernameTextControlFormItem( false, label: "Username".ToComponents(), value: AppTools.User == null ? "" : null ) );
			if( AppTools.User != null )
				stack.AddFormItems(
					mod.GetShortBioTextControlFormItem( true, label: "Short bio about you".ToComponents(), controlSetup: TextControlSetup.Create( numberOfRows: 8 ) ) );
			stack.AddFormItems( mod.GetEmailAddressEmailAddressControlFormItem( false, label: "Email".ToComponents(), value: AppTools.User == null ? "" : null ) );

			if( AppTools.User == null )
				stack.AddFormItems( password.GetPasswordModificationFormItems().ToArray() );
			else {
				var changePasswordChecked = new DataValue<bool>();
				stack.AddFormItems(
					changePasswordChecked.ToFlowCheckbox(
							"Change password".ToComponents(),
							setup: FlowCheckboxSetup.Create(
								nestedContentGetter: () => FormState.ExecuteWithValidationPredicate(
									() => changePasswordChecked.Value,
									() => FormItemList.CreateGrid( 1, items: password.GetPasswordModificationFormItems() ).ToCollection() ) ),
							value: false )
						.ToFormItem() );
			}

			return stack;
		}
	}
}