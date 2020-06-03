using System.Collections.Generic;

namespace TranslationChecker
{
	partial class TranslationChecker
	{
		class NamespaceUniquenessAnalyzer : IAnalyzer
		{
			private readonly HashSet<(string @namespace, string locale)> localeNamespaces = 
				new HashSet<(string @namespace, string locale)>();

			public void Analyze(TranslationFile file, ErrorCollector errorCollector)
			{
				var key = (file.Namespace.ToLower(), file.Locale.ToLower());
				if (localeNamespaces.Contains(key))
				{
					errorCollector.AddError($"Another translation file for the namespace \"{file.Namespace}\" " +
					         $"for locale \"{file.Locale}\" already encountered");
				}
				else
				{
					localeNamespaces.Add(key);
				}
			}
		}
	}
}
