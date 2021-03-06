﻿using System;
using System.DirectoryServices;
using log4net;

namespace web_app.Services
{
	/// <summary>
	/// Для работы с Активной Директорией Microsoft
	/// @todo Клас взят из InternetInterface Прокоментировать и убрать лишнее
	/// </summary>
	public class ActiveDirectoryHelper
	{
		private static readonly ILog _log = LogManager.GetLogger(typeof(ActiveDirectoryHelper));

		private static DirectoryEntry entryAu;
		private static string _path;
		private static string _filterAttribute;
		public static string ErrorMessage;

		/// <summary>
		/// Проверяет, существует ли данный логин в Активной Директории
		/// </summary>
		/// <param name="username">Логин</param>
		/// <returns>True, если существует</returns>
		public static bool IsLoginExist(string username)
		{
			if (FindDirectoryEntry(username) != null)
				return true;
			return false;
		}

		public static bool IsAuthenticated(string username, string pwd)
		{
			if (Authenticated(@"LDAP://OU=Офис,DC=adc,DC=analit,DC=net", username, pwd))
				return true;
			if (Authenticated(@"LDAP://OU=Клиенты,DC=adc,DC=analit,DC=net", username, pwd))
				return true;
			return false;
		}

		public static bool Authenticated(string LDAP, string username, string pwd)
		{
			var domainAndUsername = @"analit\" + username;
			entryAu = new DirectoryEntry(LDAP, domainAndUsername, pwd, AuthenticationTypes.None);
			try {
				// Bind to the native AdsObject to force authentication.
				var obj = entryAu.NativeObject;
				var search = new DirectorySearcher(entryAu);
				search.Filter = "(SAMAccountName=" + username + ")";
				search.PropertiesToLoad.Add("cn");
				SearchResult result = search.FindOne();
				// Update the new path to the user in the directory
				_path = result.Path;
				_filterAttribute = (String)result.Properties["cn"][0];
			}
			catch (Exception ex) {
				_log.Info("Пароль или логин был введен неправильно");
				_log.Info(ErrorMessage);
				ErrorMessage = ex.Message;
				return false;
			}
			entryAu.RefreshCache();
			return true;
		}


		private static DirectoryEntry GetDirectoryEntry(string login)
		{
			var entry = FindDirectoryEntry(login);
			if (entry == null)
				throw new Exception(String.Format("Учетная запись Active Directory {0} не найдена", login));
			return entry;
		}

		public static void ChangePassword(string login, string password)
		{
			var entry = GetDirectoryEntry(login);
			GetDirectoryEntry(login).Invoke("SetPassword", password);
			entry.CommitChanges();
		}

		public static void CreateUserInAD(string login, string password, bool allComputers = false)
		{
#if !DEBUG
			var root = new DirectoryEntry("LDAP://OU=Пользователи,OU=Клиенты,DC=adc,DC=analit,DC=net");
			var userGroup = new DirectoryEntry("LDAP://CN=Базовая группа клиентов - получателей данных,OU=Группы,OU=Клиенты,DC=adc,DC=analit,DC=net");
			var user = root.Children.Add("CN=" + login, "user");
			user.Properties["samAccountName"].Value = login;
			//user.Properties["description"].Value = clientCode.ToString();
			user.CommitChanges();
			user.Invoke("SetPassword", password);
			user.Properties["userAccountControl"].Value = 66048;
			user.CommitChanges();
			userGroup.Invoke("Add", user.Path);
			userGroup.CommitChanges();
			root.CommitChanges();
#endif
		}

		public static DirectoryEntry FindDirectoryEntry(string login)
		{
			using (var searcher = new DirectorySearcher(String.Format(@"(&(objectClass=user)(sAMAccountName={0}))", login))) {
				var searchResult = searcher.FindOne();
				if (searchResult != null)
					return searcher.FindOne().GetDirectoryEntry();
				return null;
			}
		}
	}
}