namespace KristofferStrube.INuget.Client;

public class NamespaceHierarchy(string name)
{
    public string Name => name;
    public List<NamespaceHierarchy> ChildHierarchies { get; } = [];
    public List<Type> ChildTypes { get; } = [];
}
