//************************************************************************************************//
//! @author SAITO Takamasa
//! @date   2019-05-03
//! @note   Copyright (c) ELIONIX.Inc. All rights reserved.
//************************************************************************************************//
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace CommentGenerator
{
	/// <summary>
	/// Command handler
	/// </summary>
	internal sealed class GenerateCommentCommand
	{
		/// <summary>
		/// Command ID.
		/// </summary>
		public const int CommandId = 0x0100;

		/// <summary>
		/// Command menu group (command set GUID).
		/// </summary>
		public static readonly Guid CommandSet = new Guid("eb516ee8-3a5f-4230-8814-a9cbd50587eb");

		/// <summary>
		/// VS Package that provides this command, not null.
		/// </summary>
		private readonly AsyncPackage package;

		/// <summary>
		/// Initializes a new instance of the <see cref="GenerateCommentCommand"/> class.
		/// Adds our command handlers for menu (commands must exist in the command table file)
		/// </summary>
		/// <param name="package">Owner package, not null.</param>
		/// <param name="commandService">Command service to add command to, not null.</param>
		private GenerateCommentCommand(AsyncPackage package, OleMenuCommandService commandService)
		{
			this.package = package ?? throw new ArgumentNullException(nameof(package));
			commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

			var menuCommandID = new CommandID(CommandSet, CommandId);
			var menuItem = new MenuCommand(this.Execute, menuCommandID);
			commandService.AddCommand(menuItem);
		}

		/// <summary>
		/// Gets the instance of the command.
		/// </summary>
		public static GenerateCommentCommand Instance
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets the service provider from the owner package.
		/// </summary>
		private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider
		{
			get
			{
				return this.package;
			}
		}

		/// <summary>改行文字を表す定数</summary>
		static private readonly string newLine_ = System.Environment.NewLine;
		/// <summary>拡張機能を使用する人の名前</summary>
		private string author_ = "";
		/// <summary>コピーライト表記</summary>
		private string copyright_ = "";
		/// <summary>日付文字列の書式</summary>
		private string dateFormat_ = "";

		/// <summary>
		/// Initializes the singleton instance of the command.
		/// </summary>
		/// <param name="package">Owner package, not null.</param>
		public static async Task InitializeAsync(AsyncPackage package)
		{
			// Switch to the main thread - the call to AddCommand in GenerateCommentCommand's constructor requires
			// the UI thread.
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

			OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
			Instance = new GenerateCommentCommand(package, commandService);
		}

		/// <summary>
		/// This function is the callback used to execute the command when the menu item is clicked.
		/// See the constructor to see how the menu item is associated with this function using
		/// OleMenuCommandService service and MenuCommand class.
		/// </summary>
		/// <param name="sender">Event sender.</param>
		/// <param name="e">Event args.</param>
		private void Execute(object sender, EventArgs e)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			var package = this.package as CommentExtensionPackage;
			author_ = package.Setting.Author;
			copyright_ = package.Setting.Copyright;
			dateFormat_ = package.Setting.DateFormat;

			GenerateComment();
		}

		private void GenerateComment()
		{
			ThreadHelper.ThrowIfNotOnUIThread();
#pragma warning disable VSTHRD002 // 問題のある同期待機を避ける
			DTE dte = (DTE)ServiceProvider.GetServiceAsync(typeof(DTE))?.Result;
			if (dte is null) {
				return;
			}
#pragma warning restore VSTHRD002 // 問題のある同期待機を避ける

			string fileName = dte.ActiveDocument.Name;
			if (fileName is null) {
				return;
			}

			//ファイルの種類により分岐
			if (System.IO.Path.GetExtension(dte.ActiveDocument.Name) == ".cs") {
				GenerateCSharpComment(dte);
			} else {
				//それ以外のやつの時はエラーメッセージ
				VsShellUtilities.ShowMessageBox(
					this.package,
					VSPackage.ErrorNoTarget,
					VSPackage.ErrorMessageTitle,
					OLEMSGICON.OLEMSGICON_CRITICAL,
					OLEMSGBUTTON.OLEMSGBUTTON_OK,
					OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
			}
		}

		private bool CheckElement(
			FileCodeModel fcm,
			TextSelection ts,
			vsCMElement target,
			Action<CodeElement> generator)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			//以前はfcm.CodeElementFrompoint呼び出し時に
			//ActivePointが指定と違う種類だった場合nullが返ってきた様だが（サンプルを見た感じ）
			//いつの間にかCOMException例外が飛ぶようになっている
			//例外が飛んだら何もしないで次の項目の判定へと進む

			try {
				CodeElement element = fcm.CodeElementFromPoint(ts.ActivePoint, target);
				if (element != null && element.Kind == target) {
					generator?.Invoke(element);
					return true;
				}
			} catch (COMException) {
				//何もしないで次の項目の判定へ
			}

			return false;
		}

		private void GenerateCSharpComment(DTE dte)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			//テキストエディタを操作するためのオブジェクト
			TextSelection ts = (TextSelection)dte.ActiveDocument.Selection;

			//ファイルの先頭行にカーソルがある場合は、
			//fileコメントを生成する
			if (ts.TopPoint.Line == 1 && ts.Text.Length == 0) {
				GenerateFileComment(ts);
				return;
			}

			//現在のカーソル位置がC#構文のどの要素に相当するかを判別するためのオブジェクト
			FileCodeModel fcm = dte.ActiveDocument.ProjectItem.FileCodeModel;

			//ここから現在のカーソル位置がC#構文のどの要素にあたるかを判別する
			//スコープの小さいものから判別していかないと、
			//関数宣言の上にあるのにclass内部と認識されてclassのコメントが生成されるようなことが起こってしまう

			//また、以前はfcm.CodeElementFrompoint呼び出し時に
			//ActivePointが指定と違う種類だった場合nullが返ってきた様だが（サンプルを見た感じ）
			//いつの間にかCOMException例外が飛ぶようになっている
			//例外が飛んだら何もしないで次の項目の判定へと進む

			//フィールド
			if (CheckElement(fcm, ts, vsCMElement.vsCMElementVariable, element => GenerateFieldComment(ts, element))) {
				return;
			}

			//プロパティ
			if (CheckElement(fcm, ts, vsCMElement.vsCMElementProperty, element => GeneratePropertyComment(ts, element))) {
				return;
			}

			//イベント
			//vsCMElementEventにしてもvsCMElementEventsDeclarationにしても引っかからない。謎。
			if (CheckElement(fcm, ts, vsCMElement.vsCMElementEvent, element => GenerateEventComment(ts, element))) {
				return;
			}

			//デリゲート
			if (CheckElement(fcm, ts, vsCMElement.vsCMElementDelegate, element => GenerateDelegateComment(ts, element))) {
				return;
			}

			//関数
			if (CheckElement(fcm, ts, vsCMElement.vsCMElementFunction, element => GenerateFunctionComment(ts, element))) {
				return;
			}

			//列挙型
			if (CheckElement(fcm, ts, vsCMElement.vsCMElementEnum, element => GenerateEnumComment(ts, element))) {
				return;
			}

			//構造体
			if (CheckElement(fcm, ts, vsCMElement.vsCMElementStruct, element => GenerateStructComment(ts, element))) {
				return;
			}

			//interface
			if (CheckElement(fcm, ts, vsCMElement.vsCMElementInterface, element => GenerateInterfaceComment(ts, element))) {
				return;
			}

			//class
			if (CheckElement(fcm, ts, vsCMElement.vsCMElementClass, element => GenerateClassComment(ts, element))) {
				return;
			}

			//名前空間はxmlドキュメントコメントが対応するまでコメントの追加をしないことに↓。
			/*
			if (CheckElement(fcm, ts, vsCMElement.vsCMElementNamespace, element => GenerateNameSpaceComment(ts, element, dte))) {
				return;
			}*/

			//それ以外のやつの時はエラーメッセージ
			VsShellUtilities.ShowMessageBox(
				this.package,
				VSPackage.ErrorNoTarget,
				VSPackage.ErrorMessageTitle,
				OLEMSGICON.OLEMSGICON_CRITICAL,
				OLEMSGBUTTON.OLEMSGBUTTON_OK,
				OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
		}

		private string GetIndentText(TextSelection ts)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			//tsが要素の先頭に移動済みであること

			//要素の先頭の位置が行の先頭からどの位置にあるかを取得
			var point = ts.ActivePoint;
			int offset = point.LineCharOffset;

			//行の先頭のindexが1のようで要素の前にある文字数はoffset - 1
			//その分を選択する
			if (offset <= 1) {
				return "";
			}

			ts.CharLeft(true, offset - 1);

			//選択されている文字がインデントとなる
			return ts.Text;
		}

		private void InsertComments(TextSelection ts, List<string> comments, string indent)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			//改行も含めて文字列を作成してしまって
			//まとめて出力
			//１行１行出力するとコードレンズが重いため

			//しかもこのやり方なら
			//間違えたコメントを挿入してしまったとしても
			//Ctrl + Z一発で消える
			StringBuilder sb = new StringBuilder();
			foreach (string str in comments) {
				//最初はインデントされた位置にカーソルがあるため
				//先に文字列を加えた後、改行してからインデント
				//ループの最後にもインデントがいる
				//元々あった関数先頭行等の要素をインデントさせるため
				sb.Append(str);
				sb.Append(newLine_);
				sb.Append(indent);
			}

			//実際に出力
			ts.Insert(sb.ToString());
		}

		private void GenerateFileComment(TextSelection ts)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			//１行１行出力していくとコードレンズが重いので
			//他の所と同じようにまとめて出力するように切り替える

			List<string> comments = new List<string> {
				"//************************************************************************************************//",
				"//! @author " + author_,
				"//! @date   " + DateTime.Now.ToString(dateFormat_),
				"//! @note   " + copyright_,
				"//************************************************************************************************//"
			};

			//ファイルの先頭に移動
			ts.StartOfLine();
			//ファイルコメントは確実にインデントは無い
			InsertComments(ts, comments, "");
		}

		private void GenerateFunctionComment(TextSelection ts, CodeElement element)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			CodeFunction functionInfo = (CodeFunction)element;

			List<string> comments = new List<string> {
				"//------------------------------------------------------------------------------------//",
				"/// <summary>" + "</summary>",
				"/// "
			};

			//ジェネリックな関数の場合typeparamコメントを追加する必要があるが
			//関数が使用しているgeneric typeを取得するプロパティが無い
			//FullNameプロパティにおいてジェネリック型まで含めた関数の宣言部分が取得できるので
			//その文字列を分解してどうにか抜き出す事にする
			//ちなみにNameプロパティは関数名しか取れないのでだめ
			string fullName = functionInfo.FullName;
			//このfullnameには、場合によっては名前空間やクラス名による修飾も含まれるため
			//Hoge<T>.GetValueとなっていた場合、Tや.GetValueが誤検出される
			//実際に関数名となる、最後の.から右だけを取得するようにする
			//.で分割して最後の要素だけを取るようにする
			var elementNames = fullName.Split('.');
			if (elementNames.Length > 0) {
				string name = elementNames[elementNames.Length - 1];

				var typeNames = name.Split('<', ',', '>');
				//一つ名は関数名の部分なので除外
				for (int i = 1; i < typeNames.Length; ++i) {
					//Splitした時に（多分）>の後の部分が空文字として配列に入ってくるのでこれは除外
					if (typeNames[i] != "") {
						//特に複数のパラメーターを使用している場合
						//前後にスペースがついてくる事があるので取り除く
						string typeName = typeNames[i].Trim(' ');
						comments.Add("/// <typeparam name=\"" + typeName + "\">" + "</typeparam>");
					}
				}
			}

			for (int j = 1; j <= functionInfo.Parameters.Count; ++j) {
				CodeParameter paramInfo = (CodeParameter)functionInfo.Parameters.Item(j);
				comments.Add("/// <param name=\"" + paramInfo.Name + "\">" + "</param>");
			}
			comments.Add("/// <returns>" + "</returns>");
			comments.Add("/// <exception cref=\"Exception\">" + "</exception>");
			comments.Add("//! @author " + author_);
			comments.Add("//------------------------------------------------------------------------------------//");

			//要素の先頭へ移動
			ts.MoveToPoint(functionInfo.StartPoint);
			//インデントの取得
			string indent = GetIndentText(ts);
			//indentの取得処理でカーソル位置が移動されるので、再度要素の先頭へ移動
			ts.MoveToPoint(functionInfo.StartPoint);

			//コメントの挿入
			InsertComments(ts, comments, indent);
		}

		private void GenerateMemberCommentCore(TextSelection ts, CodeElement element)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			List<string> comments = new List<string> {
				"/// <summary>" + "</summary>"
			};

			//再度要素の先頭へ移動
			ts.MoveToPoint(element.StartPoint);
			//インデントの取得
			string indent = GetIndentText(ts);
			//indentの取得処理でカーソル位置が移動されるので、再度要素の先頭へ移動
			ts.MoveToPoint(element.StartPoint);

			//コメントの挿入
			InsertComments(ts, comments, indent);
		}

		private void GenerateFieldComment(TextSelection ts, CodeElement element)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			GenerateMemberCommentCore(ts, element);
		}

		private void GenerateEventComment(TextSelection ts, CodeElement element)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			GenerateMemberCommentCore(ts, element);
		}

		private void GeneratePropertyComment(TextSelection ts, CodeElement element)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			//ここに来る要素には、プロパティとインデクサの２種類がある
			//CodePropertyだとインデクサの添え字情報が取れないため
			//EnvDTE80.CodeProperty2にキャストする
			EnvDTE80.CodeProperty2 propertyInfo = (EnvDTE80.CodeProperty2)element;

			//インデクサはプロパティ名がthisになっている
			string name = propertyInfo.Name;
			if (name != "this") {
				//通常のプロパティ
				GenerateMemberCommentCore(ts, element);
			} else {
				//インデクサとして生成
				GenerateIndexerComment(ts, propertyInfo);
			}
		}

		private void GenerateIndexerComment(TextSelection ts, EnvDTE80.CodeProperty2 propertyInfo)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			List<string> comments = new List<string> {
				"//------------------------------------------------------------------------------------//",
				"/// <summary>" + "</summary>",
				"/// "
			};

			for (int j = 1; j <= propertyInfo.Parameters.Count; ++j) {
				CodeParameter paramInfo = (CodeParameter)propertyInfo.Parameters.Item(j);
				comments.Add("/// <param name=\"" + paramInfo.Name + "\">" + "</param>");
			}

			comments.Add("/// <returns>" + "</returns>");
			comments.Add("//! @author " + author_);
			comments.Add("//------------------------------------------------------------------------------------//");

			//要素の先頭へ移動
			ts.MoveToPoint(propertyInfo.StartPoint);
			//インデントの取得
			string indent = GetIndentText(ts);
			//indentの取得処理でカーソル位置が移動されるので、再度要素の先頭へ移動
			ts.MoveToPoint(propertyInfo.StartPoint);

			//コメントの挿入
			InsertComments(ts, comments, indent);
		}

		private void GenerateDelegateComment(TextSelection ts, CodeElement element)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			CodeDelegate delegateInfo = (CodeDelegate)element;

			List<string> comments = new List<string> {
				"//------------------------------------------------------------------------------------//",
				"/// <summary>" + "</summary>",
				"/// "
			};

			//FxCopの規約により引き数名はsenderとeになっているはずなので決め打ちする
			//と行きたいところだけど、
			//event型以外のdelegateがある可能性もあるので
			//変数名はちゃんと取得する
			for (int j = 1; j <= delegateInfo.Parameters.Count; ++j) {
				CodeParameter paramInfo = (CodeParameter)delegateInfo.Parameters.Item(j);
				//senderの時は定型句を入れる
				if (paramInfo.Name == "sender") {
					comments.Add("/// <param name=\"sender\">イベントを発行したオブジェクト</param>");
				} else {
					comments.Add("/// <param name=\"" + paramInfo.Name + "\">" + "</param>");
				}
			}

			comments.Add("//! @author " + author_);
			comments.Add("//------------------------------------------------------------------------------------//");

			//定義の先頭へ移動
			ts.MoveToPoint(element.StartPoint);
			//インデントの取得
			string indent = GetIndentText(ts);
			//indentの取得処理でカーソル位置が移動されるので、再度要素の先頭へ移動
			ts.MoveToPoint(element.StartPoint);

			//名前空間のインデント分
			InsertComments(ts, comments, indent);
		}

		private void GenerateClassCommentCore(TextSelection ts, CodeElement element, string fullName)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			List<string> comments = new List<string> {
				"//********************************************************************************************//",
				"//--------------------------------------------------------------------------------------------//",
				"/// <summary>" + "</summary>",
				"/// ",
				"/// <remarks>",
				"/// ",
				"/// </remarks>"
			};

			//ジェネリックなクラスの場合もジェネリック関数の時と同じ
			var elementNames = fullName.Split('.');
			if (elementNames.Length > 0) {
				string name = elementNames[elementNames.Length - 1];

				var typeNames = name.Split('<', ',', '>');
				//一つ名は関数名の部分なので除外
				for (int i = 1; i < typeNames.Length; ++i) {
					//Splitした時に（多分）>の後の部分が空文字として配列に入ってくるのでこれは除外
					if (typeNames[i] != "") {
						//特に複数のパラメーターを使用している場合
						//前後にスペースがついてくる事があるので取り除く
						string typeName = typeNames[i].Trim(' ');
						comments.Add("/// <typeparam name=\"" + typeName + "\">" + "</typeparam>");
					}
				}
			}

			comments.Add("//! @author " + author_);
			comments.Add("//--------------------------------------------------------------------------------------------//");

			//クラス宣言の先頭へ移動
			ts.MoveToPoint(element.StartPoint);
			//インデントの取得
			string indent = GetIndentText(ts);
			//indentの取得処理でカーソル位置が移動されるので、再度要素の先頭へ移動
			ts.MoveToPoint(element.StartPoint);

			//名前空間のインデント分
			InsertComments(ts, comments, indent);
		}

		private void GenerateInterfaceComment(TextSelection ts, CodeElement element)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			CodeInterface interfaceInfo = (CodeInterface)element;
			string fullName = interfaceInfo.FullName;
			GenerateClassCommentCore(ts, element, fullName);
		}

		private void GenerateClassComment(TextSelection ts, CodeElement element)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			CodeClass classInfo = (CodeClass)element;
			string fullName = classInfo.FullName;
			GenerateClassCommentCore(ts, element, fullName);
		}

		private void GenerateEnumCommentCore(TextSelection ts, CodeElement element)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			List<string> comments = new List<string> {
				"/// <summary>",
				"/// ",
				"/// </summary>",
				"//! @author " + author_
			};

			//enum宣言の先頭へ移動
			ts.MoveToPoint(element.StartPoint);
			//インデントの取得
			string indent = GetIndentText(ts);
			//indentの取得処理でカーソル位置が移動されるので、再度要素の先頭へ移動
			ts.MoveToPoint(element.StartPoint);

			//名前空間のインデント分
			InsertComments(ts, comments, indent);
		}

		private void GenerateEnumComment(TextSelection ts, CodeElement element)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			GenerateEnumCommentCore(ts, element);
		}

		private void GenerateStructComment(TextSelection ts, CodeElement element)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			GenerateEnumCommentCore(ts, element);
		}

		//名前空間に再対応するときのために取っておくが、
		//警告対策のために全コメントアウト
		/*
		private void GenerateNameSpaceComment(TextSelection ts, CodeElement element, DTE dte)
		{
			CodeNamespace nameSpaceInfo = (CodeNamespace)element;
			string nameSpace = nameSpaceInfo.Name;

			//ソリューションフォルダに、名前空間のコメントを定義したファイルがあるかどうかを探し
			//今回の名前空間に対応する情報があるかどうかを探す
			string solutionPath = GetSolutionPath(dte);
			string nscdefFilePath = solutionPath + @"\" + nameSpaceCommentDefFileName_;
			NameSpaceInfo info = null;
			NameSpaceDefFile defFile;
			if (File.Exists(nscdefFilePath) == true) {
				//読み込む
				defFile = NameSpaceDefFile.Load(nscdefFilePath);

				//今回コメント対象となっている名前空間の情報があるかどうか
				info = defFile.NameSpaceInformations.Find((e) => e.NameSpace == nameSpace);
			} else {
				defFile = new NameSpaceDefFile();
			}

			//名前空間の情報からコメントを取得する
			//ファイルが無い場合とファイル無いに情報が無い場合はinfoがnullである
			//その時は、今入力して貰う。
			string comment = "";
			if (info != null) {
				comment = info.Comment;
			} else {
				using (var form = new NameSpaceCommentInputForm(nameSpace)) {
					//モーダル呼び出し
					if (form.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
						comment = form.Comment;
						//さらにファイルに情報を追加して保存
						defFile.NameSpaceInformations.Add(new NameSpaceInfo(nameSpace, comment));
						defFile.Save(nscdefFilePath);
					} else {
						//コメントを追加せず終了
						return;
					}
				}

			}

			List<string> comments = new List<string>();
			comments.Add("//! @brief " + comment);

			//名前空間定義の先頭へ移動
			ts.MoveToPoint(element.StartPoint);
			//インデントの取得
			string indent = GetIndentText(ts);
			//indentの取得処理でカーソル位置が移動されるので、再度要素の先頭へ移動
			ts.MoveToPoint(element.StartPoint);

			//インデント無し
			InsertComments(ts, comments, indent);
		}


		private string GetSolutionPath(DTE dte)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			//右クリックされたファイルのパス
			string currentPath = dte.ActiveDocument.Path;

			//ファイルがあるパスから上に辿っていき
			//slnファイルを探す

			//全ファイルチェックは時間がかかるので
			//projectsフォルダがある場合は一気にその上のフォルダに移動することにする
			//パスからprojectsという文字を探すが
			//submoduleとかの場合は複数ある可能性もあるし
			//一番右の一致を探す
			int index = currentPath.LastIndexOf("projects");
			if (index >= 0) {
				//indexは0からなのでindexがnの場合
				//projectsの先頭のpがあるのはn+1文字目
				//それ以降の文字列を削除するには先頭からn文字を取れば良い
				currentPath = currentPath.Substring(0, index);
			}

			string tempDirecotry = Path.GetDirectoryName(currentPath);

			while (true) {
				var files = Directory.GetFiles(tempDirecotry);
				foreach (var file in files) {
					if (Path.GetExtension(file) == ".sln") {
						return tempDirecotry;
					}
				}

				//一つ上の階層へ
				var directoryInfo = Directory.GetParent(tempDirecotry);
				if (directoryInfo == null) {
					//ルートだった場合はnullが返るので
					//その時はslnファイルが見つからなかったと言うこと
					return "";
				}
				tempDirecotry = directoryInfo.FullName;
			}
		}
		*/
	}
}
