using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace THEBADDEST.Tasks
{


	public static class UTaskX
	{

		// Short names for common operations
		public static UTask Wait(float seconds) => Delay(seconds);

		public static UTask Next() => NextFrame();

		public static UTask Fixed() => WaitForFixedUpdate();

		public static UTask Until(Func<bool> predicate) => WaitUntil(predicate);

		public static UTask While(Func<bool> predicate) => WaitWhile(predicate);

		public static UTask All(params UTask[] tasks) => WhenAll(tasks);

		// Shorter names for common time intervals
		public static UTask Seconds(this float seconds) => Delay(seconds);

		public static UTask Ms(this float milliseconds) => Delay(milliseconds / 1000f);

		public static UTask Minutes(this float minutes) => Delay(minutes * 60f);

		public static UTask Delay(float seconds)
		{
			if (seconds <= 0)
			{
				var immediateSource = new UTaskCompletionSource();
				immediateSource.TrySetResult();
				return immediateSource.Task;
			}

			var    source          = new UTaskCompletionSource();
			var    targetTime      = Time.time + seconds;
			Action checkTimeAction = null;
			checkTimeAction = () =>
			{
				if (Time.time >= targetTime)
				{
					source.TrySetResult();
					UTaskScheduler.RemovePerFrame(checkTimeAction);
				}
			};
			UTaskScheduler.SchedulePerFrame(checkTimeAction);
			return source.Task;
		}

		public static async UTask WhenAll(params UTask[] tasks)
		{
			var remaining = tasks.Length;
			var tcs       = new UTaskCompletionSource();
			foreach (var task in tasks)
			{
				RunTask(task);
			}

			async void RunTask(UTask task)
			{
				try
				{
					await task;
					if (--remaining == 0)
					{
						tcs.TrySetResult();
					}
				}
				catch (Exception ex)
				{
					Debug.LogError($"Task failed: {ex}");
					tcs.TrySetException(ex);
				}
			}

			await tcs.Task;
		}

		public static UTask NextFrame()
		{
			var source = new UTaskCompletionSource();
			UTaskScheduler.Schedule(() => { source.TrySetResult(); });
			return source.Task;
		}

		public static UTask ToUTask(this AsyncOperation operation)
		{
			var source = new UTaskCompletionSource();
			operation.completed += _ => { source.TrySetResult(); };
			return source.Task;
		}

		public static UTask WaitUntil(Func<bool> predicate)
		{
			var    source         = new UTaskCompletionSource();
			Action checkCondition = null;
			checkCondition = () =>
			{
				if (predicate())
				{
					source.TrySetResult();
					UTaskScheduler.RemovePerFrame(checkCondition);
				}
			};
			UTaskScheduler.SchedulePerFrame(checkCondition);
			return source.Task;
		}

		public static UTask WaitWhile(Func<bool> predicate)
		{
			return WaitUntil(() => !predicate());
		}

		public static UTask WaitForFixedUpdate()
		{
			var    source           = new UTaskCompletionSource();
			bool   isFirstFrame     = true;
			Action checkFixedUpdate = null;
			checkFixedUpdate = () =>
			{
				if (isFirstFrame)
				{
					isFirstFrame = false;
					return;
				}

				source.TrySetResult();
				UTaskScheduler.RemovePerFrame(checkFixedUpdate);
			};
			UTaskScheduler.SchedulePerFrame(checkFixedUpdate);
			return source.Task;
		}

		public static UTask ToUTask(this IEnumerator enumerator)
		{
			var source = new UTaskCompletionSource();

			void HandleCoroutine()
			{
				try
				{
					if (!enumerator.MoveNext())
					{
						source.TrySetResult();
						return;
					}

					var            current        = enumerator.Current;
					Action<Action> scheduleAction = next => UTaskScheduler.Schedule(next);
					if (current is WaitForSeconds waitForSeconds)
					{
						float seconds = waitForSeconds.GetFieldValue<float>("m_Seconds", 0f);
						scheduleAction = next => UTaskScheduler.Schedule(async () =>
						{
							await Delay(seconds);
							next();
						});
					}
					else if (current is WaitForSecondsRealtime waitRealtime)
					{
						float seconds = waitRealtime.waitTime;
						scheduleAction = next => UTaskScheduler.Schedule(async () =>
						{
							await Delay(seconds);
							next();
						});
					}
					else if (current is WaitForFixedUpdate)
					{
						scheduleAction = next => UTaskScheduler.Schedule(async () =>
						{
							await WaitForFixedUpdate();
							next();
						});
					}
					else if (current is WaitForEndOfFrame)
					{
						scheduleAction = next => UTaskScheduler.Schedule(async () =>
						{
							await NextFrame();
							next();
						});
					}
					else if (current is WaitUntil waitUntil)
					{
						var predicate = waitUntil.GetFieldValue<Func<bool>>("m_Predicate", null);
						scheduleAction = next => UTaskScheduler.Schedule(async () =>
						{
							await WaitUntil(predicate);
							next();
						});
					}
					else if (current is WaitWhile waitWhile)
					{
						var predicate = waitWhile.GetFieldValue<Func<bool>>("m_Predicate", null);
						scheduleAction = next => UTaskScheduler.Schedule(async () =>
						{
							await WaitWhile(predicate);
							next();
						});
					}
					else if (current is AsyncOperation asyncOp)
					{
						scheduleAction = next => UTaskScheduler.Schedule(async () =>
						{
							await asyncOp.ToUTask();
							next();
						});
					}
					else if (current is CustomYieldInstruction customYield)
					{
						scheduleAction = next => UTaskScheduler.Schedule(async () =>
						{
							await WaitWhile(() => customYield.keepWaiting);
							next();
						});
					}

					scheduleAction(HandleCoroutine);
				}
				catch (Exception ex)
				{
					source.TrySetException(ex);
				}
			}

			UTaskScheduler.Schedule(HandleCoroutine);
			return source.Task;
		}

		private static T GetFieldValue<T>(this object obj, string fieldName, T defaultValue)
		{
			var field = obj.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			return field != null ? (T)field.GetValue(obj) : defaultValue;
		}

		public static UTask ToUTask(this YieldInstruction yieldInstruction)
		{
			IEnumerator WrapYieldInstruction()
			{
				yield return yieldInstruction;
			}

			return WrapYieldInstruction().ToUTask();
		}

		public static UTask ToUTask(this CustomYieldInstruction yieldInstruction)
		{
			return WaitWhile(() => yieldInstruction.keepWaiting);
		}

	}


}