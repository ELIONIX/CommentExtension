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

namespace CommentGenerator
{
	//********************************************************************************************//
	//--------------------------------------------------------------------------------------------//
	/// <summary>ユーザー設定ページのクラス</summary>
	/// 
	//! @author SAITO Takamasa
	//--------------------------------------------------------------------------------------------//
	public class SettingPage : DialogPage
	{
		[LocalizedCategory("Signings")]
		[LocalizedDisplayName("Author")]
		[LocalizedDescription("Author")]
		public string Author { get; set; } = "ELIONIX";

		[LocalizedCategory("Signings")]
		[LocalizedDisplayName("Copyright")]
		[LocalizedDescription("Copyright")]
		public string Copyright { get; set; } = "Copyright (c) ELIONIX.Inc. All rights reserved.";

		[LocalizedCategory("Formats")]
		[LocalizedDisplayName("DateFormat")]
		[LocalizedDescription("DateFormat")]
		public string DateFormat { get; set; } = "yyyy-MM-dd";
	}
}
