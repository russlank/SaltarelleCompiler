﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Saltarelle.Compiler.JSModel;
using Saltarelle.Compiler.JSModel.Expressions;
using Saltarelle.Compiler.JSModel.GotoRewrite;
using Saltarelle.Compiler.JSModel.Statements;

namespace Saltarelle.Compiler.Tests.GotoTests {
	[TestFixture]
	public class GotoRewriterTests {
		private void AssertCorrect(string orig, string expected) {
			int tempCount = 0;
			var stmt = JsBlockStatement.MakeBlock(JavaScriptParser.Parser.ParseStatement(orig));
			var result = GotoRewriter.Rewrite(stmt, e => e.NodeType != ExpressionNodeType.Identifier, () => "$tmp" + (++tempCount).ToString(CultureInfo.InvariantCulture));
			var actual = OutputFormatter.Format(result);
			Assert.That(actual.Replace("\r\n", "\n"), Is.EqualTo(expected.Replace("\r\n", "\n")), "Expected:\n" + expected + "\n\nActual:\n" + actual);
		}

		[Test]
		public void CanRewriteGotoToStateMachine() {
			AssertCorrect(
@"{
	a;
	b;
lbl1:
	if (c)
		goto lbl2;
	d;
lbl2:
	e;
	f;
}", 
@"{
	var $tmp1 = 0;
	$loop1:
	for (;;) {
		switch ($tmp1) {
			case 0: {
				a;
				b;
				$tmp1 = 1;
				continue $loop1;
			}
			case 1: {
				if (c) {
					$tmp1 = 2;
					continue $loop1;
				}
				d;
				$tmp1 = 2;
				continue $loop1;
			}
			case 2: {
				e;
				f;
				break $loop1;
			}
		}
	}
}
");
		}

		[Test]
		public void TryCatchFinallyAreRewrittenByThemselves() {
			AssertCorrect(
@"{
	try {
		a;
		try {
			b;
			try {
				c;
				lbl1:
				d;
			}
			catch (e) {
				f;
			}
			g;
			lbl2:
			h;
		}
		catch (i) {
			j;
		}
		k;
		lbl3:
		l;
	}
	catch (m) {
		n;
		lbl4:
		o;
	}
	finally {
		p;
		lbl5:
		q;
	}
}",
@"{
	try {
		var $tmp3 = 0;
		$loop3:
		for (;;) {
			switch ($tmp3) {
				case 0: {
					a;
					try {
						var $tmp2 = 0;
						$loop2:
						for (;;) {
							switch ($tmp2) {
								case 0: {
									b;
									try {
										var $tmp1 = 0;
										$loop1:
										for (;;) {
											switch ($tmp1) {
												case 0: {
													c;
													$tmp1 = 1;
													continue $loop1;
												}
												case 1: {
													d;
													break $loop1;
												}
											}
										}
									}
									catch (e) {
										f;
									}
									g;
									$tmp2 = 1;
									continue $loop2;
								}
								case 1: {
									h;
									break $loop2;
								}
							}
						}
					}
					catch (i) {
						j;
					}
					k;
					$tmp3 = 1;
					continue $loop3;
				}
				case 1: {
					l;
					break $loop3;
				}
			}
		}
	}
	catch (m) {
		var $tmp4 = 0;
		$loop4:
		for (;;) {
			switch ($tmp4) {
				case 0: {
					n;
					$tmp4 = 1;
					continue $loop4;
				}
				case 1: {
					o;
					break $loop4;
				}
			}
		}
	}
	finally {
		var $tmp5 = 0;
		$loop5:
		for (;;) {
			switch ($tmp5) {
				case 0: {
					p;
					$tmp5 = 1;
					continue $loop5;
				}
				case 1: {
					q;
					break $loop5;
				}
			}
		}
	}
}
");
		}

		[Test]
		public void NestedFunctionsAreRewrittenByThemselves() {
			AssertCorrect(
@"{
	a;
	lbl1:
	b;
	var c = function() {
		d;
		lbl2:
		e;
		var f = function() {
			g;
			lbl3:
			h;
		};
		i;
	};
	j;
}",
@"{
	var $tmp3 = 0;
	$loop3:
	for (;;) {
		switch ($tmp3) {
			case 0: {
				a;
				$tmp3 = 1;
				continue $loop3;
			}
			case 1: {
				b;
				var c = function() {
					var $tmp2 = 0;
					$loop2:
					for (;;) {
						switch ($tmp2) {
							case 0: {
								d;
								$tmp2 = 1;
								continue $loop2;
							}
							case 1: {
								e;
								var f = function() {
									var $tmp1 = 0;
									$loop1:
									for (;;) {
										switch ($tmp1) {
											case 0: {
												g;
												$tmp1 = 1;
												continue $loop1;
											}
											case 1: {
												h;
												break $loop1;
											}
										}
									}
								};
								i;
								break $loop2;
							}
						}
					}
				};
				j;
				break $loop3;
			}
		}
	}
}
");
		}
	}
}
