using System;

namespace Db2ModelMapper.Core.ModelsAttributes
{
    public class Db2File : Attribute
    {
        public Db2File(string fileName)
        {
            FileName = fileName;
        }

        public string FileName { get; set; }
    }
}
