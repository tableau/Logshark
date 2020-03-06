using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace LogShark.Writers.Sql.Connections.Npgsql
{
    public class NpgsqlTypePropertyProjection<T>
    {
        public Func<T, object> GetPropertyValue { get; private set; }
        public string ColumnName { get; set; }
        public int? Order { get; set; }
        public string NpgsqlTypeName { get; set; }

        public NpgsqlTypePropertyProjection(PropertyInfo propertyInfo)
        {
            // Compiling an expression tree into a delegate is slower upfront but roughly 5x faster to execute than the following reflection:
            //    GetPropertyValue = model => propertyInfo.GetValue(model);
            var parameterExpression = Expression.Parameter(typeof(T));
            var propertyExpression = Expression.Property(parameterExpression, propertyInfo);
            var convert = Expression.Convert(propertyExpression, typeof(object));
            var lambda = Expression.Lambda<Func<T, object>>(convert, parameterExpression);
            GetPropertyValue = lambda.Compile();

            var typeAttributes = typeof(T).GetCustomAttributes(true).Select(a => a as Attribute);
            var columnAttribute = typeAttributes.OfType<ColumnAttribute>().FirstOrDefault();
            ColumnName = columnAttribute?.Name ?? propertyInfo.Name;
            Order = columnAttribute?.Order;
            NpgsqlTypeName = columnAttribute?.TypeName ?? GetNpgsqlTypeNameForClrType(propertyInfo.PropertyType);
        }

        private string GetNpgsqlTypeNameForClrType(Type clrType)
        {
            var underlyingType = Nullable.GetUnderlyingType(clrType) ?? clrType;
            switch (Type.GetTypeCode(underlyingType))
            {
                case TypeCode.Boolean: return "boolean";
                case TypeCode.Byte: return "smallint";
                case TypeCode.Char: return "text";
                case TypeCode.DateTime: return "timestamp";
                case TypeCode.Decimal: return "numeric";
                case TypeCode.Double: return "double precision";
                case TypeCode.Int16: return "smallint";
                case TypeCode.Int32: return "integer";
                case TypeCode.Int64: return "bigint";
                case TypeCode.SByte: return "smallint";
                case TypeCode.String: return "text";
                case TypeCode.UInt16: return "bigint";
                case TypeCode.UInt32: return "bigint";
                case TypeCode.UInt64: return "bigint";
                default: return "text";
            }
        }
    }
}
