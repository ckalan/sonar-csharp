﻿/*
 * SonarAnalyzer for .NET
 * Copyright (C) 2015-2017 SonarSource SA
 * mailto: contact AT sonarsource DOT com
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 3 of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software Foundation,
 * Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
 */

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using SonarAnalyzer.Common;
using SonarAnalyzer.Helpers;
using System.Collections.Immutable;

namespace SonarAnalyzer.Rules.CSharp
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    [Rule(DiagnosticId)]
    public class EqualityOnModulus : SonarDiagnosticAnalyzer
    {
        internal const string DiagnosticId = "S2197";
        private const string MessageFormat = "The result of this modulus operation may not be {0}.";

        private static readonly DiagnosticDescriptor rule =
            DiagnosticDescriptorBuilder.GetDescriptor(DiagnosticId, MessageFormat, RspecStrings.ResourceManager);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(rule);

        protected sealed override void Initialize(SonarAnalysisContext context)
        {
            context.RegisterSyntaxNodeActionInNonGenerated(
                c =>
                {
                    var equalsExpression = (BinaryExpressionSyntax)c.Node;

                    int constantValue;
                    if (CheckExpression(equalsExpression.Left, equalsExpression.Right, c.SemanticModel, out constantValue) ||
                        CheckExpression(equalsExpression.Right, equalsExpression.Left, c.SemanticModel, out constantValue))
                    {
                        c.ReportDiagnostic(Diagnostic.Create(rule, equalsExpression.GetLocation(),
                            constantValue < 0 ? "negative" : "positive"));
                    }
                },
                SyntaxKind.EqualsExpression,
                SyntaxKind.NotEqualsExpression);
        }

        private static bool CheckExpression(ExpressionSyntax constant, ExpressionSyntax modulus, SemanticModel semanticModel,
            out int constantValue)
        {
            return ExpressionNumericConverter.TryGetConstantIntValue(constant, out constantValue) &&
                   constantValue != 0 &&
                   ExpressionIsModulus(modulus) &&
                   !ExpressionIsNonNegative(modulus, semanticModel);
        }

        private static bool ExpressionIsModulus(ExpressionSyntax expression)
        {
            var binary = expression.RemoveParentheses() as BinaryExpressionSyntax;
            return binary != null && binary.IsKind(SyntaxKind.ModuloExpression);
        }

        private static bool ExpressionIsNonNegative(ExpressionSyntax expression, SemanticModel semantic)
        {
            var type = semantic.GetTypeInfo(expression).Type;
            return type.IsAny(KnownType.UnsignedIntegers);
        }
    }
}
