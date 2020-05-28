using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Mindbox.Quokka;

namespace TranslationChecker
{
	internal class QuokkaTemplateAnalyzer
	{
		private readonly Func<string, bool> templateChecker;

		public QuokkaTemplateAnalyzer()
		{
			var templateFactory = new DefaultTemplateFactory(new[] {new FormsTemplateFunction()});

			templateChecker = template => templateFactory.TryCreateTemplate(template, out var errors) != null;
		}

		public bool Analyze(string template)
		{
			if (string.IsNullOrWhiteSpace(template))
				return true;

			try
			{
				return templateChecker(template);
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.InnerException?.Message ?? ex.Message);
				throw;
			}
		}
	}
}
