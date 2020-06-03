namespace TranslationChecker
{
	internal interface IAnalyzer
	{
		void Analyze(TranslationFile file, ErrorCollector errorCollector);
	}
}