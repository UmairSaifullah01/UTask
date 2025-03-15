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
			if (float.IsNaN(seconds))
				throw new ArgumentException("Delay duration cannot be NaN", nameof(seconds));

			if (seconds <= 0)
			{
				var immediateSource = new UTaskCompletionSource();
				immediateSource.TrySetResult();
				return immediateSource.Task;
			}

			var source = new UTaskCompletionSource();
			var targetTime = Time.time + Mathf.Max(0, seconds);
			void CheckTimeAction()
			{
				if (Time.time >= targetTime)
				{
					source.TrySetResult();
					UTaskScheduler.RemovePerFrame(CheckTimeAction);
				}
			}

			UTaskScheduler.SchedulePerFrame(CheckTimeAction);
			return source.Task;
		}

		public static UTask DelayRealtime(float seconds)
		{
			if (float.IsNaN(seconds))
				throw new ArgumentException("Delay duration cannot be NaN", nameof(seconds));

			if (seconds <= 0)
			{
				var immediateSource = new UTaskCompletionSource();
				immediateSource.TrySetResult();
				return immediateSource.Task;
			}

			var source = new UTaskCompletionSource();
			var targetTime = Time.realtimeSinceStartup + Mathf.Max(0, seconds);
			void CheckTimeAction()
			{
				if (Time.realtimeSinceStartup >= targetTime)
				{
					source.TrySetResult();
					UTaskScheduler.RemovePerFrame(CheckTimeAction);
				}
			}
			UTaskScheduler.SchedulePerFrame(CheckTimeAction);
			return source.Task;
		}

		public static async UTask WhenAll(params UTask[] tasks)
		{
			if (tasks == null || tasks.Length == 0)
				return;

			// Count valid tasks
			int validTaskCount = 0;
			foreach (var task in tasks)
			{
				if (task.IsValid) validTaskCount++;
			}

			if (validTaskCount == 0)
				return;

			var remaining = validTaskCount;
			var tcs = new UTaskCompletionSource();
			var exceptions = new List<Exception>();
			var anyCanceled = false;

			foreach (var task in tasks)
			{
				if (!task.IsValid) continue;  // Skip invalid tasks
				RunTask(task);
			}

			async void RunTask(UTask task)
			{
				try
				{
					await task;
				}
				catch (OperationCanceledException)
				{
					anyCanceled = true;
				}
				catch (Exception ex)
				{
					lock (exceptions)
					{
						exceptions.Add(ex);
					}
				}
				finally
				{
					if (--remaining == 0)
					{
						if (exceptions.Count > 0)
							tcs.TrySetException(exceptions.Count == 1 ? exceptions[0] : new AggregateException(exceptions));
						else if (anyCanceled)
							tcs.TrySetCanceled();
						else
							tcs.TrySetResult();
					}
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
			var source = new UTaskCompletionSource();
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
			var source = new UTaskCompletionSource();
			bool isFirstFrame = true;
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
			if (enumerator == null)
				throw new ArgumentNullException(nameof(enumerator));

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

					var current = enumerator.Current;

					// Handle null yield return
					if (current == null)
					{
						UTaskScheduler.Schedule(HandleCoroutine);
						return;
					}

					Action<Action> scheduleAction = next => UTaskScheduler.Schedule(next);

					if (current is IEnumerator nestedCoroutine)
					{
						scheduleAction = next => UTaskScheduler.Schedule(async () =>
						{
							await nestedCoroutine.ToUTask();
							next();
						});
					}
					else if (current is WaitForSeconds waitForSeconds)
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
							await DelayRealtime(seconds);
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
						if (predicate != null)
						{
							scheduleAction = next => UTaskScheduler.Schedule(async () =>
							{
								await WaitUntil(predicate);
								next();
							});
						}
					}
					else if (current is WaitWhile waitWhile)
					{
						var predicate = waitWhile.GetFieldValue<Func<bool>>("m_Predicate", null);
						if (predicate != null)
						{
							scheduleAction = next => UTaskScheduler.Schedule(async () =>
							{
								await WaitWhile(predicate);
								next();
							});
						}
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

		/// <summary>
		/// Makes the task dependent on an UnityEngine.Object, canceling if the object is destroyed
		/// </summary>
		public static UTask ToDepend(this UTask task, UnityEngine.Object dependency)
		{
			if (dependency == null)
				throw new ArgumentNullException(nameof(dependency), "Dependency object cannot be null");

			var tcs = new UTaskCompletionSource();

			UTaskScheduler.Schedule(async () =>
			{
				try
				{
					await task;
					if (dependency == null)
					{
						tcs.TrySetCanceled();
						return;
					}
					tcs.TrySetResult();
				}
				catch (OperationCanceledException)
				{
					tcs.TrySetCanceled();
				}
				catch (Exception ex)
				{
					tcs.TrySetException(ex);
				}
			});

			return tcs.Task;
		}

		/// <summary>
		/// Makes the task dependent on an UnityEngine.Object, canceling if the object is destroyed
		/// </summary>
		public static UTask<T> ToDepend<T>(this UTask<T> task, UnityEngine.Object dependency)
		{
			if (dependency == null)
				throw new ArgumentNullException(nameof(dependency), "Dependency object cannot be null");

			var tcs = new UTaskCompletionSource<T>();

			UTaskScheduler.Schedule(async () =>
			{
				try
				{
					var result = await task;
					if (dependency == null)
					{
						tcs.TrySetCanceled();
						return;
					}
					tcs.TrySetResult(result);
				}
				catch (OperationCanceledException)
				{
					tcs.TrySetCanceled();
				}
				catch (Exception ex)
				{
					tcs.TrySetException(ex);
				}
			});

			return tcs.Task;
		}

		/// <summary>
		/// Creates a task that completes after a timeout or when the original task completes
		/// </summary>
		public static async UTask<bool> WithTimeout(this UTask task, float timeoutSeconds)
		{
			if (!task.IsValid)
				throw new ArgumentException("Task is not valid", nameof(task));

			var timeoutTask = Delay(timeoutSeconds);
			var completedTask = await WhenAny(task, timeoutTask);

			if (completedTask == timeoutTask)
				return false;

			await task; // Propagate any exceptions from the original task
			return true;
		}

		/// <summary>
		/// Creates a task that completes when any of the tasks complete
		/// </summary>
		public static async UTask<UTask> WhenAny(params UTask[] tasks)
		{
			if (tasks == null || tasks.Length == 0)
				throw new ArgumentException("At least one task is required", nameof(tasks));

			var tcs = new UTaskCompletionSource<UTask>();

			foreach (var task in tasks)
			{
				if (!task.IsValid) continue;

				RunTask(task);
			}

			async void RunTask(UTask task)
			{
				try
				{
					await task;
					tcs.TrySetResult(task);
				}
				catch (Exception)
				{
					// Ignore exceptions, they will be thrown when awaiting the task
				}
			}

			return await tcs.Task;
		}

		/// <summary>
		/// Retries a task if it fails
		/// </summary>
		public static async UTask WithRetry(this Func<UTask> taskFactory, int maxAttempts = 3, float delayBetweenAttempts = 1f)
		{
			if (taskFactory == null)
				throw new ArgumentNullException(nameof(taskFactory));

			Exception lastException = null;

			for (int attempt = 0; attempt < maxAttempts; attempt++)
			{
				try
				{
					if (attempt > 0)
						await Delay(delayBetweenAttempts);

					await taskFactory();
					return;
				}
				catch (Exception ex)
				{
					lastException = ex;
				}
			}

			throw new AggregateException($"Task failed after {maxAttempts} attempts", lastException);
		}

		/// <summary>
		/// Runs a task with a timeout and optional retry
		/// </summary>
		public static async UTask WithTimeoutAndRetry(this Func<UTask> taskFactory, float timeoutSeconds, int maxAttempts = 3, float delayBetweenAttempts = 1f)
		{
			await WithRetry(async () =>
			{
				var task = taskFactory();
				if (!await task.WithTimeout(timeoutSeconds))
					throw new TimeoutException($"Task timed out after {timeoutSeconds} seconds");
			}, maxAttempts, delayBetweenAttempts);
		}

		/// <summary>
		/// Runs multiple tasks in sequence
		/// </summary>
		public static async UTask InSequence(params Func<UTask>[] taskFactories)
		{
			if (taskFactories == null || taskFactories.Length == 0)
				return;

			foreach (var factory in taskFactories)
			{
				if (factory == null) continue;
				await factory();
			}
		}

		/// <summary>
		/// Creates a task that can be manually completed from outside
		/// </summary>
		public static UTaskCompletionSource CreateManualTask()
		{
			return new UTaskCompletionSource();
		}

		/// <summary>
		/// Creates a task that can be manually completed from outside with a result
		/// </summary>
		public static UTaskCompletionSource<T> CreateManualTask<T>()
		{
			return new UTaskCompletionSource<T>();
		}

	}


}