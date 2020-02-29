using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using LogShark.Containers;

namespace LogShark.Writers.Sql.Connections.Npgsql
{
    class NpgsqlTypeProjector
    {
        private Dictionary<Type, object> _typeProjections;

        public NpgsqlTypeProjector()
        {
            _typeProjections = new Dictionary<Type, object>();
        }

        public void GenerateTypeProjection<T>(DataSetInfo outputInfo)
        {
            var type = typeof(T);
            if (!_typeProjections.ContainsKey(type))
            {
                _typeProjections[type] = new NpgsqlTypeProjection<T>(outputInfo);
            }
        }

        public NpgsqlTypeProjection<T> GetTypeProjection<T>()
        {
            var type = typeof(T);
            if (!_typeProjections.ContainsKey(type))
            {
                throw new Exception($"TypeProjection for type '{typeof(T).FullName}' has not been generated. Call '{nameof(GenerateTypeProjection)}' first.");
            }
            return _typeProjections[type] as NpgsqlTypeProjection<T>;
        }
    }
}
