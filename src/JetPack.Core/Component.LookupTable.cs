using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

namespace JetPack
{
	public class ComponentLookupTable : MonoBehaviour
	{
		public HashSet<object> ComponentList = new HashSet<object>();

		public void Init(GameObject _gameObject)
		{
			if (_gameObject == null) return;

			ComponentList = new HashSet<object>(_gameObject.GetComponentsInChildren<Component>(true)?.Where(x => x != null && x.GetType() != typeof(ComponentLookupTable)).Select(x => x as object));
		}

		public List<T> Components<T>() where T : class
		{
			List<T> _result = new List<T>();
			foreach (T x in ComponentList.Where(x => x is T))
				_result.Add(x);
			return _result;
		}

		public List<object> Components(Type _type)
		{
			List<object> _result = new List<object>();
			foreach (object x in ComponentList.Where(x => x.GetType() == _type))
				_result.Add(x);
			return _result;
		}
	}
}
