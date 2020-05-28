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

		public string Path { get; private set; }

		public string Locale { get; private set; }

		public string Namespace { get; private set; }

		public bool IsFrontend { get; private set; }

		public static TranslationFile TryCreateFromFilePath(string filePath)
		{
			var match = TranslationFileRegex.Match(filePath);
			if (!match.Success)
				return null;

			return new TranslationFile
			{
				Path = filePath,
				Locale = match.Groups[2].Value,
				Namespace = match.Groups[1].Value,
				IsFrontend = filePath.Contains(@".Frontend\", StringComparison.InvariantCultureIgnoreCase) ||
					filePath.Contains(@".LegacyFrontend\", StringComparison.InvariantCultureIgnoreCase)
			};
		}
	}
}