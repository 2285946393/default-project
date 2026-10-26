using System.Text;
using MegaCrit.Sts2.Core.Entities.Powers;
using STS2RitsuLib.Scaffolding.Content;

namespace RestSiteExpansion.Powers;

public abstract class TemplatePower : ModPowerTemplate
{
	private string IconName
	{
		get
		{
			string text = ((object)this).GetType().Name;
			if (text.EndsWith("Power"))
			{
				string text2 = text;
				text = text2.Substring(0, text2.Length - 5);
			}
			StringBuilder stringBuilder = new StringBuilder();
			for (int i = 0; i < text.Length; i++)
			{
				if (i > 0 && char.IsUpper(text[i]))
				{
					stringBuilder.Append('_');
				}
				stringBuilder.Append(char.ToLowerInvariant(text[i]));
			}
			return stringBuilder.ToString();
		}
	}

	public override PowerAssetProfile AssetProfile => new PowerAssetProfile(IconResPath(IconName + ".png"), IconResPath(IconName + ".png"));

	public abstract override PowerType Type { get; }

	public abstract override PowerStackType StackType { get; }

	private static string IconResPath(string fileName)
	{
		return "res://art/images/powers/" + fileName;
	}
}
