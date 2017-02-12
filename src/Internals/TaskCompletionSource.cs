using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace System.Collections.Async.Internals
{
    /// <summary>
    /// Utility methods for <see cref="TaskCompletionSource{TResult}"/>
    /// </summary>
    public static class TaskCompletionSource
    {
        private static Func<Task, object, int> _resetTaskFunc;

        static TaskCompletionSource()
        {
            // Collect all necessary fields of a Task that needs to be reset.
#if NETSTANDARD
            var m_stateFlags = typeof(Task).GetTypeInfo().GetDeclaredField("m_stateFlags");
            var m_continuationObject = typeof(Task).GetTypeInfo().GetDeclaredField("m_continuationObject");
            var m_taskId = typeof(Task).GetTypeInfo().GetDeclaredField("m_taskId");
            var m_stateObject = typeof(Task).GetTypeInfo().GetDeclaredField("m_stateObject");
#else
            var m_stateFlags = typeof(Task).GetField("m_stateFlags", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var m_continuationObject = typeof(Task).GetField("m_continuationObject", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var m_taskId = typeof(Task).GetField("m_taskId", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var m_stateObject = typeof(Task).GetField("m_stateObject", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
#endif

            // Make sure that all of them available (has been checked with .NET Framework 4.5 only).
            if (m_stateFlags != null && m_continuationObject != null && m_taskId != null && m_stateObject != null)
                try
                {
                    /* Using Linq Expressions compile a simple function:
                     * 
                     * int ResetTask(Task task, object state)
                     * {
                     *   task.m_stateFlags = <default int32 value>;
                     *   task.m_continuationObject = null;
                     *   task.m_taskId = 0;
                     *   m_stateObject = state;
                     *   return 0;
                     * }
                     */

                    var defaultStateFlags = (int)m_stateFlags.GetValue(new TaskCompletionSource<int>().Task);

                    var targetArg = Expression.Parameter(typeof(Task), "task");
                    var stateObjectArg = Expression.Parameter(typeof(object), "stateObject");

                    var body = Expression.Block(
                        Expression.Assign(Expression.MakeMemberAccess(targetArg, m_stateFlags), Expression.Constant(defaultStateFlags, typeof(int))),
                        Expression.Assign(Expression.MakeMemberAccess(targetArg, m_continuationObject), Expression.Constant(null, typeof(object))),
                        Expression.Assign(Expression.MakeMemberAccess(targetArg, m_taskId), Expression.Constant(0, typeof(int))),
                        Expression.Assign(Expression.MakeMemberAccess(targetArg, m_stateObject), stateObjectArg),
                        Expression.Constant(0, typeof(int)) // this can be anything of any type - lambda expression allows to compile Func<> only, but not an Action<>
                    );

                    var lambda = Expression.Lambda(body, targetArg, stateObjectArg);
                    _resetTaskFunc = (Func<Task, object, int>)lambda.Compile();

                    // Do initial testing of the reset function
                    TestResetFunction();
                }
                catch
                {
                    // If something goes wrong, the feature just won't be enabled.
                    _resetTaskFunc = null;
                }
        }

        private static void TestResetFunction()
        {
            var stateObject1 = new object();
            var stateObject2 = new object();
            var tcs = new TaskCompletionSource<int>();

            // Test reset before SetResult
            _resetTaskFunc(tcs.Task, stateObject1);
            if (tcs.Task.IsCanceled || tcs.Task.IsCompleted || tcs.Task.IsFaulted || tcs.Task.AsyncState != stateObject1)
            {
                _resetTaskFunc = null;
                return;
            }

            // Test SetResult
            tcs.SetResult(123);
            if (tcs.Task.IsCanceled || !tcs.Task.IsCompleted || tcs.Task.IsFaulted)
            {
                _resetTaskFunc = null;
                return;
            }

            // Test reset before SetCanceled
            _resetTaskFunc(tcs.Task, stateObject2);
            if (tcs.Task.IsCanceled || tcs.Task.IsCompleted || tcs.Task.IsFaulted || tcs.Task.AsyncState != stateObject2)
            {
                _resetTaskFunc = null;
                return;
            }

            // Test SetCanceled
            tcs.SetCanceled();
            if (!tcs.Task.IsCanceled || !tcs.Task.IsCompleted || tcs.Task.IsFaulted)
            {
                _resetTaskFunc = null;
                return;
            }

            // Test reset before SetException
            _resetTaskFunc(tcs.Task, stateObject1);
            if (tcs.Task.IsCanceled || tcs.Task.IsCompleted || tcs.Task.IsFaulted || tcs.Task.AsyncState != stateObject1)
            {
                _resetTaskFunc = null;
                return;
            }

            // Test SetException
            var ex = new Exception();
            tcs.SetException(ex);
            if (tcs.Task.IsCanceled || !tcs.Task.IsCompleted || !tcs.Task.IsFaulted || tcs.Task.Exception.InnerException != ex)
            {
                _resetTaskFunc = null;
                return;
            }
        }

        /// <summary>
        /// Forcibly disables re-use of <see cref="TaskCompletionSource{TResult}"/> instances in the <see cref="Reset{T}(ref TaskCompletionSource{T}, object)"/> method.
        /// This is just a safety switch in case when something goes wrong with re-using instances of <see cref="TaskCompletionSource{TResult}"/>.
        /// </summary>
        public static void DisableTaskCompletionSourceReUse()
        {
            _resetTaskFunc = null;
        }

        /// <summary>
        /// Resets a <see cref="TaskCompletionSource{TResult}"/> to initial incomplete state.
        /// This method by default re-uses the same instance of the <see cref="TaskCompletionSource{TResult}"/> by re-setting internal state of its <see cref="Task"/> using reflection.
        /// If such feature is not available or explicitly disable with the <see cref="DisableTaskCompletionSourceReUse"/> method, it just returns a new instance of a <see cref="TaskCompletionSource{TResult}"/>.
        /// </summary>
        /// <typeparam name="T">Type of the result value</typeparam>
        /// <param name="taskCompletionSource">Target <see cref="TaskCompletionSource{TResult}"/> to be reset or recreated. It's safe to pass null.</param>
        /// <param name="stateObject">Optional state object that you pass into <see cref="TaskCompletionSource{TResult}"/> constructor.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Reset<T>(ref TaskCompletionSource<T> taskCompletionSource, object stateObject = null)
        {
            if (_resetTaskFunc != null && taskCompletionSource != null)
            {
                _resetTaskFunc(taskCompletionSource.Task, stateObject);
            }
            else
            {
                taskCompletionSource = new TaskCompletionSource<T>();
            }
        }
    }
}