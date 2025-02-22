﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CliFx.Attributes;
using CliFx.Infrastructure;
using CliFx.Schema;
using CliFx.Utils;
using CliFx.Utils.Extensions;

namespace CliFx;

/// <summary>
/// Builder for <see cref="CliApplication" />.
/// </summary>
public partial class CliApplicationBuilder
{
    private readonly HashSet<Type> _commandTypes = new();

    private bool _isDebugModeAllowed = true;
    private bool _isPreviewModeAllowed = true;
    private string? _title;
    private string? _executableName;
    private string? _version;
    private string? _description;
    private IConsole? _console;
    private ITypeActivator? _typeActivator;

    /// <summary>
    /// Adds a command to the application.
    /// </summary>
    public CliApplicationBuilder AddCommand(Type commandType)
    {
        _commandTypes.Add(commandType);

        return this;
    }

    /// <summary>
    /// Adds a command to the application.
    /// </summary>
    public CliApplicationBuilder AddCommand<TCommand>() where TCommand : ICommand =>
        AddCommand(typeof(TCommand));

    /// <summary>
    /// Adds multiple commands to the application.
    /// </summary>
    public CliApplicationBuilder AddCommands(IEnumerable<Type> commandTypes)
    {
        foreach (var commandType in commandTypes)
            AddCommand(commandType);

        return this;
    }

    /// <summary>
    /// Adds commands from the specified assembly to the application.
    /// </summary>
    /// <remarks>
    /// This method looks for public non-abstract classes that implement <see cref="ICommand" />
    /// and are annotated by <see cref="CommandAttribute" />.
    /// </remarks>
    public CliApplicationBuilder AddCommandsFrom(Assembly commandAssembly)
    {
        foreach (var commandType in commandAssembly.ExportedTypes.Where(CommandSchema.IsCommandType))
            AddCommand(commandType);

        return this;
    }

    /// <summary>
    /// Adds commands from the specified assemblies to the application.
    /// </summary>
    /// <remarks>
    /// This method looks for public non-abstract classes that implement <see cref="ICommand" />
    /// and are annotated by <see cref="CommandAttribute" />.
    /// </remarks>
    public CliApplicationBuilder AddCommandsFrom(IEnumerable<Assembly> commandAssemblies)
    {
        foreach (var commandAssembly in commandAssemblies)
            AddCommandsFrom(commandAssembly);

        return this;
    }

    /// <summary>
    /// Adds commands from the calling assembly to the application.
    /// </summary>
    /// <remarks>
    /// This method looks for public non-abstract classes that implement <see cref="ICommand" />
    /// and are annotated by <see cref="CommandAttribute" />.
    /// </remarks>
    public CliApplicationBuilder AddCommandsFromThisAssembly() => AddCommandsFrom(Assembly.GetCallingAssembly());

    /// <summary>
    /// Specifies whether debug mode (enabled with the [debug] directive) is allowed in the application.
    /// </summary>
    public CliApplicationBuilder AllowDebugMode(bool isAllowed = true)
    {
        _isDebugModeAllowed = isAllowed;
        return this;
    }

    /// <summary>
    /// Specifies whether preview mode (enabled with the [preview] directive) is allowed in the application.
    /// </summary>
    public CliApplicationBuilder AllowPreviewMode(bool isAllowed = true)
    {
        _isPreviewModeAllowed = isAllowed;
        return this;
    }

    /// <summary>
    /// Sets application title, which is shown in the help text.
    /// </summary>
    /// <remarks>
    /// By default, application title is inferred from the assembly name.
    /// </remarks>
    public CliApplicationBuilder SetTitle(string title)
    {
        _title = title;
        return this;
    }

    /// <summary>
    /// Sets application executable name, which is shown in the help text.
    /// </summary>
    /// <remarks>
    /// By default, application executable name is inferred from the assembly file name.
    /// The file name is also prefixed with `dotnet` if it's a DLL file.
    /// </remarks>
    public CliApplicationBuilder SetExecutableName(string executableName)
    {
        _executableName = executableName;
        return this;
    }

    /// <summary>
    /// Sets application version, which is shown in the help text or
    /// when the user specifies the version option.
    /// </summary>
    /// <remarks>
    /// By default, application version is inferred from the assembly version.
    /// </remarks>
    public CliApplicationBuilder SetVersion(string version)
    {
        _version = version;
        return this;
    }

    /// <summary>
    /// Sets application description, which is shown in the help text.
    /// </summary>
    public CliApplicationBuilder SetDescription(string? description)
    {
        _description = description;
        return this;
    }

    /// <summary>
    /// Configures the application to use the specified implementation of <see cref="IConsole" />.
    /// </summary>
    public CliApplicationBuilder UseConsole(IConsole console)
    {
        _console = console;
        return this;
    }

    /// <summary>
    /// Configures the application to use the specified implementation of <see cref="ITypeActivator" />.
    /// </summary>
    public CliApplicationBuilder UseTypeActivator(ITypeActivator typeActivator)
    {
        _typeActivator = typeActivator;
        return this;
    }

    /// <summary>
    /// Configures the application to use the specified function for activating types.
    /// </summary>
    public CliApplicationBuilder UseTypeActivator(Func<Type, object> typeActivator) =>
        UseTypeActivator(new DelegateTypeActivator(typeActivator));

    /// <summary>
    /// Configures the application to use the specified service provider for activating types.
    /// </summary>
    public CliApplicationBuilder UseTypeActivator(IServiceProvider serviceProvider) =>
        UseTypeActivator(serviceProvider.GetService);

    /// <summary>
    /// Creates a configured instance of <see cref="CliApplication" />.
    /// </summary>
    public CliApplication Build()
    {
        var metadata = new ApplicationMetadata(
            _title ?? GetDefaultTitle(),
            _executableName ?? GetDefaultExecutableName(),
            _version ?? GetDefaultVersionText(),
            _description
        );

        var configuration = new ApplicationConfiguration(
            _commandTypes.ToArray(),
            _isDebugModeAllowed,
            _isPreviewModeAllowed
        );

        return new CliApplication(
            metadata,
            configuration,
            _console ?? new SystemConsole(),
            _typeActivator ?? new DefaultTypeActivator()
        );
    }
}

public partial class CliApplicationBuilder
{
    private static string GetDefaultTitle()
    {
        var entryAssemblyName = EnvironmentEx.EntryAssembly?.GetName().Name;
        if (string.IsNullOrWhiteSpace(entryAssemblyName))
            return "App";

        return entryAssemblyName;
    }

    private static string GetDefaultExecutableName()
    {
        var entryAssemblyLocation = EnvironmentEx.EntryAssembly?.Location;
        if (string.IsNullOrWhiteSpace(entryAssemblyLocation))
            return "app";

        // If the application was launched via matching EXE apphost, use that as the executable name
        var isLaunchedViaAppHost = string.Equals(
            EnvironmentEx.ProcessPath,
            Path.ChangeExtension(entryAssemblyLocation, ".exe"),
            StringComparison.OrdinalIgnoreCase
        );

        if (isLaunchedViaAppHost)
            return Path.GetFileNameWithoutExtension(entryAssemblyLocation);

        // Otherwise, use the entry assembly as the executable name.
        // Prefix it with `dotnet` if it's a DLL file.
        var isDll = string.Equals(
            Path.GetExtension(entryAssemblyLocation),
            ".dll",
            StringComparison.OrdinalIgnoreCase
        );

        return isDll
            ? "dotnet " + Path.GetFileName(entryAssemblyLocation)
            : Path.GetFileNameWithoutExtension(entryAssemblyLocation);
    }

    private static string GetDefaultVersionText()
    {
        var entryAssemblyVersion = EnvironmentEx.EntryAssembly?.GetName().Version;
        if (entryAssemblyVersion is null)
            return "v1.0";

        return "v" + entryAssemblyVersion.ToSemanticString();
    }
}