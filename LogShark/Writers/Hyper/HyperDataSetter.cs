using System;
using Tableau.HyperAPI;

namespace LogShark.Writers.Hyper
{
    internal static class HyperDataSetter
    {
        public static void SetBoolean(Inserter inserter, bool value)
        {
            inserter.Add(value);
        }

        public static void SetInt16(Inserter inserter, short value)
        {
            inserter.Add(value);
        }

        public static void SetInt32(Inserter inserter, int value)
        {
            inserter.Add(value);
        }

        public static void SetInt64(Inserter inserter, long value)
        {
            inserter.Add(value);
        }

        public static void SetDouble(Inserter inserter, double value)
        {
            inserter.Add(value);
        }

        public static void SetString(Inserter inserter, string value)
        {
            if (value == null)
            {
                inserter.AddNull();
            }
            else
            {
                inserter.Add(value);
            }
        }

        public static void SetDateTime(Inserter inserter, DateTime value)
        {
            inserter.Add(new Timestamp(value.Year, value.Month, value.Day, value.Hour, value.Minute, value.Second, value.Millisecond * 1000));
        }

        public static void SetDateTimeOffset(Inserter inserter, DateTimeOffset value)
        {
            SetDateTime(inserter, value.DateTime);
        }

        public static void SetNullableBoolean(Inserter inserter, bool? value)
        {
            if (value.HasValue)
            {
                inserter.Add(value.Value);
            }
            else
            {
                inserter.AddNull();
            }
        }

        public static void SetNullableInt16(Inserter inserter, short? value)
        {
            if (value.HasValue)
            {
                inserter.Add(value.Value);
            }
            else
            {
                inserter.AddNull();
            }
        }

        public static void SetNullableInt32(Inserter inserter, int? value)
        {
            if (value.HasValue)
            {
                inserter.Add(value.Value);
            }
            else
            {
                inserter.AddNull();
            }
        }

        public static void SetNullableInt64(Inserter inserter, long? value)
        {
            if (value.HasValue)
            {
                inserter.Add(value.Value);
            }
            else
            {
                inserter.AddNull();
            }
        }

        public static void SetNullableDouble(Inserter inserter, double? value)
        {
            if (value.HasValue)
            {
                inserter.Add(value.Value);
            }
            else
            {
                inserter.AddNull();
            }
        }

        public static void SetNullableDateTime(Inserter inserter, DateTime? value)
        {
            if (value.HasValue)
            {
                inserter.Add(new Timestamp(value.Value.Year, value.Value.Month, value.Value.Day, 
                    value.Value.Hour, value.Value.Minute, value.Value.Second, value.Value.Millisecond * 1000));
            }
            else
            {
                inserter.AddNull();
            }
        }
        
        public static void SetNullableDateTimeOffset(Inserter inserter, DateTimeOffset? value)
        {
            SetNullableDateTime(inserter, value?.DateTime);
        }
    }
}
