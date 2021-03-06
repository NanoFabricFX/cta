﻿using CTA.Rules.Actions;
using CTA.Rules.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using NUnit.Framework;

namespace CTA.Rules.Test.Actions
{
    public class ClassActionsTests
    {
        private SyntaxGenerator _syntaxGenerator;
        private ClassActions _classActions;
        private ClassDeclarationSyntax _node;

        [SetUp]
        public void SetUp()
        {
            var workspace = new AdhocWorkspace();
            var language = LanguageNames.CSharp;
            _syntaxGenerator = SyntaxGenerator.GetGenerator(workspace, language);
            _classActions = new ClassActions();
            _node = _syntaxGenerator.ClassDeclaration("MyClass") as ClassDeclarationSyntax;
        }

        [Test]
        public void GetAddCommentAction_Adds_Leading_Comment_To_Class_Declaration()
        {
            const string commentToAdd = "This is a comment";
            var addCommentFunc = _classActions.GetAddCommentAction(commentToAdd);
            var newNode = addCommentFunc(_syntaxGenerator, _node);

            var expectedResult = @$"/* Added by CTA: {commentToAdd} */
class MyClass
{{
}}";
            Assert.AreEqual(expectedResult, newNode.ToFullString());
        }

        [Test]
        public void GetChangeNameAction_Changes_Class_Name_To_Specified_Value()
        {
            const string newClassName = "NewClassName";
            var changeNameFunc = _classActions.GetChangeNameAction(newClassName);
            var newNode = changeNameFunc(_syntaxGenerator, _node);

            var expectedResult = @$"class {newClassName}
{{
}}";
            Assert.AreEqual(expectedResult, newNode.ToFullString());
        }

        [Test]
        public void GetRenameClassAction_Changes_Class_Name_To_Specified_Value()
        {
            const string newClassName = "NewClassName";
            var changeNameFunc = _classActions.GetRenameClassAction(newClassName);
            var newNode = changeNameFunc(_syntaxGenerator, _node);

            var expectedResult = @$"class {newClassName}
{{
}}";
            Assert.AreEqual(expectedResult, newNode.ToFullString());
        }

        [Test]
        public void GetRemoveAttributeAction_Removes_Specified_Attribute()
        {
            const string attributeToRemove = "Serializable";
            var nodeWithAttributes = (ClassDeclarationSyntax)_syntaxGenerator.AddAttributes(_node,
                _syntaxGenerator.Attribute("Serializable"),
                _syntaxGenerator.Attribute("SecurityCritical"));

            var removeAttributeFunc = _classActions.GetRemoveAttributeAction(attributeToRemove);
            var newNode = removeAttributeFunc(_syntaxGenerator, nodeWithAttributes);

            var expectedResult = @$"[SecurityCritical]
class MyClass
{{
}}";
            Assert.AreEqual(expectedResult, newNode.ToFullString());
        }

        [Test]
        public void GetRemoveMethodAction_Removes_Specified_Method()
        {
            const string methodName= "MyMethod";
            var methodNode = SyntaxFactory.MethodDeclaration(SyntaxFactory.ParseTypeName("void"), methodName);
            var nodeWithMethod = _node.AddMembers(methodNode);

            var removeMethodFunc = _classActions.GetRemoveMethodAction(methodName);
            var newNode = removeMethodFunc(_syntaxGenerator, nodeWithMethod);

            var expectedResult = @$"class MyClass
{{
}}";
            Assert.AreEqual(expectedResult, newNode.ToFullString());
        }

        [Test]
        public void RemoveLastBaseClass()
        {
            var baseClassname = "ControllerBase";
            var addBaseClass = _classActions.GetAddBaseClassAction(baseClassname);
            var removeBaseClassMethod = _classActions.GetRemoveBaseClassAction(baseClassname);

            var nodeWithClass = addBaseClass(_syntaxGenerator, _node);
            nodeWithClass = removeBaseClassMethod(_syntaxGenerator, nodeWithClass);

            //Make sure the inheritance symbol is removed when last base class is removed:
            StringAssert.DoesNotContain(":", nodeWithClass.ToFullString());
        }

        [Test]
        public void ClassDeclarationEquals()
        {
            var classAction = new ClassDeclarationAction() { Key = "Test", Value = "Test2", ClassDeclarationActionFunc = _classActions.GetAddAttributeAction("Test") };
            var cloned = classAction.Clone();
            Assert.True(classAction.Equals(cloned));

            cloned.Value = "DifferentValue";
            Assert.False(classAction.Equals(cloned));
        }
    }
}