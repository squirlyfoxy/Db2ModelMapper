using System;

namespace Db2ModelMapper.Core.ModelsAttributes
{
    public enum Db2KeyValueUsage
    {
        Key = 0,
        Value = 1,
    }

    public class Db2KeyValue : Attribute
    {
        public Db2KeyValue(Db2KeyValueUsage usage)
        {
            this.Usage = usage;
        }

        public Db2KeyValueUsage Usage { get; private set; }
    }
}
