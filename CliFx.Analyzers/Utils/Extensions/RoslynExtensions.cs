﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CliFx.Analyzers.Utils.Extensions;

internal static class RoslynExtensions
{
    public static bool DisplayNameMatches(this ISymbol symbol, string name) =>
        string.Equals(
            // Fully qualified name, without `global::`
            symbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
            name,
            StringComparison.Ordinal
        );

    public static IEnumerable<INamedTypeSymbol> GetBaseTypes(this ITypeSymbol type)
    {
        var current = type.BaseType;

        while (current is not null)
        {
            yield return current;
            current = current.BaseType;
        }
    }

    public static bool IsAssignableFrom(this ITypeSymbol target, ITypeSymbol source) =>
        SymbolEqualityComparer.Default.Equals(target, source) ||
        source.GetBaseTypes().Contains(target, SymbolEqualityComparer.Default) ||
        source.AllInterfaces.Contains(target, SymbolEqualityComparer.Default);

    public static void HandleClassDeclaration(
        this AnalysisContext analysisContext,
        Action<SyntaxNodeAnalysisContext, ClassDeclarationSyntax, ITypeSymbol> analyze)
    {
        analysisContext.RegisterSyntaxNodeAction(ctx =>
        {
            if (ctx.Node is not ClassDeclarationSyntax classDeclaration)
                return;

            var type = ctx.SemanticModel.GetDeclaredSymbol(classDeclaration);
            if (type is null)
                return;

            analyze(ctx, classDeclaration, type);
        }, SyntaxKind.ClassDeclaration);
    }

    public static void HandlePropertyDeclaration(
        this AnalysisContext analysisContext,
        Action<SyntaxNodeAnalysisContext, PropertyDeclarationSyntax, IPropertySymbol> analyze)
    {
        analysisContext.RegisterSyntaxNodeAction(ctx =>
        {
            if (ctx.Node is not PropertyDeclarationSyntax propertyDeclaration)
                return;

            var property = ctx.SemanticModel.GetDeclaredSymbol(propertyDeclaration);
            if (property is null)
                return;

            analyze(ctx, propertyDeclaration, property);
        }, SyntaxKind.PropertyDeclaration);
    }
}