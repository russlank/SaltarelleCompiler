﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using ICSharpCode.NRefactory.TypeSystem;
using NUnit.Framework;
using Saltarelle.Compiler.JSModel.Expressions;

namespace Saltarelle.Compiler.Tests.MethodCompilationTests
{
    public class MethodCompilerTestBase : CompilerTestBase {
        protected IMethod Method { get; private set; }
        protected MethodCompiler MethodCompiler { get; private set; }
        protected JsFunctionDefinitionExpression CompiledMethod { get; private set; }

        protected void CompileMethod(string source, INamingConventionResolver namingConvention = null, IRuntimeLibrary runtimeLibrary = null, IErrorReporter errorReporter = null, string methodName = "M") {
            Compile(new[] { "using System; class C { " + source + "}" }, namingConvention, runtimeLibrary, errorReporter, (m, res, mc) => {
				if (m.Name == methodName) {
					Method = m;
					MethodCompiler = mc;
					CompiledMethod = res;
				}
            });

			Assert.That(Method, Is.Not.Null, "Method " + methodName + " was not compiled");
        }

		protected void AssertCorrect(string csharp, string expected, INamingConventionResolver namingConvention = null) {
			CompileMethod(csharp, namingConvention: namingConvention ?? new MockNamingConventionResolver {
				GetPropertyImplementation = p => new Regex("^F[0-9]*$").IsMatch(p.Name) ? PropertyImplOptions.Field("$" + p.Name)
				                                                                        : PropertyImplOptions.GetAndSetMethods(MethodImplOptions.NormalMethod("get_$" + p.Name),
				                                                                                                               MethodImplOptions.NormalMethod("set_$" + p.Name)),
				GetMethodImplementation = m => MethodImplOptions.NormalMethod("$" + m.Name),
				GetEventImplementation  = e => EventImplOptions.AddAndRemoveMethods(MethodImplOptions.NormalMethod("add_$" + e.Name), MethodImplOptions.NormalMethod("remove_$" + e.Name)),
			});
			string actual = OutputFormatter.Format(CompiledMethod.Body, true);

			int begin = actual.IndexOf("// BEGIN");
			if (begin > -1) {
				while (begin < (actual.Length - 1) && actual[begin - 1] != '\n')
					begin++;
				actual = actual.Substring(begin);
			}

			int end = actual.IndexOf("// END");
			if (end >= 0) {
				while (end >= 0 && actual[end] != '\n')
					end--;
				actual = actual.Substring(0, end + 1);
			}
			Assert.That(actual.Replace("\r\n", "\n"), Is.EqualTo(expected.Replace("\r\n", "\n")));
		}

		protected void DoForAllIntegerTypes(Action<string> a) {
			DoForAllSignedIntegerTypes(a);
			DoForAllUnsignedIntegerTypes(a);
		}

		protected void DoForAllFloatingPointTypes(Action<string> a) {
			foreach (var type in new[] { "float", "double", "decimal" })
				a(type);
		}

		protected void DoForAllSignedIntegerTypes(Action<string> a) {
			foreach (var type in new[] { "sbyte", "short", "int", "long" })
				a(type);
		}

		protected void DoForAllUnsignedIntegerTypes(Action<string> a) {
			foreach (var type in new[] { "byte", "ushort", "uint", "ulong"  })
				a(type);
		}

		[SetUp]
		public void Setup() {
			Method = null;
			MethodCompiler = null;
			CompiledMethod = null;
		}
    }
}