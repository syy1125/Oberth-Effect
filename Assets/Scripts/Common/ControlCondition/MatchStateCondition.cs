using System;
using System.Collections.Generic;
using System.Linq;

namespace Syy1125.OberthEffect.Common.ControlCondition
{
public class MatchStateCondition : IControlCondition
{
	private string _controlGroupId;
	private string[] _matchStateIds;

	public MatchStateCondition(string controlGroupId, string[] matchStateIds)
	{
		_controlGroupId = controlGroupId;
		_matchStateIds = matchStateIds;
	}

	public bool IsTrue(Func<string, string> getState)
	{
		return _matchStateIds.Contains(getState(_controlGroupId));
	}

	public IEnumerable<string> GetControlGroups()
	{
		yield return _controlGroupId;
	}
}
}