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
		/// <summary>ファイルヘッダーや関数コメントに記述される著者名</summary>
		[DefaultValue("ELIONIX")]
		[LocalizedCategory("Signings")]
		[LocalizedDisplayName("Author")]
		[LocalizedDescription("Author")]
		public string Author { get; set; } = "ELIONIX";

		/// <summary>true: ファイルヘッダーにコピーライト文字列を出力する</summary>
		[DefaultValue(true)]
		[LocalizedCategory("Signings")]
		[LocalizedDisplayName("WritesCopyright")]
		[LocalizedDescription("WritesCopyright")]
		public bool WritesCopyright { get; set; } = true;

		/// <summary>ファイルヘッダーに記述されるコピーライト文言</summary>
		[DefaultValue("Copyright (c) ELIONIX.Inc. All rights reserved.")]
		[LocalizedCategory("Signings")]
		[LocalizedDisplayName("Copyright")]
		[LocalizedDescription("Copyright")]
		public string Copyright { get; set; } = "Copyright (c) ELIONIX.Inc. All rights reserved.";

		/// <summary>
		/// 日付フォーマット。
		/// DateTime.ToStringに渡す引数となる。
		/// </summary>
		[DefaultValue("yyyy-MM-dd")]
		[LocalizedCategory("Formats")]
		[LocalizedDisplayName("DateFormat")]
		[LocalizedDescription("DateFormat")]
		public string DateFormat { get; set; } = "yyyy-MM-dd";

		/// <summary>true: ファイルヘッダーのコメントに装飾を付ける</summary>
		[DefaultValue(true)]
		[LocalizedCategory("Formats")]
		[LocalizedDisplayName("DecoratesFileHeader")]
		[LocalizedDescription("DecoratesFileHeader")]
		public bool DecoratesFileHeader { get; set; } = true;

		/// <summary>true: クラスや関数等のコメントに装飾を付ける</summary>
		[DefaultValue(true)]
		[LocalizedCategory("Formats")]
		[LocalizedDisplayName("DecoratesComment")]
		[LocalizedDescription("DecoratesComment")]
		public bool DecoratesComment { get; set; } = true;
	}
}
