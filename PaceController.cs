using System;
using System.Threading;



namespace vkchatstat
{
    public class PaceController
    {
        DateTime last_time = DateTime.UtcNow;
        CountedSemaphore sem;
        Timer timer;
        int pace;
        int max;


        public PaceController(int max, int pace)
        {
            this.max = max;
            this.pace = pace;
            sem = new CountedSemaphore(max, max);
            
            TimerCallback tc = new TimerCallback(Release);
            timer = new Timer(tc, null, pace, pace);
        }


        public void Sieze()
        {
            sem.WaitOne();
        }


        private void Release(object obj)
        {
            sem.ReleaseAll();
        }
    }
}