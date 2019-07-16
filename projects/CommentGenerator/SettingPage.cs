//================================================================================================//
//! @author SAITO Takamasa
//! @date   2019-05-04
//! @note   Copyright (c) ELIONIX.Inc. All rights reserved.
//================================================================================================//
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;

namespace CommentGenerator
{
	//============================================================================================//
	//--------------------------------------------------------------------------------------------//
	/// <summary>
	/// ユーザー設定ページのクラス
	/// </summary>
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

		/// <summary>ファイルヘッダーに記述されるコピーライト文言</summary>
		[DefaultValue("Copyright (c) ELIONIX.Inc. All rights reserved.")]
		[LocalizedCategory("Signings")]
		[LocalizedDisplayName("Copyright")]
		[LocalizedDescription("Copyright")]
		public string Copyright { get; set; } = "Copyright (c) ELIONIX.Inc. All rights reserved.";


		/// <summary>true: ファイルヘッダーにコピーライト文字列を出力する</summary>
		[DefaultValue(true)]
		[LocalizedCategory("Formats")]
		[LocalizedDisplayName("WritesCopyright")]
		[LocalizedDescription("WritesCopyright")]
		public bool WritesCopyright { get; set; } = true;

		/// <summary>著者の記述を行うかどうか</summary>
		[DefaultValue((object)AuthorSignKind.Yes)]
		[LocalizedCategory("Formats")]
		[LocalizedDisplayName("SignsAuthor")]
		[LocalizedDescription("SignsAuthor")]
		public AuthorSignKind SignsAuthor { get; set; } = AuthorSignKind.Yes;

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

	/// <summary>
	/// 著者のコメントを記述するかどうか
	/// </summary>
	//! @author SAITO Takamasa
	public enum AuthorSignKind
	{
		/// <summary>記述する</summary>
		Yes,

		/// <summary>ファイルヘッダーにのみ記述</summary>
		OnlyHeader,

		/// <summary>記述しない</summary>
		No,
	}
}
