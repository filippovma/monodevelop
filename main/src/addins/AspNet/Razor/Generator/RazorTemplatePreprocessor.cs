//
// RazorTemplateFileGenerator.cs
//
// Author:
//       Michael Hutchinson <mhutch@xamarin.com>
//
// Copyright (c) 2012 Xamarin Inc (http://xamarin.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.CodeDom.Compiler;
using System.Linq;
using MonoDevelop.Core;
using MonoDevelop.TextTemplating;
using System.Threading.Tasks;
using MonoDevelop.Ide.CustomTools;
using MonoDevelop.Projects;

namespace MonoDevelop.AspNet.Razor.Generator
{
	class RazorTemplatePreprocessor : ISingleFileCustomTool
	{
		public Task Generate (ProgressMonitor monitor, ProjectFile file, SingleFileCustomToolResult result)
		{
			return Task.Run (delegate {
				try {
					GenerateInternal (monitor, file, result);
				} catch (Exception ex) {
					result.UnhandledException = ex;
				}
			});
		}

		static void GenerateInternal (ProgressMonitor monitor, ProjectFile file, SingleFileCustomToolResult result)
		{
			if (file.Project.SupportedLanguages.All (l => l != "C#")) {
				const string msg = "Razor templates are only supported in C# projects";
				result.Errors.Add (new CompilerError (file.Name, -1, -1, null, msg));
				monitor.Log.WriteLine (msg);
				return;
			}

			var host = new PreprocessedRazorHost (file.FilePath);

			var defaultOutputName = file.FilePath.ChangeExtension (".cs");

			var ns = CustomToolService.GetFileNamespace (file, defaultOutputName);
			host.DefaultNamespace = ns;

			CompilerErrorCollection errors;
			var code = host.GenerateCode (out errors);
			result.Errors.AddRange (errors);

			var writer = new MonoDevelop.DesignerSupport.CodeBehindWriter ();
			writer.WriteFile (defaultOutputName, code);
			writer.WriteOpenFiles ();

			result.GeneratedFilePath = defaultOutputName;

			foreach (var err in result.Errors) {
				monitor.Log.WriteLine (err);
			}
		}
	}
}