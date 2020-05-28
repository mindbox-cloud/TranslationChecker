using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace TranslationChecker
{
	public class FormatAnalyzer
	{
		private static readonly Regex correctLineRegex = new Regex(@"^[{}]|^\t""", RegexOptions.Compiled);

		public void Analyze(TranslationFile file, ErrorCollector errorCollector)
		{
			using (var reader = new StreamReader(file.Path))
			{
				while (!reader.EndOfStream)
				{
					var line = reader.ReadLine();
					if (!string.IsNullOrWhiteSpace(line))
					{
						if (!correctLineRegex.IsMatch(line))
						{
							errorCollector.AddError(
								"Incorrect file format: single Tab character must be used to indent the keys");
							return;
						}
					}
				}
			}
		} 
	}
}
