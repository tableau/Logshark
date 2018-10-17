using Optional;
using System;
using Tableau.ExtractApi.Exceptions;

namespace Tableau.ExtractApi
{
    public interface IExtract<T> : IDisposable where T : new()
    {
        Option<T, ExtractInsertionException> Insert(T item);
    }
}