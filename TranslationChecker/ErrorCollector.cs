using System.Collections.Generic;

namespace TranslationChecker
{
	public class ErrorCollector
	{
		public List<string> Errors { get; } = new List<string>();

		public void AddError(string errorMessage)
		{
			Errors.Add(errorMessage);
		}
	}
}