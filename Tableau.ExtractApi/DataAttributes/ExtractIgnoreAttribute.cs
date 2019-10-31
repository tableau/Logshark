using System;

namespace Tableau.ExtractApi.DataAttributes
{
    /// <summary>
    /// If present, indicates that the annotated property should not be persisted in any resultant data tables.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class ExtractIgnoreAttribute : Attribute
    {
    }
}