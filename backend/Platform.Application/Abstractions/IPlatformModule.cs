using System.Reflection;

namespace Platform.Application.Abstractions;

public interface IPlatformModule
{
    string Name { get; }

    Assembly Assembly { get; }
}
