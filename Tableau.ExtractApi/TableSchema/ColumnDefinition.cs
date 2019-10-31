using System;
using System.Collections.Generic;
using System.Reflection;
using Tableau.ExtractApi.Extensions;

using ExtractDataType = com.tableausoftware.common.Type;

namespace Tableau.ExtractApi.TableSchema
{
    public sealed class ColumnDefinition
    {
        private static readonly IDictionary<Type, ExtractDataType> systemToHyperTypeMap = new Dictionary<Type, ExtractDataType>
        {
            { typeof(bool), ExtractDataType.BOOLEAN },
            { typeof(char), ExtractDataType.UNICODE_STRING },
            { typeof(DateTime), ExtractDataType.DATETIME },
            { typeof(decimal), ExtractDataType.DOUBLE },
            { typeof(double), ExtractDataType.DOUBLE },
            { typeof(float), ExtractDataType.DOUBLE },
            { typeof(int), ExtractDataType.INTEGER },
            { typeof(long), ExtractDataType.INTEGER },
            { typeof(string), ExtractDataType.UNICODE_STRING },
            { typeof(TimeSpan), ExtractDataType.DURATION }
        };

        public string Name { get; private set; }

        public Type Type { get; private set; }

        public PropertyInfo Property { get; private set; }

        public Type InnerType { get; private set; }

        public bool IsNullable { get; private set; }

        public ExtractDataType ExtractType { get; private set; }

        public ColumnDefinition(PropertyInfo property)
        {
            Name = property.Name.ToSnakeCase();
            Property = property;
            Type = property.PropertyType;
            InnerType = Nullable.GetUnderlyingType(Type);
            IsNullable = InnerType != null;

            if (IsNullable && InnerType != null && systemToHyperTypeMap.ContainsKey(InnerType))
            {
                ExtractType = systemToHyperTypeMap[InnerType];
            }
            else if (systemToHyperTypeMap.ContainsKey(Type))
            {
                ExtractType = systemToHyperTypeMap[Type];
            }
            else
            {
                ExtractType = ExtractDataType.UNICODE_STRING;
            }
        }
    }
}