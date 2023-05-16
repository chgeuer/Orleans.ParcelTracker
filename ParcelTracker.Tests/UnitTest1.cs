namespace ParcelTracker.Tests
{
    internal static class MyExtension
    {
        internal record QueueAndPrio<T>(Dictionary<int, Queue<T>> Queue, int Priority);

        private static Func<Task> PutJob<T>(this Dictionary<int, Queue<T>> queue, int prio, T value, Func<Task>? persist = null)
        {
            return () => queue.AddJob(new Job<T>(Priority: prio, JobDescription: value), persist);
        }

        internal static QueueAndPrio<T> WithPrio<T>(this Dictionary<int, Queue<T>> queue, int prio) => new(queue, prio);

        internal static Func<Task> PutJob<T>(this QueueAndPrio<T> qp, T job, Func<Task>? persist = null) => qp.Queue.PutJob(qp.Priority, job, persist);

        internal static Func<Task> AssertNextJobIs<T>(this Dictionary<int, Queue<T>> queue, T? expected, Func<Task>? persist = null)
        {
            return async () =>
            {
                var job = await queue.GetJob(persist);
                if (job == null)
                {
                    Assert.That(expected, Is.Null);
                }
                else
                {
                    Assert.That(job.JobDescription, Is.EqualTo(expected));
                }
            };
        }

        internal static async Task RunAll(this IEnumerable<Func<Task>> tasks)
        {
            foreach (var t in tasks)
            {
                await t();
            }
        }
    }

    public class Tests
    {
        //[SetUp]
        //public void Setup() { }

        [Test]
        public Task Test1()
        {
            Dictionary<int, Queue<int?>> q = new();

            IEnumerable<Func<Task>> tasks = new[]
            {
                q.WithPrio(1).PutJob(1),
                q.WithPrio(1).PutJob(2),
                q.WithPrio(8).PutJob(3),
                q.WithPrio(2).PutJob(4),
                q.WithPrio(1).PutJob(5),
                q.WithPrio(10).PutJob(6),
                q.WithPrio(1).PutJob(7),
                q.AssertNextJobIs(1),
                q.AssertNextJobIs(2),
                q.AssertNextJobIs(5),
                q.AssertNextJobIs(7),
                q.AssertNextJobIs(4),
                q.AssertNextJobIs(3),
                q.AssertNextJobIs(6),
                q.AssertNextJobIs(null),
                q.AssertNextJobIs(null)
            };

            return tasks.RunAll();
        }

        [Test]
        public Task Test2()
        {
            Dictionary<int, Queue<int?>> q = new();

            IEnumerable<Func<Task>> tasks = new[]
            {
                q.WithPrio(1).PutJob(1),    // 1: [1]
                q.AssertNextJobIs(1),       // empty
                q.AssertNextJobIs(null),
                q.WithPrio(10).PutJob(2),   // 10: [2]
                q.AssertNextJobIs(2),       // empty
                q.AssertNextJobIs(null),
                q.WithPrio(5).PutJob(3),    // 5: [3]
                q.WithPrio(1).PutJob(4),    // 1: [4], 5: [3]
                q.AssertNextJobIs(4),       //         5: [3]
                q.WithPrio(1).PutJob(5),    // 1: [5], 5: [3]
                q.WithPrio(5).PutJob(6),    // 1: [5], 5: [3, 6]
                q.AssertNextJobIs(5),       // 5: [3, 6]
                q.AssertNextJobIs(3),       // 5: [6]
                q.AssertNextJobIs(6),       // empty
                q.AssertNextJobIs(null),
            };

            return tasks.RunAll();
        }
    }
}