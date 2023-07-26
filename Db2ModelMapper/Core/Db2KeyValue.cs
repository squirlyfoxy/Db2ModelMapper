using System;
using System.Linq.Expressions;
using Db2ModelMapper.Core.ModelsAttributes;

namespace Db2ModelMapper.Core
{
    public class Db2KeyValue<T>
    {
        public Db2KeyValue(Expression<Func<T, object>> properySelector, object valueToSearch, bool filter = true)
        {
            this.ValueToSearch = valueToSearch;

            var properties = typeof(T).GetProperties();
            var propToSearch = properySelector.Body as MemberExpression ??
                ((UnaryExpression)properySelector.Body).Operand as MemberExpression;

            foreach (var property in properties)
            {
                if (property.Name == propToSearch.Member.Name)
                {
                    foreach (var prop in propToSearch.Member.GetCustomAttributes(true))
                    {
                        if (prop is Db2Data)
                        {
                            this.PropertyToSearch = ((Db2Data)prop).Column;
                        }
                    }
                }
            }

            this.Filter = filter;
        }

        public object ValueToSearch { get; set; }

        public string PropertyToSearch { get; set; }

        public bool Filter { get; set; }
    }
}
