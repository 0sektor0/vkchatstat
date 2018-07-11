namespace vkchatstat
{
    public class Statistic
    {
        public int stat_count;
        public string name;
        public long id;


        public Statistic(string name, long id, int stat_count)
        {
            this.name = name;
            this.stat_count = stat_count;
            this.id = id; 
        }
    }
}