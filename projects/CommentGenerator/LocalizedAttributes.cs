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

namespace CommentGenerator
{
	//********************************************************************************************//
	//--------------------------------------------------------------------------------------------//
	/// <summary>多言語化可能なCategory属性</summary>
	/// 
	//! @author SAITO Takamasa
	//--------------------------------------------------------------------------------------------//
	public class LocalizedCategoryAttribute : CategoryAttribute
	{
		//------------------------------------------------------------------------------------//
		/// <summary>既定のカテゴリ名でインスタンスを作成</summary>
		/// 
		//! @author SAITO Takamasa
		//------------------------------------------------------------------------------------//
		public LocalizedCategoryAttribute()
			: base()
		{
		}

		//------------------------------------------------------------------------------------//
		/// <summary>指定したカテゴリ名でインスタンスを作成</summary>
		/// 
		/// <param name="category">カテゴリ名</param>
		//! @author SAITO Takamasa
		//------------------------------------------------------------------------------------//
		public LocalizedCategoryAttribute(string category)
			: base(category)
		{
		}

		//------------------------------------------------------------------------------------//
		/// <summary>指定したカテゴリのローカライズされた名前を検索します</summary>
		/// 
		/// <param name="value">検索するカテゴリの識別子</param>
		/// <returns>
		/// ローカライズされたカテゴリ名。
		/// ローカライズされた名前がない場合は null。
		/// </returns>
		//! @author SAITO Takamasa
		//------------------------------------------------------------------------------------//
		protected override string GetLocalizedString(string value)
		{
			return VSPackage.ResourceManager.GetString("Category" + value, VSPackage.Culture);
		}
	}

	//********************************************************************************************//
	//--------------------------------------------------------------------------------------------//
	/// <summary>多言語化可能なDisplayName属性</summary>
	/// 
	//! @author SAITO Takamasa
	//--------------------------------------------------------------------------------------------//
	public class LocalizedDisplayNameAttribute : DisplayNameAttribute
	{
		//------------------------------------------------------------------------------------//
		/// <summary>既定の表示名でインスタンスを作成</summary>
		/// 
		//! @author SAITO Takamasa
		//------------------------------------------------------------------------------------//
		public LocalizedDisplayNameAttribute()
			: base()
		{
		}

		//------------------------------------------------------------------------------------//
		/// <summary>指定した表示名でインスタンスを作成</summary>
		/// 
		/// <param name="displayName">表示名</param>
		//! @author SAITO Takamasa
		//------------------------------------------------------------------------------------//
		public LocalizedDisplayNameAttribute(string displayName)
			: base(displayName)
		{
		}

		/// <summary>ローカライズされた表示名</summary>
		public override string DisplayName 
			=> VSPackage.ResourceManager.GetString("DisplayName" + base.DisplayName, VSPackage.Culture);
	}

	//********************************************************************************************//
	//--------------------------------------------------------------------------------------------//
	/// <summary>多言語化可能なDescription属性</summary>
	/// 
	//! @author SAITO Takamasa
	//--------------------------------------------------------------------------------------------//
	public class LocalizedDescriptionAttribute : DescriptionAttribute
	{
		//------------------------------------------------------------------------------------//
		/// <summary>既定の説明文でインスタンスを作成</summary>
		/// 
		//! @author SAITO Takamasa
		//------------------------------------------------------------------------------------//
		public LocalizedDescriptionAttribute()
			: base()
		{
		}

		//------------------------------------------------------------------------------------//
		/// <summary>指定した説明文でインスタンスを作成</summary>
		/// 
		/// <param name="description">説明文</param>
		//! @author SAITO Takamasa
		//------------------------------------------------------------------------------------//
		public LocalizedDescriptionAttribute(string description)
			: base(description)
		{
		}

		/// <summary>ローカライズされた説明文</summary>
		public override string Description
			=> VSPackage.ResourceManager.GetString("Description" + base.Description, VSPackage.Culture);
	}
}
