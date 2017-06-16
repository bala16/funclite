using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FuncLite.Helpers
{
    public class OperationManager
    {
        private const int DefaultRetries = 3;
        private const int DefaultDelayBeforeRetry = 250; // 250 ms

        public static Task AttemptAsync(Func<Task> action, int retries = DefaultRetries, int delayBeforeRetry = DefaultDelayBeforeRetry)
        {
            return AttemptAsync(async () =>
            {
                await action();
                return true;
            }, retries, delayBeforeRetry);
        }

        public static async Task<TVal> AttemptAsync<TVal>(Func<Task<TVal>> action, int retries = DefaultRetries, int delayBeforeRetry = DefaultDelayBeforeRetry)
        {
            while (retries > 0)
            {
                try
                {
                    return await action();
                }
                catch
                {
                    retries--;
                    if (retries == 0)
                    {
                        throw;
                    }
                }

                await Task.Delay(delayBeforeRetry);
            }

            return default(TVal);
        }
    }
}
