using System;
using Mindbox.Quokka;

namespace TranslationChecker
{
	internal class FormsTemplateFunction : ScalarTemplateFunction<decimal, string, string, string, string>
	{
		public FormsTemplateFunction()
			: base(
				"forms",
				new DecimalFunctionArgument("quantity"),
				new StringFunctionArgument("singularForm"),
				new StringFunctionArgument("dualForm"),
				new StringFunctionArgument("pluralForm"))
		{
		}

		public override string Invoke(decimal quantity, string singularForm, string dualForm, string pluralForm)
		{
			var intQuantity = Convert.ToInt32(quantity);
			return GetQuantityForm(intQuantity, singularForm, dualForm, pluralForm);
		}

		private string GetQuantityForm(
			int quantity, 
			string singularForm, 
			string dualForm, 
			string pluralForm)
		{
			var last2Digits =
				quantity > 0 ? quantity % 100 :
				quantity == int.MinValue ? Math.Abs(int.MinValue + 100) % 100 :
				Math.Abs(quantity) % 100;
			var lastDigit = last2Digits % 10;
			var lastButOneDigit = last2Digits / 10;
			var lastDigitIs2Or3Or4 = lastDigit == 2 || lastDigit == 3 || lastDigit == 4;

			return
				lastButOneDigit == 1 ? pluralForm :
				lastDigit == 1 ? singularForm :
				lastDigitIs2Or3Or4 ? dualForm :
				pluralForm;
		}
	}
}