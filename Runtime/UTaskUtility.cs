using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace THEBADDEST.Tasks
{
	public static class UTaskUtility
	{
		// Extension methods for float
		public static UTask Seconds(this float seconds) => UTask.Delay(seconds);

		public static UTask Ms(this float milliseconds) => UTask.Delay(milliseconds / 1000f);

		public static UTask Minutes(this float minutes) => UTask.Delay(minutes * 60f);

		// Extension methods for IEnumerator
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
							await UTask.Delay(seconds);
							next();
						});
					}
					else if (current is WaitForSecondsRealtime waitRealtime)
					{
						float seconds = waitRealtime.waitTime;
						scheduleAction = next => UTaskScheduler.Schedule(async () =>
						{
							await UTask.DelayRealtime(seconds);
							next();
						});
					}
					else if (current is WaitForFixedUpdate)
					{
						scheduleAction = next => UTaskScheduler.Schedule(async () =>
						{
							await UTask.WaitForFixedUpdate();
							next();
						});
					}
					else if (current is WaitForEndOfFrame)
					{
						scheduleAction = next => UTaskScheduler.Schedule(async () =>
						{
							await UTask.NextFrame();
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
								await UTask.WaitUntil(predicate);
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
								await UTask.WaitWhile(predicate);
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
							await UTask.WaitWhile(() => customYield.keepWaiting);
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

		// Extension methods for YieldInstruction and CustomYieldInstruction
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
			return UTask.WaitWhile(() => yieldInstruction.keepWaiting);
		}

		// Extension method for AsyncOperation
		public static UTask ToUTask(this AsyncOperation operation)
		{
			var source = new UTaskCompletionSource();
			operation.completed += _ => { source.TrySetResult(); };
			return source.Task;
		}

		// Extension method for UnityEngine.Object dependency
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

		// Extension method for timeout
		public static async UTask<bool> WithTimeout(this UTask task, float timeoutSeconds)
		{
			if (!task.IsValid)
				throw new ArgumentException("Task is not valid", nameof(task));

			var timeoutTask = UTask.Delay(timeoutSeconds);
			var completedTask = await UTask.WhenAny(task, timeoutTask);

			if (completedTask == timeoutTask)
				return false;

			await task; // Propagate any exceptions from the original task
			return true;
		}

		// Extension method for retry
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
						await UTask.Delay(delayBetweenAttempts);

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

		// Extension method for timeout and retry
		public static async UTask WithTimeoutAndRetry(this Func<UTask> taskFactory, float timeoutSeconds, int maxAttempts = 3, float delayBetweenAttempts = 1f)
		{
			await WithRetry(async () =>
			{
				var task = taskFactory();
				if (!await task.WithTimeout(timeoutSeconds))
					throw new TimeoutException($"Task timed out after {timeoutSeconds} seconds");
			}, maxAttempts, delayBetweenAttempts);
		}

		// Extension method for running tasks in sequence
		public static async UTask InSequence(this IEnumerable<Func<UTask>> taskFactories)
		{
			if (taskFactories == null)
				return;

			foreach (var factory in taskFactories)
			{
				if (factory == null) continue;
				await factory();
			}
		}

		public static UTask WithCancellation(this UTask task, UTaskCancellationToken cancellationToken)
		{
			if (!task.IsValid)
				throw new ArgumentException("Task is not valid", nameof(task));

			if (!cancellationToken.CanBeCanceled)
				return task;

			if (cancellationToken.IsCancellationRequested)
				return UTask.Break();

			var tcs = new UTaskCompletionSource();
			var registration = cancellationToken.Register(() => tcs.TrySetCanceled());

			UTaskScheduler.Schedule(async () =>
			{
				try
				{
					await task;
					tcs.TrySetResult();
				}
				catch (Exception ex)
				{
					tcs.TrySetException(ex);
				}
				finally
				{
					registration.Dispose();
				}
			});

			return tcs.Task;
		}

		public static UTask<T> WithCancellation<T>(this UTask<T> task, UTaskCancellationToken cancellationToken)
		{
			if (!task.IsValid)
				throw new ArgumentException("Task is not valid", nameof(task));

			if (!cancellationToken.CanBeCanceled)
				return task;

			if (cancellationToken.IsCancellationRequested)
				return UTask<T>.Break();

			var tcs = new UTaskCompletionSource<T>();
			var registration = cancellationToken.Register(() => tcs.TrySetCanceled());

			UTaskScheduler.Schedule(async () =>
			{
				try
				{
					var result = await task;
					tcs.TrySetResult(result);
				}
				catch (Exception ex)
				{
					tcs.TrySetException(ex);
				}
				finally
				{
					registration.Dispose();
				}
			});

			return tcs.Task;
		}

		/// <summary>
		/// Schedules the continuation of this UTask on a background thread using the thread pool.
		/// </summary>
		public static UTask MultiThreaded(this UTask task)
		{
			var tcs = new UTaskCompletionSource();
			UTaskScheduler.Schedule(async () =>
			{
				try
				{
					await task;
					UTaskThreadPoolScheduler.Schedule(() => tcs.TrySetResult());
				}
				catch (OperationCanceledException)
				{
					UTaskThreadPoolScheduler.Schedule(() => tcs.TrySetCanceled());
				}
				catch (Exception ex)
				{
					UTaskThreadPoolScheduler.Schedule(() => tcs.TrySetException(ex));
				}
			});
			return tcs.Task;
		}

		/// <summary>
		/// Schedules the continuation of this UTask<T> on a background thread using the thread pool.
		/// </summary>
		public static UTask<T> MultiThreaded<T>(this UTask<T> task)
		{
			var tcs = new UTaskCompletionSource<T>();
			UTaskScheduler.Schedule(async () =>
			{
				try
				{
					var result = await task;
					UTaskThreadPoolScheduler.Schedule(() => tcs.TrySetResult(result));
				}
				catch (OperationCanceledException)
				{
					UTaskThreadPoolScheduler.Schedule(() => tcs.TrySetCanceled());
				}
				catch (Exception ex)
				{
					UTaskThreadPoolScheduler.Schedule(() => tcs.TrySetException(ex));
				}
			});
			return tcs.Task;
		}

		/// <summary>
		/// Adds a continuation to a UTask, running the given action after the task completes.
		/// </summary>
		public static UTask ContinueWith(this UTask task, Action continuation)
		{
			var tcs = new UTaskCompletionSource();
			UTaskScheduler.Schedule(async () =>
			{
				try
				{
					await task;
					continuation?.Invoke();
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
	}
}