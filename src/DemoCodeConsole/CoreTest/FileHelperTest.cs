using System;
using System.IO;
using Beinet.Core;
using Beinet.Core.FileExt;
using Beinet.Core.Serializer;

namespace DemoCodeConsole.CoreTest
{
    class FileHelperTest : IRunable
    {
        public void Run()
        {
            var sourceContent = File.ReadAllText(@"D:\show-busy-java-threads");
            var file = @"D:\abc.txt";
            JsonSerializer.DEFAULT.SerializToFile(file, sourceContent);
            var targetContent = JsonSerializer.DEFAULT.DeSerializFromFile<string>(file);
            // FileHelper.Save(file, sourceContent);
            //var targetContent = File.ReadAllText(file);
            Console.WriteLine(sourceContent);
            Console.WriteLine("==============================================");
            Console.WriteLine(targetContent);
            Console.WriteLine("==============================================");
            Console.WriteLine(sourceContent == targetContent);

            // var source = @"D:\work\source\m2-rpa-scheduler-worker-csharp\ZixunRpaWorker\bin\Debug\upgrade";
            // var target = @"D:\work\source\m2-rpa-scheduler-worker-csharp\ZixunRpaWorker\bin\Debug";
            // FileHelper.MoveDir(source, target);
        }
    }
}