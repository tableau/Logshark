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
        // Keyed by (Type, DataSetInfo) rather than just Type, since multiple writers can share the same POCO type
        // (e.g. Art plugin uses FlattenedArtEvent for both its "Art" and "ArtDesktop" data sets).
        private Dictionary<(Type Type, DataSetInfo DataSetInfo), object> _typeProjections;

        public NpgsqlTypeProjector()
        {
            _typeProjections = new Dictionary<(Type, DataSetInfo), object>();
        }

        public void GenerateTypeProjection<T>(DataSetInfo outputInfo)
        {
            var key = (typeof(T), outputInfo);
            if (!_typeProjections.ContainsKey(key))
            {
                _typeProjections[key] = new NpgsqlTypeProjection<T>(outputInfo);
            }
        }

        public NpgsqlTypeProjection<T> GetTypeProjection<T>(DataSetInfo outputInfo)
        {
            var key = (typeof(T), outputInfo);
            if (!_typeProjections.ContainsKey(key))
            {
                throw new Exception($"TypeProjection for type '{typeof(T).FullName}' and data set '{outputInfo}' has not been generated. Call '{nameof(GenerateTypeProjection)}' first.");
            }
            return _typeProjections[key] as NpgsqlTypeProjection<T>;
        }
    }
}
