// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Razor.Language.Components;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.IntegrationTests
{
    public class ComponentGenericTypeIntegrationTest : RazorIntegrationTestBase
    {
        private readonly CSharpSyntaxTree GenericContextComponent = Parse(@"
using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;
namespace Test
{
    public class GenericContext<TItem> : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            var items = (IReadOnlyList<TItem>)Items ?? Array.Empty<TItem>();
            for (var i = 0; i < items.Count; i++)
            {
                if (ChildContent == null)
                {
                    builder.AddContent(i, Items[i]);
                }
                else
                {
                    builder.AddContent(i, ChildContent, new Context() { Index = i, Item = items[i], });
                }
            }
        }

        [Parameter]
        List<TItem> Items { get; set; }

        [Parameter]
        RenderFragment<Context> ChildContent { get; set; }

        public class Context
        {
            public int Index { get; set; }
            public TItem Item { get; set; }
        }
    }
}
");

        private readonly CSharpSyntaxTree MultipleGenericParameterComponent = Parse(@"
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;
namespace Test
{
    public class MultipleGenericParameter<TItem1, TItem2, TItem3> : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.AddContent(0, Item1);
            builder.AddContent(1, Item2);
            builder.AddContent(2, Item3);
        }

        [Parameter]
        TItem1 Item1 { get; set; }

        [Parameter]
        TItem2 Item2 { get; set; }

        [Parameter]
        TItem3 Item3 { get; set; }
    }
}
");

        internal override string FileKind => FileKinds.Component;

        internal override bool UseTwoPhaseCompilation => true;

        [Fact]
        public void GenericComponent_WithoutAnyTypeParameters_TriggersDiagnostic()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(GenericContextComponent);

            // Act
            var generated = CompileToCSharp(@"
<GenericContext />");

            // Assert
            var diagnostic = Assert.Single(generated.Diagnostics);
            Assert.Same(ComponentDiagnosticFactory.GenericComponentTypeInferenceUnderspecified.Id, diagnostic.Id);
            Assert.Equal(
                "The type of component 'GenericContext' cannot be inferred based on the values provided. Consider " +
                "specifying the type arguments directly using the following attributes: 'TItem'.",
                diagnostic.GetMessage());
        }

        [Fact]
        public void GenericComponent_WithMissingTypeParameters_TriggersDiagnostic()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(MultipleGenericParameterComponent);

            // Act
            var generated = CompileToCSharp(@"
<MultipleGenericParameter TItem1=int />");

            // Assert
            var diagnostic = Assert.Single(generated.Diagnostics);
            Assert.Same(ComponentDiagnosticFactory.GenericComponentMissingTypeArgument.Id, diagnostic.Id);
            Assert.Equal(
                "The component 'MultipleGenericParameter' is missing required type arguments. " +
                "Specify the missing types using the attributes: 'TItem2', 'TItem3'.",
                diagnostic.GetMessage());
        }
    }
}
