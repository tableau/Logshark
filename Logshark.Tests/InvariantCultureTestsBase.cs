using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace LogShark.Tests
{
    public abstract class InvariantCultureTestsBase
    {
        protected InvariantCultureTestsBase()
        {
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
        }
    }
}
