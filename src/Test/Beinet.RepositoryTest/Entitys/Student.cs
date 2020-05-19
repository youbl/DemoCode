

using System;
using Beinet.Repository;
using Beinet.Repository.Entitys;

namespace Beinet.RepositoryTest.Entitys
{
    [Entity]
    [Table(Name = "bookingarea", Catalog = "booking")]
    public class Student
    {
        /// <summary>
        /// 数据库自增字段，主键
        /// </summary>
        [Id]
        [GeneratedValue(Strategy = GenerationType.IDENTITY)]
        public long Id { get; set; }

        public long RestId { get; set; }

        public string AreaName { get; set; }

        public int MinPeople { get; set; }

        public int MaxPeople { get; set; }

        public int Sort { get; set; }

        [Column(Insertable = false, Updatable = false)]
        public DateTime CreationTime { get; set; }

        [Column(Name= "LastModificationTime", Insertable = false, Updatable = false)]
        public DateTime LastTime { get; set; }
    }
}
