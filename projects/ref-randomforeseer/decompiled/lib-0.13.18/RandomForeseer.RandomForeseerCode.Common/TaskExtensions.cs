using System;
using System.Threading.Tasks;

namespace RandomForeseer.RandomForeseerCode.Common;

internal static class TaskExtensions
{
	public static async Task<T> WithFinally<T>(this Task<T> task, Action onFinally)
	{
		try
		{
			return await task;
		}
		finally
		{
			onFinally();
		}
	}

	public static async Task WithFinally(this Task task, Action onFinally)
	{
		try
		{
			await task;
		}
		finally
		{
			onFinally();
		}
	}
}
