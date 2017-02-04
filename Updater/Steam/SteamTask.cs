using System;
using System.Threading;
using System.Threading.Tasks;
using SteamKit2;

namespace Updater.Steam
{
    public class SteamTask<T> where T : CallbackMsg
    {
        public TaskState State { get; private set; } = TaskState.Running;
        public T Result { get; private set; }

        private readonly CancellationTokenSource cancellation = new CancellationTokenSource();
        private readonly CallbackManager callbackManager;

        public SteamTask(CallbackManager callbackManager) : this(callbackManager, JobID.Invalid) { }
        public SteamTask(CallbackManager callbackManager, JobID jobId)
        {
            callbackManager.Subscribe<T>(jobId, obj =>
            {
                if (obj.JobID != jobId && jobId != JobID.Invalid)
                {
                    return; // Not our event.
                }

                SetResult(TaskState.OK, obj);
            });

            this.callbackManager = callbackManager;
        } 

        public Task<T> WaitForResult()
        {
            return Task.Run(async () =>
            {
                await Task.Run(() =>
                {
                    TimeSpan delay = TimeSpan.FromSeconds(0.5);

                    while (State == TaskState.Running)
                    {
                        cancellation.Token.ThrowIfCancellationRequested();
                        callbackManager.RunWaitAllCallbacks(delay);
                    }
                });
                
                cancellation.Token.ThrowIfCancellationRequested();
                return Result;
            }, cancellation.Token);
        }

        private void SetResult(TaskState state, T result)
        {
            if (state == TaskState.Running)
                throw new ArgumentException("TaskState.Running is not a valid option when passed to SetResult.", nameof(state));

            Result = result;
            State = state;
        }

        public void Cancel()
        {
            cancellation.Cancel();
        }
    }
}