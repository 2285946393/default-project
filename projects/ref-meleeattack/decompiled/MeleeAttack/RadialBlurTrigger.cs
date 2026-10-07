namespace MeleeAttack;

public static class RadialBlurTrigger
{
	public static void Trigger(float strength = 0.35f, float duration = 0f, string owner = null)
	{
		RadialBlur.Show(strength, duration, owner);
	}

	public static void Stop(string owner = null)
	{
		RadialBlur.Stop(owner);
	}

	public static void FadeOut(float duration = 0.3f)
	{
		RadialBlur.FadeOut(duration);
	}
}
