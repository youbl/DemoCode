using Beinet.Core;
using Beinet.Core.FileExt;

namespace DemoCodeConsole.CoreTest
{
    class FileHelperTest : IRunable
    {
        public void Run()
        {
            var source = @"D:\work\source\m2-rpa-scheduler-worker-csharp\ZixunRpaWorker\bin\Debug\upgrade";
            var target = @"D:\work\source\m2-rpa-scheduler-worker-csharp\ZixunRpaWorker\bin\Debug";
            FileHelper.MoveDir(source, target);
        }
    }
}