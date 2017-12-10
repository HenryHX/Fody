using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Mono.Cecil;

public class AssemblyResolver : IAssemblyResolver
{
    Dictionary<string, string> referenceDictionary;
    ILogger logger;
    List<string> splitReferences;
    Dictionary<string, AssemblyDefinition> assemblyDefinitionCache = new Dictionary<string, AssemblyDefinition>(StringComparer.InvariantCultureIgnoreCase);

    public AssemblyResolver(Dictionary<string, string> referenceDictionary, ILogger logger, List<string> splitReferences)
    {
        this.referenceDictionary = referenceDictionary;
        this.logger = logger;
        this.splitReferences = splitReferences;
    }

    AssemblyDefinition GetAssembly(string file, ReaderParameters parameters)
    {
        if (assemblyDefinitionCache.TryGetValue(file, out var assembly))
        {
            return assembly;
        }
        if (parameters.AssemblyResolver == null)
        {
            parameters.AssemblyResolver = this;
        }
        try
        {
            return assemblyDefinitionCache[file] = AssemblyDefinition.ReadAssembly(file, parameters);
        }
        catch (Exception exception)
        {
            throw new Exception($"Could not read '{file}'.", exception);
        }
    }

    public AssemblyDefinition Resolve(AssemblyNameReference assemblyNameReference)
    {
        return Resolve(assemblyNameReference, new ReaderParameters());
    }

    public AssemblyDefinition Resolve(AssemblyNameReference assemblyNameReference, ReaderParameters parameters)
    {
        if (parameters == null)
        {
            parameters = new ReaderParameters();
        }

        if (referenceDictionary.TryGetValue(assemblyNameReference.Name, out var fileFromDerivedReferences))
        {
            return GetAssembly(fileFromDerivedReferences, parameters);
        }

        return TryToReadFromDirs(assemblyNameReference, parameters);
    }

    AssemblyDefinition TryToReadFromDirs(AssemblyNameReference assemblyNameReference, ReaderParameters parameters)
    {
        var filesWithMatchingName = SearchDirForMatchingName(assemblyNameReference).ToList();
        foreach (var filePath in filesWithMatchingName)
        {
            var assemblyName = AssemblyName.GetAssemblyName(filePath);
            if (assemblyNameReference.Version == null || assemblyName.Version == assemblyNameReference.Version)
            {
                return GetAssembly(filePath, parameters);
            }
        }
        foreach (var filePath in filesWithMatchingName.OrderByDescending(s => AssemblyName.GetAssemblyName(s).Version))
        {
            return GetAssembly(filePath, parameters);
        }

        var joinedReferences = string.Join(Environment.NewLine, splitReferences.OrderBy(x => x));
        logger.LogDebug(string.Format("Can not find '{0}'.{1}Tried:{1}{2}", assemblyNameReference.FullName, Environment.NewLine, joinedReferences));
        return null;
    }

    IEnumerable<string> SearchDirForMatchingName(AssemblyNameReference assemblyNameReference)
    {
        var fileName = assemblyNameReference.Name + ".dll";
        return referenceDictionary.Values
            .Select(x => Path.Combine(Path.GetDirectoryName(x), fileName))
            .Where(File.Exists);
    }

    public void Dispose()
    {
        foreach (var value in assemblyDefinitionCache.Values)
        {
            value?.Dispose();
        }
    }
}