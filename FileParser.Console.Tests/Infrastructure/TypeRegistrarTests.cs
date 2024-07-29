using FileParser.Console.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Testing;

namespace FileParser.Console.Tests.Infrastructure;

[TestFixture]
internal class TypeRegistrarTests
{
    [Test]
    public void TypeRegistrar_Implementation_Should_Be_Correct()
    {
        var typeRegistrarBaseTests = new TypeRegistrarBaseTests(() => new TypeRegistrar(new ServiceCollection()));

        Assert.That(typeRegistrarBaseTests.RunAllTests, Throws.Nothing);
    }
}
