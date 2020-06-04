using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TranslationChecker
{
	internal class FileContentAnalyzer : IAnalyzer
	{
		private static readonly Regex LocalizationKeyRegex =
			new Regex(@"^([a-zA-Z0-9_]+):([a-zA-Z0-9_-]+)$", RegexOptions.Compiled);

		private readonly QuokkaTemplateAnalyzer quokkaAnalyzer;
		private readonly FormatAnalyzer formatAnalyzer;

		public FileContentAnalyzer()
		{
			quokkaAnalyzer = new QuokkaTemplateAnalyzer();
			formatAnalyzer = new FormatAnalyzer();
		}

		public void Analyze(TranslationFile file, ErrorCollector errorCollector)
		{
			try
			{
				using (var streamReader = new StreamReader(file.Path))
				using (var jsonReader = new JsonTextReader(streamReader))
				{
					var translationData = JObject.Load(jsonReader).ToObject<Dictionary<string, string>>();

					foreach (var (key, translation) in translationData!)
					{
						var match = LocalizationKeyRegex.Match(key);
						if (!match.Success)
						{
							errorCollector.AddError($"Key \"{key}\" is invalid. Valid key format is Namespace:Key");
						}
						else
						{
							var keyNamespace = match.Groups[1].Value;
							if (!string.Equals(keyNamespace, file.Namespace, StringComparison.InvariantCultureIgnoreCase))
							{
								errorCollector.AddError($"Namespace in key \"{key}\" doesn't match the file namespace");
							}
						}

						if (!quokkaAnalyzer.Analyze(translation))
							errorCollector.AddError($"Translation for key \"{key}\" is not a valid Quokka template: \"{translation}\"");
					}
				}
				
				formatAnalyzer.Analyze(file, errorCollector);
			}
			catch (Exception ex)
			{
				errorCollector.AddError(ex.Message);
			}
		}
	}
}
