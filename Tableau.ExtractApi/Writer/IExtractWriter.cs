using Optional;
using System;
using Tableau.ExtractApi.Exceptions;
using Tableau.ExtractApi.Helpers;

namespace Tableau.ExtractApi.Writer
{
    internal interface IExtractWriter<in T> : IDisposable
    {
        Option<Unit, ExtractInsertionException> Insert(T item);
    }
}