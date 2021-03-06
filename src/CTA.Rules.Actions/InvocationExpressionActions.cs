﻿using CTA.Rules.Config;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using System;

namespace CTA.Rules.Actions
{
    /// <summary>
    /// List of actions that can run on Invocation Expressions
    /// </summary>
    public class InvocationExpressionActions
    {
        public Func<SyntaxGenerator, InvocationExpressionSyntax, InvocationExpressionSyntax> GetReplaceMethodAction(string newMethod)
        {
            //TODO what's the outcome if newMethod doesn't have a valid signature.. are there any options we could provide to parseexpression ?
            Func<SyntaxGenerator, InvocationExpressionSyntax, InvocationExpressionSyntax> ReplaceMethod = (SyntaxGenerator syntaxGenerator, InvocationExpressionSyntax node) =>
            {
                node = node.WithExpression(SyntaxFactory.ParseExpression(newMethod)).NormalizeWhitespace();
                return node;
            };
            return ReplaceMethod;
        }

        public Func<SyntaxGenerator, InvocationExpressionSyntax, InvocationExpressionSyntax> GetAppendMethodAction(string appendMethod)
        {
            Func<SyntaxGenerator, InvocationExpressionSyntax, InvocationExpressionSyntax> ReplaceMethod = (SyntaxGenerator syntaxGenerator, InvocationExpressionSyntax node) =>
            {
                node = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    node,
                    SyntaxFactory.IdentifierName(SyntaxFactory.ParseName(appendMethod).ToString())),
                SyntaxFactory.ArgumentList()).NormalizeWhitespace();
                return node;
            };
            return ReplaceMethod;
        }

        public Func<SyntaxGenerator, InvocationExpressionSyntax, InvocationExpressionSyntax> GetAddCommentAction(string comment)
        {
            Func<SyntaxGenerator, InvocationExpressionSyntax, InvocationExpressionSyntax> AddComment = (SyntaxGenerator syntaxGenerator, InvocationExpressionSyntax node) =>
            {
                SyntaxTriviaList currentTrivia = node.GetLeadingTrivia();
                currentTrivia = currentTrivia.Add(SyntaxFactory.SyntaxTrivia(SyntaxKind.MultiLineCommentTrivia, string.Format(Constants.CommentFormat, comment)));
                node = node.WithLeadingTrivia(currentTrivia).NormalizeWhitespace();
                return node;
            };
            return AddComment;
        }
    }
}
