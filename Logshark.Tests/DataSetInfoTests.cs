using System.Collections.Generic;
using FluentAssertions;
using LogShark.Containers;
using Xunit;

namespace LogShark.Tests
{
    public class DataSetInfoTests : InvariantCultureTestsBase
    {
        [Fact]
        public void VerifyHashSetFunctionality()
        {
            var info1 = new DataSetInfo("group", "name");
            var info2 = new DataSetInfo("group", "name");
            var set = new HashSet<DataSetInfo> { info1 };
            
            var secondElementAdded = set.Add(info2);
            
            secondElementAdded.Should().Be(false);
        }
        
        [Fact]
        public void VerifyEqualityMethod()
        {
            var info1 = new DataSetInfo("group", "name");
            var info2 = new DataSetInfo("group", "name");
            
            var areEqual = info1.Equals(info2);
            
            areEqual.Should().Be(true);
        }

        [Fact]
        public void VerifyEqualityOperator()
        {
            var info1 = new DataSetInfo("group", "name");
            var info2 = new DataSetInfo("group", "name");
            
            var areEqual = info1 == info2;
            
            areEqual.Should().Be(true);
        }
    }
}