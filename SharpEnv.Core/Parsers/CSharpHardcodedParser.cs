using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpEnv.Core.Models;

namespace SharpEnv.Core.Parsers;

public sealed class CSharpHardcodedParser : IConfigParser
{
    public ConfigSourceType SourceType => ConfigSourceType.CSharp;

    public bool CanParse(string filePath) =>
        string.Equals(Path.GetExtension(filePath), ".cs", StringComparison.OrdinalIgnoreCase);

    public IEnumerable<ConfigEntry> Parse(string content, string relativeFilePath)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return [];
        }

        var entries = new List<ConfigEntry>();
        var syntaxTree = CSharpSyntaxTree.ParseText(content);
        var root = syntaxTree.GetCompilationUnitRoot();

        foreach (var field in root.DescendantNodes().OfType<FieldDeclarationSyntax>())
        {
            if (!HasTargetModifiers(field.Modifiers) || !IsSupportedPrimitiveType(field.Declaration.Type))
            {
                continue;
            }

            var enclosingType = field.FirstAncestorOrSelf<TypeDeclarationSyntax>();
            if (enclosingType is null)
            {
                continue;
            }

            var className = enclosingType.Identifier.Text;

            foreach (var variable in field.Declaration.Variables)
            {
                if (variable.Initializer?.Value is null)
                {
                    continue;
                }

                if (!TryExtractLiteralValue(variable.Initializer.Value, out var value))
                {
                    continue;
                }

                entries.Add(new ConfigEntry
                {
                    Key = $"{className}__{variable.Identifier.Text}",
                    Value = value,
                    SourceType = ConfigSourceType.CSharp,
                    RelativeFilePath = relativeFilePath,
                    IsSelected = true
                });
            }
        }

        return entries;
    }

    private static bool HasTargetModifiers(SyntaxTokenList modifiers)
    {
        var isConst = false;
        var isStatic = false;
        var isReadOnly = false;

        foreach (var modifier in modifiers)
        {
            if (modifier.IsKind(SyntaxKind.ConstKeyword))
            {
                isConst = true;
            }
            else if (modifier.IsKind(SyntaxKind.StaticKeyword))
            {
                isStatic = true;
            }
            else if (modifier.IsKind(SyntaxKind.ReadOnlyKeyword))
            {
                isReadOnly = true;
            }
        }

        return isConst || (isStatic && isReadOnly);
    }

    private static bool IsSupportedPrimitiveType(TypeSyntax typeSyntax)
    {
        if (typeSyntax is PredefinedTypeSyntax predefined)
        {
            return predefined.Keyword.Kind() is
                SyntaxKind.StringKeyword or
                SyntaxKind.IntKeyword or
                SyntaxKind.LongKeyword or
                SyntaxKind.BoolKeyword or
                SyntaxKind.DoubleKeyword or
                SyntaxKind.DecimalKeyword;
        }

        return false;
    }

    private static bool TryExtractLiteralValue(ExpressionSyntax expression, out string value)
    {
        if (expression is LiteralExpressionSyntax literal)
        {
            if (literal.IsKind(SyntaxKind.NullLiteralExpression))
            {
                value = string.Empty;
                return false;
            }

            value = literal.Token.Value?.ToString() ?? literal.Token.ValueText;
            return true;
        }

        if (expression is PrefixUnaryExpressionSyntax unary &&
            unary.Operand is LiteralExpressionSyntax operandLiteral &&
            !operandLiteral.IsKind(SyntaxKind.NullLiteralExpression) &&
            (unary.IsKind(SyntaxKind.UnaryMinusExpression) || unary.IsKind(SyntaxKind.UnaryPlusExpression)))
        {
            var raw = operandLiteral.Token.Value?.ToString() ?? operandLiteral.Token.ValueText;
            value = unary.IsKind(SyntaxKind.UnaryMinusExpression) ? $"-{raw}" : raw;
            return true;
        }

        value = string.Empty;
        return false;
    }
}
