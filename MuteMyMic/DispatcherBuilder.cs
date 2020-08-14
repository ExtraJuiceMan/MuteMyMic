using System.Threading;
using System.Windows.Threading;

namespace MuteMyMic
{
    // Thank you feO2x https://stackoverflow.com/a/25504449
    public static class DispatcherBuilder
    {
        public static Dispatcher Build()
        {
            Dispatcher dispatcher = null;
            var manualResetEvent = new ManualResetEvent(false);
            var thread = new Thread(() =>
            {
                dispatcher = Dispatcher.CurrentDispatcher;
                var synchronizationContext = new DispatcherSynchronizationContext(dispatcher);
                SynchronizationContext.SetSynchronizationContext(synchronizationContext);

                manualResetEvent.Set();
                Dispatcher.Run();
            });
            thread.Start();
            manualResetEvent.WaitOne();
            manualResetEvent.Dispose();
            return dispatcher;
        }
    }
}
