using System.Threading;
using System.Threading.Tasks;

namespace SorasNerdDen.Services.CancellationTokens
{
    public static class CancellationTokenExtensions
    {
        public static Task WaitAsync(this CancellationToken cancellationToken)
        {
            TaskCompletionSource<bool> cancelationTaskCompletionSource = new TaskCompletionSource<bool>();
            cancellationToken.Register(CancellationTokenCallback, cancelationTaskCompletionSource);

            return cancellationToken.IsCancellationRequested
                ? Task.CompletedTask
                : cancelationTaskCompletionSource.Task;
        }

        private static void CancellationTokenCallback(object taskCompletionSource)
        {
            ((TaskCompletionSource<bool>)taskCompletionSource).SetResult(true);
        }
    }
}
