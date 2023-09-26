using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace UnitTests.GenericExpressionsUnitTest
{
    public class StringContainsExpressionUnitTest
    {
        public class TestClass
        {
            public string Name { get; set; } = string.Empty;
            public string? NullableName { get; set; }
            public DateTime DateTest { get; set; }
            public int IntTest { get; set; }
            public double DoubleTest { get; set; }
            public long LongTest { get; set; }
            public decimal DecimalTest { get; set; }
        }

        public static IEnumerable<object[]> StringContainsExpression_Should_Return_Null_Data()
        {
            yield return new object[] { null! };
            yield return new object[] { "" };
            yield return new object[] { "    " };
            yield return new object[] { "\t\n\r" };
        }
        [MemberData(nameof(StringContainsExpression_Should_Return_Null_Data))]
        [Theory]
        public static void StringContainsExpression_Should_Return_Null(string searchExpression)
        {
            ParameterExpression parameter = Expression.Parameter(typeof(TestClass));
            PropertyInfo propertyInfo = typeof(TestClass).GetProperty(nameof(TestClass.Name))!;
            Expression? expression = GenericExpressions.StringContainsExpression(parameter, propertyInfo, searchExpression);
            expression.Should().BeNull();
        }

        public static IEnumerable<object[]> StringContainsExpression_Should_Return_Expression_Data()
        {
            yield return new object[] { "test test" };
            yield return new object[] { "1234" };
        }
        [MemberData(nameof(StringContainsExpression_Should_Return_Expression_Data))]
        [Theory]
        public static void StringContainsExpression_Should_Return_Expression(string searchExpression)
        {
            ParameterExpression parameter = Expression.Parameter(typeof(TestClass));
            PropertyInfo propertyInfo = typeof(TestClass).GetProperty(nameof(TestClass.Name))!;
            Expression? expression = GenericExpressions.StringContainsExpression(parameter, propertyInfo, searchExpression);
            expression.Should().NotBeNull();
        }

        public static IEnumerable<object[]> StringContainsExpression_Should_Filter_Int_Data()
        {
            List<TestClass> collection = new()
            {
                new(){ IntTest = 1 },
                new(){ IntTest = 2 },
                new(){ IntTest = 255 },
                new(){ IntTest = 1_000 },
                new(){ IntTest = 1_000_000 },
                new(){ IntTest = 2_500_255 },
                new(){ IntTest = 3_333_333 },
            };

            yield return new object[] { "1", collection, 3 };
            yield return new object[] { "2", collection, 3 };
            yield return new object[] { "000", collection, 2 };
            yield return new object[] { "3", collection, 1 };
            yield return new object[] { "9", collection, 0 };
        }
        [MemberData(nameof(StringContainsExpression_Should_Filter_Int_Data))]
        [Theory]
        public static void StringContainsExpression_Should_Filter_Int(string searchExpression, List<TestClass> collection, int countAfterFilter)
        {
            ParameterExpression parameter = Expression.Parameter(typeof(TestClass));
            PropertyInfo property = typeof(TestClass).GetProperty(nameof(TestClass.IntTest))!;
            Expression expression = GenericExpressions.StringContainsExpression(parameter, property, searchExpression)!;

            collection
                .AsQueryable()
                .Where(Expression.Lambda<Func<TestClass, bool>>(expression, parameter))
                .Count()
                .Should()
                .Be(countAfterFilter);
        }

        /* Note: 
         *      The UnitTest is case sensitive, when used on collection in-memory.
         *      When used in combination with EF query, the database decides if its case sensitive.
         *      Using the EF interpretation of string.Contains()
        */
        public static IEnumerable<object[]> StringContainsExpression_Should_Filter_String_Data()
        {
            List<TestClass> collection = new()
            {
                new() { Name = "cat" },
                new() { Name = "dog" },
                new() { Name = "salmon" },
                new() { Name = "monkey" },
                new() { Name = "puppy" },
                new() { Name = "caterpillar" },
            };

            yield return new object[] { "cat", collection, 2 };
            yield return new object[] { "cat", collection, 2 };
            yield return new object[] { "mon", collection, 2 };
            yield return new object[] { "dog", collection, 1 };
            yield return new object[] { "pp", collection, 1 };
            yield return new object[] { "kitten", collection, 0 };
        }
        [MemberData(nameof(StringContainsExpression_Should_Filter_String_Data))]
        [Theory]
        public static void StringContainsExpression_Should_Filter_String(string searchExpression, List<TestClass> collection, int countAfterFilter)
        {
            ParameterExpression parameter = Expression.Parameter(typeof(TestClass));
            PropertyInfo property = typeof(TestClass).GetProperty(nameof(TestClass.Name))!;
            Expression expression = GenericExpressions.StringContainsExpression(parameter, property, searchExpression)!;

            collection
                .AsQueryable()
                .Where(Expression.Lambda<Func<TestClass, bool>>(expression, parameter))
                .Count()
                .Should()
                .Be(countAfterFilter);
        }
    }
}
