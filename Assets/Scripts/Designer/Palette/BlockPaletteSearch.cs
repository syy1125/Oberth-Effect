using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Syy1125.OberthEffect.Spec.Block;
using Syy1125.OberthEffect.Spec.Database;

namespace Syy1125.OberthEffect.Designer.Palette
{
public class BlockPaletteSearch
{
	private enum TermType
	{
		Name,
		Description,
		Mod,
		Category,
		BlockId,
	}

	private static HashSet<char> TermControls = new() { '#', '@', '%', '&' };

	private interface ISearchTerm
	{
		bool Match(BlockSpec block);
	}

	private class LeafSearchTerm : ISearchTerm
	{
		private TermType _type;
		private string _term;

		public static LeafSearchTerm Create(TermType type, string term)
		{
			if (string.IsNullOrEmpty(term)) return null;
			return new LeafSearchTerm(type, term.ToLower());
		}

		private LeafSearchTerm(TermType type, string term)
		{
			_type = type;
			_term = term;
		}

		public bool Match(BlockSpec block)
		{
			return _type switch
			{
				TermType.Name => block.Info.FullName.ToLower().Contains(_term),
				TermType.Description => block.Info.Description.ToLower().Contains(_term),
				TermType.Mod => BlockDatabase.Instance.GetBlockSpecInstance(block.BlockId)
					.OverrideOrder.Any(mod => mod.ToLower().Contains(_term)),
				TermType.Category => BlockDatabase.Instance.GetCategory(block.CategoryId)
					.DisplayName.ToLower()
					.Contains(_term),
				TermType.BlockId => block.BlockId.ToLower().Contains(_term),
				_ => throw new ArgumentOutOfRangeException()
			};
		}

		public override string ToString()
		{
			return _type switch
			{
				TermType.Name => $"\"{_term}\"",
				TermType.Description => $"#\"{_term}\"",
				TermType.Mod => $"@\"{_term}\"",
				TermType.Category => $"%\"{_term}\"",
				TermType.BlockId => $"&\"{_term}\"",
				_ => throw new ArgumentOutOfRangeException()
			};
		}
	}

	private class ConjunctionSearchTerm : ISearchTerm
	{
		private List<ISearchTerm> _terms;

		public static ConjunctionSearchTerm Combine(ISearchTerm left, ISearchTerm right)
		{
			if (left is ConjunctionSearchTerm leftConjunction)
			{
				if (right is ConjunctionSearchTerm rightConjunction)
				{
					leftConjunction._terms.AddRange(rightConjunction._terms);
				}
				else
				{
					leftConjunction._terms.Add(right);
				}

				return leftConjunction;
			}
			else
			{
				if (right is ConjunctionSearchTerm rightConjunction)
				{
					rightConjunction._terms.Insert(0, left);
					return rightConjunction;
				}
				else
				{
					return new ConjunctionSearchTerm(left, right);
				}
			}
		}

		private ConjunctionSearchTerm(params ISearchTerm[] terms)
		{
			_terms = new List<ISearchTerm>(terms);
		}

		public bool Match(BlockSpec block)
		{
			return _terms.All(term => term.Match(block));
		}

		public override string ToString()
		{
			return "(" + string.Join("&", _terms.Select(term => term.ToString())) + ")";
		}
	}

	private class DisjunctionSearchTerm : ISearchTerm
	{
		private List<ISearchTerm> _terms;

		public static DisjunctionSearchTerm Combine(ISearchTerm left, ISearchTerm right)
		{
			if (left is DisjunctionSearchTerm leftDisjunction)
			{
				if (right is DisjunctionSearchTerm rightDisjunction)
				{
					leftDisjunction._terms.AddRange(rightDisjunction._terms);
				}
				else
				{
					leftDisjunction._terms.Add(right);
				}

				return leftDisjunction;
			}
			else
			{
				if (right is DisjunctionSearchTerm rightDisjunction)
				{
					rightDisjunction._terms.Insert(0, left);
					return rightDisjunction;
				}
				else
				{
					return new DisjunctionSearchTerm(left, right);
				}
			}
		}

		private DisjunctionSearchTerm(params ISearchTerm[] terms)
		{
			_terms = new List<ISearchTerm>(terms);
		}

		public bool Match(BlockSpec block)
		{
			return _terms.Any(term => term.Match(block));
		}

		public override string ToString()
		{
			return "(" + string.Join("|", _terms.Select(term => term.ToString())) + ")";
		}
	}

	private class NegationSearchTerm : ISearchTerm
	{
		private readonly ISearchTerm _term;

		public static ISearchTerm Create(ISearchTerm term)
		{
			if (term == null) return null;

			if (term is NegationSearchTerm negationTerm)
			{
				return negationTerm._term;
			}

			return new NegationSearchTerm(term);
		}

		private NegationSearchTerm(ISearchTerm term)
		{
			_term = term;
		}

		public bool Match(BlockSpec block)
		{
			return !_term.Match(block);
		}

		public override string ToString()
		{
			return $"!{_term}";
		}
	}

	private ISearchTerm _rootTerm;

	public BlockPaletteSearch(string search)
	{
		int pos = 0;
		_rootTerm = Parse(search, ref pos);
	}

	private enum OperatorPriority
	{
		None,
		Or,
		And
	}

	private static HashSet<char> Operators = new() { '&', '|', '(', ')' };

	// Parses the search string. If it encounters parentheses (meaning grouped search term), it calls itself recursively to parse the inner search term.
	private static ISearchTerm Parse(string search, ref int pos)
	{
		var terms = new Stack<(OperatorPriority Op, ISearchTerm Term)>();
		OperatorPriority currentOp = OperatorPriority.None;

		while (pos < search.Length)
		{
			while (pos < search.Length && char.IsWhiteSpace(search[pos])) pos++;

			if (pos >= search.Length) break;

			switch (search[pos])
			{
				case '|':
				{
					currentOp = OperatorPriority.Or;
					pos++;
					continue;
				}
				case '&':
				{
					currentOp = OperatorPriority.And;
					pos++;
					continue;
				}
				case '(':
				{
					pos++;
					ISearchTerm term = Parse(search, ref pos);
					PushTerm(terms, ref currentOp, term);
					continue;
				}
				case ')':
				{
					pos++;
					return Combine(terms);
				}
				default:
				{
					ISearchTerm term = ParseSingle(search, ref pos);
					PushTerm(terms, ref currentOp, term);
					continue;
				}
			}
		}

		return Combine(terms);
	}

	// Parse a single search term. Handles leaf nodes and negation.
	// Can return null if the filter is invalid.
	private static ISearchTerm ParseSingle(string search, ref int pos)
	{
		if (pos >= search.Length) return null;

		TermType type = TermType.Name;

		// Check for prefix
		switch (search[pos])
		{
			case '~':
				pos++;
				return NegationSearchTerm.Create(ParseSingle(search, ref pos));
			case '#':
				type = TermType.Description;
				pos++;
				break;
			case '@':
				type = TermType.Mod;
				pos++;
				break;
			case '%':
				type = TermType.Category;
				pos++;
				break;
			case '&':
				type = TermType.BlockId;
				pos++;
				break;
		}

		StringBuilder term = new StringBuilder();
		bool escape = false;

		for (; pos < search.Length; pos++)
		{
			char c = search[pos];

			if (escape)
			{
				escape = false;
				term.Append(c);
				continue;
			}

			switch (c)
			{
				case '\\':
					escape = true;
					continue;
				default:
					if (char.IsWhiteSpace(c) || TermControls.Contains(c) || Operators.Contains(c))
					{
						return LeafSearchTerm.Create(type, term.ToString());
					}
					else
					{
						term.Append(c);
						continue;
					}
			}
		}

		return LeafSearchTerm.Create(type, term.ToString());
	}

	// Push the current operator and term onto the stack, resolving earlier operations if they have higher priority.
	// Clears the current operator afterward.
	// Can handle null terms.
	private static void PushTerm(
		Stack<(OperatorPriority Op, ISearchTerm Term)> terms, ref OperatorPriority op, ISearchTerm term
	)
	{
		if (term == null)
		{
			op = OperatorPriority.None;
			return;
		}

		while (terms.Count > 1 && terms.Peek().Op > op)
		{
			CombineLast(terms);
		}

		terms.Push((op, term));
		op = OperatorPriority.None;
	}

	private static void CombineLast(Stack<(OperatorPriority Op, ISearchTerm Term)> terms)
	{
		(OperatorPriority rightOp, ISearchTerm right) = terms.Pop();
		(OperatorPriority leftOp, ISearchTerm left) = terms.Pop();
		switch (rightOp)
		{
			case OperatorPriority.None:
			case OperatorPriority.And:
				terms.Push((leftOp, ConjunctionSearchTerm.Combine(left, right)));
				break;
			case OperatorPriority.Or:
				terms.Push((leftOp, DisjunctionSearchTerm.Combine(left, right)));
				break;
			default:
				throw new ArgumentOutOfRangeException();
		}
	}

	private static ISearchTerm Combine(Stack<(OperatorPriority Op, ISearchTerm Term)> terms)
	{
		if (terms.Count == 0) return null;

		while (terms.Count > 1)
		{
			CombineLast(terms);
		}

		return terms.Pop().Term;
	}

	public bool Match(BlockSpec block)
	{
		return _rootTerm?.Match(block) ?? true;
	}

	public override string ToString()
	{
		return _rootTerm?.ToString() ?? "null";
	}
}
}