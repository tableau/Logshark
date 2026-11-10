using System;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Loader;
using AwesomeAssertions;
using LogShark.HostExtensions;
using LogShark.Shared.HostExtensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LogShark.Tests.HostExtensions
{
    public class HostExtensionLoaderTests : IDisposable
    {
        private readonly string _emptyDir = Path.Combine(Path.GetTempPath(), "HostExtensionLoaderTests_" + Guid.NewGuid().ToString("N"));
        private readonly string _fixtureDir = Path.Combine(Path.GetTempPath(), "HostExtensionLoaderTests_" + Guid.NewGuid().ToString("N"));
        private readonly string _dependencyDir = Path.Combine(Path.GetTempPath(), "HostExtensionLoaderTests_" + Guid.NewGuid().ToString("N"));

        private string FixtureAssemblyPath => Path.Combine(_fixtureDir, HostExtensionLoader.AssemblyFileName);

        public HostExtensionLoaderTests()
        {
            Directory.CreateDirectory(_emptyDir);
            Directory.CreateDirectory(_fixtureDir);
            Directory.CreateDirectory(_dependencyDir);
        }

        public void Dispose()
        {
            // Assemblies loaded into the default context stay locked for the process lifetime on Windows
            foreach (var dir in new[] { _emptyDir, _fixtureDir, _dependencyDir })
            {
                try { Directory.Delete(dir, true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
            }
        }

        [Fact]
        public void ReturnsNullWhenExtensionAssemblyAbsent()
        {
            HostExtensionLoader.Load(_emptyDir, NullLogger.Instance).Should().BeNull();
        }

        [Fact]
        public void ReturnsNullWhenDirectoryDoesNotExist()
        {
            HostExtensionLoader.Load(Path.Combine(_emptyDir, "missing"), NullLogger.Instance).Should().BeNull();
        }

        [Fact]
        public void LoadsSingleValidImplementation()
        {
            EmitExtensionAssembly(FixtureAssemblyPath, implementationCount: 1, throwInConstructor: false, baseType: typeof(object));

            var extension = HostExtensionLoader.Load(_fixtureDir, NullLogger.Instance);

            extension.Should().NotBeNull();
            extension.ConfigOverlayPath.Should().BeNull();
            extension.CreateArtifactPublisher(null, NullLoggerFactory.Instance).Should().BeOfType<NullArtifactPublisher>();
        }

        [Fact]
        public void NoExtensionYieldsNullArtifactPublisher()
        {
            HostExtensionLoader.CreateArtifactPublisher(null, null, NullLoggerFactory.Instance).Should().BeOfType<NullArtifactPublisher>();
        }

        [Fact]
        public void CorruptAssemblyFailsHardNamingTheFile()
        {
            File.WriteAllBytes(FixtureAssemblyPath, new byte[] { 0x4D, 0x5A, 0xDE, 0xAD, 0xBE, 0xEF, 0x00, 0x01, 0x02, 0x03 });

            Action load = () => HostExtensionLoader.Load(_fixtureDir, NullLogger.Instance);

            load.Should().Throw<HostExtensionLoadException>()
                .Where(e => e.Message.Contains(FixtureAssemblyPath))
                .WithInnerException<BadImageFormatException>();
        }

        [Fact]
        public void TwoImplementationsFailHard()
        {
            EmitExtensionAssembly(FixtureAssemblyPath, implementationCount: 2, throwInConstructor: false, baseType: typeof(object));

            Action load = () => HostExtensionLoader.Load(_fixtureDir, NullLogger.Instance);

            load.Should().Throw<HostExtensionLoadException>()
                .Where(e => e.Message.Contains(FixtureAssemblyPath))
                .Where(e => e.Message.Contains("Fixture.Extension0") && e.Message.Contains("Fixture.Extension1"));
        }

        [Fact]
        public void NoImplementationFailsHard()
        {
            EmitExtensionAssembly(FixtureAssemblyPath, implementationCount: 0, throwInConstructor: false, baseType: typeof(object));

            Action load = () => HostExtensionLoader.Load(_fixtureDir, NullLogger.Instance);

            load.Should().Throw<HostExtensionLoadException>()
                .Where(e => e.Message.Contains(FixtureAssemblyPath) && e.Message.Contains(nameof(ILogSharkHostExtension)));
        }

        [Fact]
        public void ThrowingConstructorFailsHardWithCause()
        {
            EmitExtensionAssembly(FixtureAssemblyPath, implementationCount: 1, throwInConstructor: true, baseType: typeof(object));

            Action load = () => HostExtensionLoader.Load(_fixtureDir, NullLogger.Instance);

            load.Should().Throw<HostExtensionLoadException>()
                .Where(e => e.Message.Contains(FixtureAssemblyPath))
                .Where(e => e.Message.Contains(ConstructorFailureMessage));
        }

        [Fact]
        public void MissingDependencyFailsHardNamingTheDependency()
        {
            var dependencyName = "FixtureDependency_" + Guid.NewGuid().ToString("N");
            var dependencyPath = Path.Combine(_dependencyDir, dependencyName + ".dll");
            EmitBaseClassAssembly(dependencyPath, dependencyName);
            // Loaded into a private context only so the fixture can be emitted against it; the default context never sees it
            var dependencyBase = new AssemblyLoadContext(null).LoadFromAssemblyPath(dependencyPath).GetType("Fixture.Base", throwOnError: true);
            EmitExtensionAssembly(FixtureAssemblyPath, implementationCount: 1, throwInConstructor: false, baseType: dependencyBase);

            Action load = () => HostExtensionLoader.Load(_fixtureDir, NullLogger.Instance);

            load.Should().Throw<HostExtensionLoadException>()
                .Where(e => e.Message.Contains(FixtureAssemblyPath) && e.Message.Contains(dependencyName));
        }

        private const string ConstructorFailureMessage = "fixture constructor failure";

        private static void EmitBaseClassAssembly(string path, string assemblyName)
        {
            var assemblyBuilder = new PersistedAssemblyBuilder(new AssemblyName(assemblyName), typeof(object).Assembly);
            var module = assemblyBuilder.DefineDynamicModule(assemblyName);
            var type = module.DefineType("Fixture.Base", TypeAttributes.Public | TypeAttributes.Class);
            type.DefineDefaultConstructor(MethodAttributes.Public);
            type.CreateType();
            assemblyBuilder.Save(path);
        }

        // Each fixture gets a unique assembly name: the loader uses the default context, which allows one assembly per name
        private static void EmitExtensionAssembly(string path, int implementationCount, bool throwInConstructor, Type baseType)
        {
            var assemblyName = "FixtureExtension_" + Guid.NewGuid().ToString("N");
            var assemblyBuilder = new PersistedAssemblyBuilder(new AssemblyName(assemblyName), typeof(object).Assembly);
            var module = assemblyBuilder.DefineDynamicModule(assemblyName);

            if (implementationCount == 0)
            {
                module.DefineType("Fixture.NotAnExtension", TypeAttributes.Public | TypeAttributes.Class).CreateType();
            }

            for (var i = 0; i < implementationCount; i++)
            {
                var type = module.DefineType($"Fixture.Extension{i}", TypeAttributes.Public | TypeAttributes.Class, baseType);
                type.AddInterfaceImplementation(typeof(ILogSharkHostExtension));

                var ctor = type.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, Type.EmptyTypes);
                var ctorIl = ctor.GetILGenerator();
                ctorIl.Emit(OpCodes.Ldarg_0);
                ctorIl.Emit(OpCodes.Call, baseType.GetConstructor(Type.EmptyTypes)!);
                if (throwInConstructor)
                {
                    ctorIl.Emit(OpCodes.Ldstr, ConstructorFailureMessage);
                    ctorIl.Emit(OpCodes.Newobj, typeof(InvalidOperationException).GetConstructor(new[] { typeof(string) })!);
                    ctorIl.Emit(OpCodes.Throw);
                }
                else
                {
                    ctorIl.Emit(OpCodes.Ret);
                }

                const MethodAttributes interfaceImpl = MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Final;

                var getOverlay = type.DefineMethod("get_ConfigOverlayPath", interfaceImpl | MethodAttributes.SpecialName, typeof(string), Type.EmptyTypes);
                var getOverlayIl = getOverlay.GetILGenerator();
                getOverlayIl.Emit(OpCodes.Ldnull);
                getOverlayIl.Emit(OpCodes.Ret);
                type.DefineMethodOverride(getOverlay, typeof(ILogSharkHostExtension).GetProperty(nameof(ILogSharkHostExtension.ConfigOverlayPath))!.GetMethod!);
                type.DefineProperty(nameof(ILogSharkHostExtension.ConfigOverlayPath), PropertyAttributes.None, typeof(string), null).SetGetMethod(getOverlay);

                var create = type.DefineMethod(nameof(ILogSharkHostExtension.CreateArtifactPublisher), interfaceImpl, typeof(IArtifactPublisher), new[] { typeof(IConfiguration), typeof(ILoggerFactory) });
                var createIl = create.GetILGenerator();
                createIl.Emit(OpCodes.Newobj, typeof(NullArtifactPublisher).GetConstructor(Type.EmptyTypes)!);
                createIl.Emit(OpCodes.Ret);
                type.DefineMethodOverride(create, typeof(ILogSharkHostExtension).GetMethod(nameof(ILogSharkHostExtension.CreateArtifactPublisher))!);

                type.CreateType();
            }

            assemblyBuilder.Save(path);
        }
    }
}
