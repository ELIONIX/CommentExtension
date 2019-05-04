//************************************************************************************************//
//! @author SAITO Takamasa
//! @date   2019-05-04
//! @note   Copyright (c) ELIONIX.Inc. All rights reserved.
//************************************************************************************************//
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;

namespace CommentExtension
{
	//********************************************************************************************//
	//--------------------------------------------------------------------------------------------//
	/// <summary>ユーザー設定ページのクラス</summary>
	/// 
	//! @author SAITO Takamasa
	//--------------------------------------------------------------------------------------------//
	public class SettingPage : DialogPage
	{
		[Category("Comment Constants")]
		[DisplayName("Copyright")]
		[Description("Copyright notation used for note items in document comments")]
		public string Copyright { get; set; } = "Copyright (c) ELIONIX.Inc. All rights reserved.";

		[Category("Comment Constants")]
		[DisplayName("Author")]
		[Description("Name used for author item of document comment")]
		public string Author { get; set; } = "ELIONIX";

		[Category("Comment Constants")]
		[DisplayName("Date format")]
		[Description("Date format")]
		public string DateFormat { get; set; } = "YYYY-MM-DD";
	}
}
