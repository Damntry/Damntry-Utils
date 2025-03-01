using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Damntry.Utils.Collections {


	//Taken from: https://stackoverflow.com/a/10442244/739345
	public class TreeNode<T> {

		private readonly T _value;
		private readonly List<TreeNode<T>> _children = new List<TreeNode<T>>();

		public TreeNode(T value) {
			_value = value;
		}

		private TreeNode(T value, TreeNode<T> parent) {
			_value = value;
			this.Parent = parent;
		}

		public TreeNode<T> this[int i] {
			get { return _children[i]; }
		}

		public TreeNode<T> Parent { get; private set; }

		public T Value { get { return _value; } }

		public ReadOnlyCollection<TreeNode<T>> Children {
			get { return _children.AsReadOnly(); }
		}

		public TreeNode<T> AddChild(T value) {
			var node = new TreeNode<T>(value, this);
			_children.Add(node);
			return node;
		}

		public TreeNode<T>[] AddChildren(params T[] values) {
			return values.Select(AddChild).ToArray();
		}

		public bool RemoveChild(TreeNode<T> node) {
			return _children.Remove(node);
		}

		public void TransverseAndDo(Action<T> action) {
			action(Value);
			foreach (var child in _children) {
				child.TransverseAndDo(action);
			}
		}

		public IEnumerable<T> Flatten() {
			return new[] { Value }.Concat(_children.SelectMany(x => x.Flatten()));
		}
	}
}
