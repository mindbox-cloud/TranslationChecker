using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace TranslationChecker
{
	public class ProjectInclusionAnalyzer : IAnalyzer
	{
		private readonly string[] ignoredNamespaces;
		private readonly Dictionary<string, bool> includedFiles = new(StringComparer.InvariantCultureIgnoreCase);
		private readonly Dictionary<string, bool> includedWildcards = new(StringComparer.InvariantCultureIgnoreCase);

		public ProjectInclusionAnalyzer(string baseDirectory, string[] ignoredNamespaces)
		{
			this.ignoredNamespaces = ignoredNamespaces;

			var csprojFiles = Directory.GetFiles(baseDirectory, "*.csproj", SearchOption.AllDirectories)
				.Select(path => path.Replace('\\', '/'));

			foreach (var proj in csprojFiles)
			{
				using (var reader = new StreamReader(proj))
				{
					var document = XDocument.Load(reader);

					var i18nFiles = document.Descendants()
						.Where(x => x.Name.LocalName == "ItemGroup")
						.SelectMany(itemGroup => itemGroup.Descendants()
							.Where(y =>
								y.Attribute("Include")
									?.Value
									.EndsWith("i18n.json", StringComparison.InvariantCultureIgnoreCase)
								?? false)
							.Select(node => new
							{
								Path = Path.Combine(
									Path.GetDirectoryName(proj)!,
									node.Attribute("Include").Value)
									.Replace('\\', '/'),
								IsCopiedOnBuild = node.Descendants()
									.Where(y => y.Name.LocalName == "CopyToOutputDirectory")
									.Any(y => y.Value.Trim().Equals("PreserveNewest",
										StringComparison.InvariantCultureIgnoreCase))
							}));

					foreach (var includedFile in i18nFiles)
					{
						if (includedFile.Path.Contains("*")
							|| includedFile.Path.Contains("?"))
						{
							includedWildcards.Add(includedFile.Path, includedFile.IsCopiedOnBuild); 

						}
						else
						{
							includedFiles.Add(includedFile.Path, includedFile.IsCopiedOnBuild);
						}
					}
				}
			}
		}

		public void Analyze(TranslationFile file, ErrorCollector errorCollector)
		{
			if (ignoredNamespaces.Contains(file.Namespace))
				return;

			var isCopiedOnBuild =
				IsCopiedDirectly(includedFiles, file.Path)
				?? IsCopiedViaWildcard(includedWildcards, file.Path);

			if (isCopiedOnBuild == null)
			{
				errorCollector.AddError("File is not included in any project");
			}
			else
			{
				if (!isCopiedOnBuild.Value)
					errorCollector.AddError("\"Copy to output directory\" option must be set to \"Copy if newer\"");
			}
		}

		private static bool? IsCopiedDirectly(
			Dictionary<string, bool> includedFiles,
			string path)
			=> !includedFiles.TryGetValue(path, out var isCopiedOnBuild)
				? null as bool?
				: isCopiedOnBuild;

		private static bool? IsCopiedViaWildcard(
			Dictionary<string, bool> includedWildcards,
			string path)
			=> includedWildcards
				.Where(x => IsWildcardMatch(x.Key, path))
				.Select(x => x.Value as bool?)
				.FirstOrDefault();

		private static bool IsWildcardMatch(string wildcard, string path)
		{
			var regex =
				"^"
				+ wildcard
					.Replace("\\", "\\\\")
					.Replace(".", "\\.")
					.Replace("*", ".*")
					.Replace("?", ".")
				+ "$";
			return Regex.IsMatch(path, regex);
		}
	}
}