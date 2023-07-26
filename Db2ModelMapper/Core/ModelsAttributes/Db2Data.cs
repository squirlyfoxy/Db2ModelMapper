using System;

namespace Db2ModelMapper.Core.ModelsAttributes
{
    public class Db2Data : Attribute
    {
        public Db2Data(string column, string customFormat = null)
        {
            Column = column;
            CustomFormat = customFormat;
        }

        public string Column { get; set; }

        public string CustomFormat { get; set; }
    }
}
