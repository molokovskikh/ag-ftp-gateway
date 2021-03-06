﻿using System.Web;
using web_app.Interfaces;

namespace web_app.Models
{
	//Админ
	public class Admin : IWebOperator
	{
		public virtual uint Id { get; set; }

		public virtual string Login { get; set; }

		public virtual bool IsAdmin => true;

		public virtual string Name { get; set; }

		public virtual IWebOperator GetOperator()
		{
			return this;
		}
	}
}