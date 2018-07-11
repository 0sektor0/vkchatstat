using System.Threading;



namespace vkchatstat
{
    public class CountedSemaphore
    {
        private int maximum_count;
        private int count;
        private int limit;
        private object locker = new object();


        public CountedSemaphore(int initial_count, int maximum_count)
        {
            count = initial_count;
            limit = maximum_count;
            this.maximum_count = maximum_count;
        }


        public void WaitOne()
        {
            lock (locker)
            {
                while (count == 0) 
                {
                    Monitor.Wait(locker);
                }

                count--;
            }
        }


        public bool TryRelease()
        {
            lock(locker)
            {
                if (count < limit)
                {
                    count++;
                    Monitor.PulseAll(locker);
                    return true;
                }

                return false;
            }
        }


        public bool ReleaseAll()
        {
            lock(locker)
            {
                if(count != maximum_count)
                {
                    count = maximum_count;
                    return true;
                }
                
                return false;
            }
        }
    }
}