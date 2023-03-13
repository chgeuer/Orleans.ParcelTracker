namespace ParcelTracker.Tests
{

    internal static class MyExtension
    {
        internal static Func<Task> PutJob<T>(this Dictionary<int, Queue<T>> queue, int prio, T value)
        {
            return () => queue.AddJob(new Job<T>(Priority: prio, JobDescription: value));
        }

        internal static Func<Task> AssertNextJobIs<T>(this Dictionary<int, Queue<T>> queue, T? expected)
        {
            return async () =>
            {
                var job = await queue.GetJob();
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
                q.PutJob(1, 1),
                q.PutJob(1, 2),
                q.PutJob(8, 3),
                q.PutJob(2, 4),
                q.PutJob(1, 5),
                q.PutJob(10, 6),
                q.PutJob(1, 7),
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
                q.PutJob(1, 1), // 1: 1
                q.AssertNextJobIs(1), // []
                q.AssertNextJobIs(null),
                q.PutJob(10, 2), // 10: 2
                q.AssertNextJobIs(2), // empty
                q.AssertNextJobIs(null),
                q.PutJob(5, 3), // 5: 3
                q.PutJob(1, 4), // 1: 4, 5: 3
                q.AssertNextJobIs(4), // 5: 3
                q.PutJob(1, 5), // 1: 5, 5: 3
                q.AssertNextJobIs(5), // 5: 3
                q.AssertNextJobIs(3), // empty
                q.AssertNextJobIs(null),
            };

            return tasks.RunAll();
        }
    }
}