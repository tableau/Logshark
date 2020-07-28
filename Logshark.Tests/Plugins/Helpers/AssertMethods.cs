using System;
using System.Collections.Generic;
using System.Reflection;
using FluentAssertions;

namespace LogShark.Tests.Plugins.Helpers
{
    public static class AssertMethods
    {
        /// <summary>
        /// The goal for this method is to verify that a certain class instance has certain properties set to expected values,
        /// while all other properties defined in this class are set to their default values (usually - null).
        /// This only inspects props declared within the class itself and ignores inherited properties
        ///
        /// This is useful to verify all properties in a sparsely populated class with many properties and helps to catch
        /// any unexpected additional values 
        /// </summary>
        public static void AssertThatAllClassOwnPropsAreAtDefaultExpectFor<T>(
            T objectToInspect,
            IDictionary<string, object> expectedNonNullValues,
            string testName)
        {
            var properties = objectToInspect.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
            var encounteredProps = new List<string>();

            foreach (var property in properties)
            {
                if (expectedNonNullValues.ContainsKey(property.Name))
                {
                    var expectedValue = expectedNonNullValues[property.Name];
                    property.GetValue(objectToInspect).Should().Be(expectedValue, $"Test: `{testName}`. Property {property.Name} expected to be `{expectedValue}`");
                    encounteredProps.Add(property.Name);
                }
                else
                {
                    property.GetValue(objectToInspect).Should().Be(GetDefault(property), $"Test: `{testName}`. Property {property.Name} expected to be at default value, as it doesn't have explicitly specified value in expected dictionary");
                }
            }

            encounteredProps.Should().BeEquivalentTo(expectedNonNullValues.Keys);
        }
        
        private static object GetDefault(PropertyInfo propertyInfo)
        {
            var type = propertyInfo.GetType();
            return type.IsValueType
                ? Activator.CreateInstance(type)
                : null;
        }
    }
}