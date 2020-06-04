using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TranslationChecker
{
	public class TranslationFile
	{
		public const string TranslationFileSuffix = ".i18n.json";

		private static readonly Regex TranslationFileRegex = new Regex(
			$@"([^\\\/]+)\.([^\\\/]+){Regex.Escape(TranslationFileSuffix)}$",
			RegexOptions.IgnoreCase | RegexOptions.Compiled);

		private TranslationFile(string path, string locale, string @namespace)
		{
			Path = path;
			Locale = locale;
			Namespace = @namespace;
		}

		public string Path { get; }

		public string Locale { get; }

		public string Namespace { get; }

		public static TranslationFile? TryCreateFromFilePath(string filePath)
		{
			var match = TranslationFileRegex.Match(filePath);
			if (!match.Success)
				return null;

			return new TranslationFile
			(
				filePath,
				match.Groups[2].Value,
				match.Groups[1].Value
			);
		}
	}
}