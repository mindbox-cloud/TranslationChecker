using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace TranslationChecker
{
	partial class TranslationChecker
	{
		static int Main(string[] args)
		{
			var errorsOccured = false;
			var launchParameters = LaunchParameters.FromCommandLineArguments(args);
			if (launchParameters == null)
				return -1;

			var baseDirectory = launchParameters.BaseDirectory;

			Console.WriteLine($"Working directory: {baseDirectory}");

			var analyzers = new List<IAnalyzer>();

			analyzers.Add(new FileContentAnalyzer());
			if (!launchParameters.SkipProjectInclusionCheck)
				analyzers.Add(new ProjectInclusionAnalyzer(baseDirectory));

			analyzers.Add(new NamespaceUniquenessAnalyzer());
			
			Console.WriteLine("Searching for translation files...");

			var i18nFiles = LocateInternationalizationFiles(baseDirectory);

			foreach (var translationFilePath in i18nFiles)
			{
				var errorCollector = new ErrorCollector();

				var translationFile = TranslationFile.TryCreateFromFilePath(translationFilePath);
				if (translationFile == null)
				{
					errorCollector.AddError("Could not retrieve locale and namespace from file path.");
				}
				else
				{
					foreach (var analyzer in analyzers)
					{
						analyzer.Analyze(translationFile, errorCollector);
					}
				}

				if (errorCollector.Errors.Any())
				{
					errorsOccured = true;
					LogError($"{Path.GetRelativePath(baseDirectory, translationFilePath)}:");
					foreach (var error in errorCollector.Errors)
						LogError(error);
				}
			}

			if (errorsOccured)
			{
				Console.WriteLine("Error: there are problems with some translation files.");
				Console.WriteLine("Inspect the log messages above.");
				return -1;
			}

			Console.WriteLine("Success: no problems found with translation files.");
			return 0;
		}

		private class LaunchParameters
		{
			public static LaunchParameters FromCommandLineArguments(string[] args)
			{
				if (args.Length == 0)
				{
					Console.WriteLine("Required arguments <SolutionFolderPath> [--skipInclusionCheck]");
					return null;
				}

				return new LaunchParameters
				{
					BaseDirectory = args[0],
					SkipProjectInclusionCheck = args.Any(arg => arg == "--skipInclusionCheck")
				};
			}

			private LaunchParameters()
			{
				// empty
			}

			public string BaseDirectory { get; private set; }

			public bool SkipProjectInclusionCheck { get; private set; }
		}

		private static void LogError(string message)
		{
			ConsoleColor originalColor = Console.ForegroundColor;
			Console.ForegroundColor = ConsoleColor.Red;

			Console.Error.WriteLine(message);

			Console.ForegroundColor = originalColor;
		}

		private static List<string> LocateInternationalizationFiles(string baseDirectory)
		{
			var ignoredPathRules = new[]
			{
				"/bin/",
				"/obj/",
				"/IntegrationTestsSources/",
				"/TestResults/",
				"/node_modules/"
			};

			var i18nFiles = Directory.GetFiles(
					baseDirectory,
					$"*{TranslationFile.TranslationFileSuffix}",
					SearchOption.AllDirectories)
				.Where(path => !ignoredPathRules.Any(
					ignoredPart => path.IndexOf(ignoredPart, StringComparison.InvariantCultureIgnoreCase) > 0))
				.Select(path => path.Replace('\\', '/'))
				.Select(path =>
				{
					Console.WriteLine(path);
					return path;
				})
				.OrderBy(x => x)
				.ToList();

			Console.WriteLine($"Found {i18nFiles.Count} files.");
			Console.WriteLine();
			return i18nFiles;
		}
	}
}
