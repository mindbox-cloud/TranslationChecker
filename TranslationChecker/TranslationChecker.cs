using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;

namespace TranslationChecker
{
	partial class TranslationChecker
	{
		static int Main(string[] args)
		{
			var launchParameters = LaunchParameters.FromCommandLineArguments(args);
			if (launchParameters == null)
				return -1;

			Console.WriteLine($"Working directory: {launchParameters.BaseDirectory}");

			var baseDirectory = launchParameters.BaseDirectory;
			var skipProjectInclusionCheck = launchParameters.SkipProjectInclusionCheck;
			
			var errorsOccured = 
				TranslationErrorsFound(baseDirectory, skipProjectInclusionCheck) | NewCyrillicLinesFound(baseDirectory);
			
			if (errorsOccured)
			{
				Console.WriteLine("Error: there are problems with translations.");
				Console.WriteLine("Inspect the log messages above.");
				return -1;
			}

			Console.WriteLine("Success: no problems found with translations.");
			return 0;
		}

		private static bool TranslationErrorsFound(string baseDirectory, bool skipProjectInclusionCheck)
		{
			var analyzers = new List<IAnalyzer>
			{
				new FileContentAnalyzer()
			};

			if (!skipProjectInclusionCheck)
				analyzers.Add(new ProjectInclusionAnalyzer(baseDirectory));

			analyzers.Add(new NamespaceUniquenessAnalyzer());
			
			Console.WriteLine("Searching for translation files...");

			var i18nFiles = LocateInternationalizationFiles(baseDirectory);

			var foundError = false;
			
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
					LogError($"{Path.GetRelativePath(baseDirectory, translationFilePath)}:");
					foreach (var error in errorCollector.Errors)
						LogError(error);

					foundError = true;
				}
			}

			return foundError;
		}
		
		private static bool NewCyrillicLinesFound(string baseDirectory)
		{
			var exceptionsFilePath = Path.Join(baseDirectory, "/build/cyrillic-lines-exceptions.json");

			if (!File.Exists(exceptionsFilePath))
				return true;
						
			var exceptions = JsonConvert.DeserializeObject<Dictionary<string, int>>(File.ReadAllText(exceptionsFilePath));
			var cyrillicLinesPerFile = CyrillicCounter.CountLinesWithCyrillicInFolder(baseDirectory);

			var filesWithNewCyrillicLines = exceptions.Keys.Concat(cyrillicLinesPerFile.Keys)
				.Distinct()
				.Select(key => new
				{
					RelativePath = key,
					InExceptions = exceptions.ContainsKey(key) ? exceptions[key] : 0,
					InFile = cyrillicLinesPerFile.ContainsKey(key) ? cyrillicLinesPerFile[key] : 0
				})
				.Select(dto => new
				{
					dto.RelativePath,
					Delta = dto.InFile - dto.InExceptions
				})
				.Where(dto => dto.Delta > 0)
				.ToArray();

			if (!filesWithNewCyrillicLines.Any())
				return false;
			
			foreach (var file in filesWithNewCyrillicLines)
			{
				LogError($"File {file.RelativePath} has {file.Delta} more lines with cyrillic symbols.");
			}

			return true;
		}

		private class LaunchParameters
		{
			public static LaunchParameters? FromCommandLineArguments(string[] args)
			{
				if (args.Length == 0)
				{
					Console.WriteLine("Required arguments <SolutionFolderPath> [--skipInclusionCheck]");
					return null;
				}

				return new LaunchParameters
				(
					args[0],
					args.Any(arg => arg == "--skipInclusionCheck")
				);
			}

			private LaunchParameters(string baseDirectory, bool skipProjectInclusionCheck)
			{
				BaseDirectory = baseDirectory;
				SkipProjectInclusionCheck = skipProjectInclusionCheck;
				// empty
			}

			public string BaseDirectory { get; }

			public bool SkipProjectInclusionCheck { get; }
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
