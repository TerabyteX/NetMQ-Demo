//#define GenerateUsers
#define PerformanceTest
//#define MailTest

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

using NetMQ;
using NetMQ.Sockets;

namespace NetMQConsole
{
    class Program
    {
        static void Main(string[] args)
        {
#if PerformanceTest
            RunTests(
                new LatencyTest(),
                new ThroughputTest());

            Console.WriteLine("完成性能测试.");
#endif

#if MailTest
            RunTests(new MailTest());
#endif

#if GenerateUsers
            Console.WriteLine("生成测试数据进行中...");
            UserService.GenerateUsers();
            Console.WriteLine("生成测试数据完成");
#endif
            Console.ReadLine();
        }

        static void RunTests(params ITest[] tests)
        {
            foreach (ITest test in tests)
            {
                Console.WriteLine("运行测试 {0}...", test.TestName);
                Console.WriteLine();
                test.RunTest();
                Console.WriteLine();
            }
        }
    }

    interface ITest
    {
        string TestName { get; }

        void RunTest();
    }

    #region 性能测试
    class ThroughputTest : ITest
    {
        private static readonly int[] MessageSizes = { 8, 64, 256, 1024, 4096, 8192, 10240, 16384 };

        private const int MsgCount = 1000000;

        public string TestName
        {
            get { return "吞吐量测试"; }
        }

        public void RunTest()
        {
            var task1 = Task.Factory.StartNew(() =>
            {
                ProxyPull();
            });

            var task2 = Task.Factory.StartNew(() =>
            {
                ProxyPush();
            });

            Task.WaitAll(task1, task2);
        }

        private static void ProxyPull()
        {
            using (var context = NetMQContext.Create())
            using (var socket = context.CreatePullSocket())
            {
                socket.Bind("tcp://*:9099");

                foreach (int messageSize in MessageSizes)
                {
                    var message = new byte[messageSize];

                    message = socket.Receive();
                    Debug.Assert(message[messageSize / 2] == 0x42, "消息不包含验证数据.");

                    var watch = new Stopwatch();
                    watch.Start();

                    for (int i = 1; i < MsgCount; i++)
                    {
                        message = socket.Receive();
                        Debug.Assert(message[messageSize / 2] == 0x42, "消息不包含验证数据.");
                    }

                    watch.Stop();

                    long elapsedTime = watch.ElapsedTicks;
                    long messageThroughput = MsgCount * Stopwatch.Frequency / elapsedTime;
                    long byteThroughput = messageThroughput * messageSize;
                    long megabitThroughput = byteThroughput * 8 / 1000000;

                    Console.WriteLine("消息大小: {0} 字节", messageSize);
                    Console.WriteLine("平均消息吞吐量: {0} 消息/秒", messageThroughput);
                    Console.WriteLine("平均字节吞吐量: {0} 兆字节/秒", byteThroughput / 1000000);
                    Console.WriteLine("平均比特吞吐量: {0} 兆比特/秒", megabitThroughput);
                }
            }
        }

        private static void ProxyPush()
        {
            using (var context = NetMQContext.Create())
            using (var socket = context.CreatePushSocket())
            {
                socket.Connect("tcp://127.0.0.1:9099");

                foreach (int messageSize in MessageSizes)
                {
                    var msg = new byte[messageSize];
                    msg[messageSize / 2] = 0x42;

                    for (int i = 0; i < MsgCount; i++)
                    {
                        socket.Send(msg);
                    }
                }
            }
        }
    }

    class LatencyTest : ITest
    {
        private const int RoundTripCount = 10000;

        private static readonly int[] MessageSizes = { 8, 64, 512, 4096, 8192, 16384, 32768 };

        public string TestName
        {
            get { return "时延测试"; }
        }

        public void RunTest()
        {
            var task1 = Task.Factory.StartNew(() =>
            {
                Client();
            });

            var task2 = Task.Factory.StartNew(() =>
            {
                Server();
            });

            Task.WaitAll(task1, task2);
        }

        private static void Client()
        {
            using (var context = NetMQContext.Create())
            using (var socket = context.CreateRequestSocket())
            {
                socket.Connect("tcp://127.0.0.1:9999");

                foreach (int messageSize in MessageSizes)
                {
                    var msg = new byte[messageSize];
                    var reply = new byte[messageSize];

                    var watch = new Stopwatch();
                    watch.Start();

                    for (int i = 0; i < RoundTripCount; i++)
                    {
                        socket.Send(msg);

                        reply = socket.Receive();
                    }

                    watch.Stop();
                    long elapsedTime = watch.ElapsedTicks;

                    Console.WriteLine("消息大小: " + messageSize + " 字节");
                    Console.WriteLine("往返次数: " + RoundTripCount);

                    double latency = (double)elapsedTime / RoundTripCount / 2 * 1000000 / Stopwatch.Frequency;
                    Console.WriteLine("平均时延: {0} 微秒", latency.ToString("f2"));
                }
            }
        }

        private static void Server()
        {
            using (var context = NetMQContext.Create())
            using (var socket = context.CreateResponseSocket())
            {
                socket.Bind("tcp://*:9999");

                foreach (int messageSize in MessageSizes)
                {
                    var message = new byte[messageSize];

                    for (int i = 0; i < RoundTripCount; i++)
                    {
                        message = socket.Receive();

                        socket.Send(message);
                    }
                }
            }
        }
    }
    #endregion

    #region 发送邮件
    class MailTest : ITest
    {
        public string TestName
        {
            get { return "发送邮件测试"; }
        }

        public void RunTest()
        {
            Task.Factory.StartNew(() =>
            {
                SendMails();
            });
        }

        public MailTest()
        {
            var milliSeconds = ConfigHelper.ClearConsolePeriod;
            clearTimer = new Timer(ClearConsole, null, milliSeconds, milliSeconds);

            mailsTimer = new Timer(GetMails, null, 0, ConfigHelper.GetMailsPeriod);
        }

        private ConcurrentQueue<string> failedList = new ConcurrentQueue<string>();
        private Timer clearTimer;
        private Timer mailsTimer;
        public int SendNum { get; private set; }
        public int CurrentPage
        {
            get { return _CurrentPage; }
            private set { _CurrentPage = value; }
        }
        private int _CurrentPage = 1;

        private void SendMails()
        {
            using (var context = NetMQContext.Create())
            using (var socket = context.CreatePullSocket())
            {
                socket.Bind("tcp://*:8888");

                while (true)
                {
                    var mail = socket.ReceiveString();
                    if (!string.IsNullOrEmpty(mail))
                    {
                        Console.WriteLine("发送邮件至: " + mail);
                        try
                        {
                            MailService.Send(mail, "标题", "内容");
                        }
                        catch
                        {
                            failedList.Enqueue(mail);
                        }
                        SendNum += 1;
                        Console.WriteLine("邮件发送总数: " + SendNum);
                    }
                }
            }
        }

        private void GetMails(object obj)
        {
            Console.WriteLine(string.Format("第{0}次获取用户数据", CurrentPage));
            var usersPerTime = ConfigHelper.UsersPerTime;
            var mails = UserService.GetEmails(CurrentPage, usersPerTime);

            if (mails != null)
            {
                using (var context = NetMQContext.Create())
                using (var socket = context.CreatePushSocket())
                {
                    socket.Connect("tcp://127.0.0.1:8888");

                    foreach (var mail in mails)
                    {
                        socket.Send(mail);
                    }
                }

                if (mails.Count == usersPerTime)
                {
                    CurrentPage += 1;
                }
                else
                {
                    clearTimer.Dispose();
                    mailsTimer.Dispose();
                }
            }

            while (failedList.Count > 0)
            {
                string mail = null;
                var result = failedList.TryDequeue(out mail);
                try
                {
                    MailService.Send(mail, "标题", "内容");
                }
                catch (Exception ex)
                {
                    //todo:记录错误日志
                    Console.WriteLine("发送失败: " + mail);
                }
            }
        }

        private void ClearConsole(object obj)
        {
            Console.Clear();
        }
    }
    #endregion
}