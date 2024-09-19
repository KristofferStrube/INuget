using Microsoft.AspNetCore.Components;
using System.Reflection;

namespace KristofferStrube.INuget.Client.Components;
public partial class TypePresenter
{
    [Parameter, EditorRequired]
    public required Type Type { get; set; }

    private object? constructedValue;

    private object? returnValue;

    private ConstructorInfo[] Constructors => Type.GetConstructors()
        .Where(c => c.GetCustomAttribute<ObsoleteAttribute>() is null)
        .ToArray();

    private MethodInfo[] Methods => Type.GetMethods()
        .Where(c =>
            c.IsPublic
            && !c.IsStatic
            && !c.IsSpecialName
            && c.GetCustomAttribute<ObsoleteAttribute>() is null
        )
        .ToArray();

    private void Created(object? value)
    {
        constructedValue = value;
        StateHasChanged();
    }
}