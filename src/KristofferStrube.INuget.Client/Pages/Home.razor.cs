using KristofferStrube.INuget.Client.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System.Reflection;
using System.Runtime.Loader;

namespace KristofferStrube.INuget.Client.Pages;

public partial class Home : ComponentBase
{
    private string packageNameInput = "";
    private string packageVersionInput = "";

    private List<NugetClient.PackageVersion> versions = new();
    private string? error;
    Assembly? assembly;
    Dictionary<string, bool>? downloads;
    List<Type>? types;
    NamespaceHierarchy? namespaces;
    bool redirect;
    int loadedTypes = 0;
    string? status;

    [Parameter]
    public string? PackageName { get; set; }

    [Parameter]
    public string? PackageVersion { get; set; }

    [Inject]
    public required NugetClient NugetClient { get; set; }

    [Inject]
    public required NavigationManager NavigationManager { get; set; }

    protected override async Task OnParametersSetAsync()
    {
        redirect = false;
        if (PackageName is not null && packageNameInput is "")
        {
            await FindPackage();
        }
        if (PackageVersion is not null && packageVersionInput is "")
        {
            await LookupPackageVersion();
        }
        redirect = true;
    }

    public async Task Enter(KeyboardEventArgs e)
    {
        if (e.Code == "Enter" || e.Code == "NumpadEnter")
        {
            await FindPackage();
        }
    }

    public async Task FindPackage()
    {
        try
        {
            if (PackageName is not null && packageNameInput is "")
            {
                packageNameInput = PackageName;
            }

            status = "Finding versions of NuGet package";
            await InvokeAsync(StateHasChanged);

            versions = await NugetClient.Versions(packageNameInput.ToLower());
            if (packageNameInput is not "")
            {
                PackageName = packageNameInput;
            }
            if (redirect)
            {
                NavigationManager.NavigateTo($"packages/{PackageName}/", replace: true);
            }
            error = null;
        }
        catch
        {
            error = $"Could not find package with id '{packageNameInput}'.";
        }
        status = null;
    }

    public async Task LookupPackageVersion()
    {
        try
        {
            AssemblyLoadContext context = new(PackageName, isCollectible: true);

            if (PackageVersion is not null && packageVersionInput is "")
            {
                packageVersionInput = PackageVersion;
            }

            status = "Resolving dependencies.";
            await InvokeAsync(StateHasChanged);

            var dependencies = await NugetClient.Dependencies(PackageName, packageVersionInput);

            status = null;
            await InvokeAsync(StateHasChanged);

            if (packageVersionInput is not "")
            {
                PackageVersion = packageVersionInput;
            }
            if (redirect)
            {
                NavigationManager.NavigateTo($"packages/{PackageName}/{PackageVersion}/", replace: true);
            }
            downloads = dependencies.DistinctBy(d => d.Id).ToDictionary(d => $"{d.Id} ({d.Version})", d => false);
            await InvokeAsync(StateHasChanged);
            await Task.Delay(100);

            var mainDependency = dependencies.First();
            using Stream mainDll = await NugetClient.DLL(mainDependency.Id, mainDependency.Version);
            var mainAssembly = context.LoadFromStream(mainDll);
            downloads[$"{mainDependency.Id} ({mainDependency.Version})"] = true;
            await InvokeAsync(StateHasChanged);
            await Task.Delay(100);

            await Task.WhenAll(
                dependencies.Skip(1).Select(async dependency =>
                {
                    using Stream dll = await NugetClient.DLL(dependency.Id, dependency.Version);
                    context.LoadFromStream(dll);
                    downloads[$"{dependency.Id} ({dependency.Version})"] = true;
                    await InvokeAsync(StateHasChanged);
                    await Task.Delay(10);
                })
            );

            downloads = null;

            types = mainAssembly
                .GetExportedTypes()
                .Where(t => !t.IsGenericType)
                .Where(t => t.GetCustomAttribute<ObsoleteAttribute>() is null)
                .Where(t => t.GetConstructors().Where(c => c.GetCustomAttribute<ObsoleteAttribute>() is null && c.IsPublic).Count() != 0)
                .ToList();

            namespaces = new("root");
            loadedTypes = 0;
            foreach (Type type in types)
            {
                loadedTypes++;
                await InvokeAsync(StateHasChanged);
                await Task.Delay(1);

                if (type.Namespace is not { } typeNamespace)
                    return;

                string[] namespaceParts = typeNamespace.Split(".");
                NamespaceHierarchy currentHierarchy = namespaces;
                foreach (string part in namespaceParts)
                {
                    if (currentHierarchy.ChildHierarchies.FirstOrDefault(c => c.Name == part) is not { } childHierarchy)
                    {
                        childHierarchy = new(part);
                        currentHierarchy.ChildHierarchies.Add(childHierarchy);
                    }
                    currentHierarchy = childHierarchy;
                }
                currentHierarchy.ChildTypes.Add(type);
            }

            error = null;
        }
        catch (Exception e)
        {
            error = $"Failed to get version of package.";
            Console.WriteLine(e.Message);
        }
        status = null;
    }
}