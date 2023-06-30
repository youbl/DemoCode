namespace OPJobs.MySql
{
    class DbItem
    {
        /// <summary>
        /// 数据库类型, MySql
        /// </summary>
        public DbType Type { get; set; }
        public string Server { get; set; }
        public int Port { get; set; }
        public string Uid { get; set; }
        public string Pwd { get; set; }
        public string Database { get; set; }

        /// <summary>
        /// 是否要导出数据
        /// </summary>
        public bool ExportData { get; set; }
    }

    public enum DbType
    {
        MySql
    }
}
