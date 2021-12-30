﻿using System;
using System.Collections.Generic;

namespace DATReader.DatStore {
	// if DatFileType is UnSet then we store the children files in List<DatBase> unsorted. (The order they are added from the DAT.)
	// problem with this is that searching for a child by name would have to be a linear search. (Which gets very slow.)
	// So for this reason UnSet file lists also use the _childrenNameIndex to store a sorted index.
	// So the List<DatBase> _children is still the original DAT order, but the List<int> _childrenNameIndex gives a quick way to search for the items.

	public class DatDir : DatBase {
		public DatGame DGame;
		private readonly List<DatBase> _children = new List<DatBase>();
		private readonly List<int> _childrenNameIndex = new List<int>();

		public DatDir(DatFileType type) : base(type) {
		}

		public int ChildCount => _children.Count;

		public DatBase Child(int index) => _children[index];

		public DatBase ChildSorted(int index) => DatFileType == DatFileType.UnSet ?
				_children[_childrenNameIndex[index]] :
				_children[index];

		public DatBase[] ToArray() => _children.ToArray();

		public int ChildAdd(DatBase datItem) {
			int index;
			if (DatFileType == DatFileType.UnSet) {
				ChildNameBinarySearch(datItem, true, false, out var indexSearch);
				_children.Add(datItem);
				index = _children.Count - 1;
				_childrenNameIndex.Insert(indexSearch, index);
				return index;
			}

			ChildNameBinarySearch(datItem, false, false, out index);
			_children.Insert(index, datItem);
			return index;
		}

		public void ChildRemove(DatBase datItem) {
			var count = _children.Count;
			for (var i = 0; i < count; i++) {
				if (_children[i] != datItem) {
					continue;
				}

				_children.RemoveAt(i);

				if (DatFileType != DatFileType.UnSet) {
					return;
				}

				for (var j = 0; j < count; j++) {
					if (_childrenNameIndex[j] == i) {
						_childrenNameIndex.RemoveAt(j);
						count--;
						j--;
					} else if (_childrenNameIndex[j] > i) {
						_childrenNameIndex[j]--;
					}
				}
				return;
			}
		}

		public void ChildrenClear() {
			_children.Clear();
			_childrenNameIndex.Clear();
		}

		public int ChildNameSearch(DatBase lName, out int index) {
			if (DatFileType != DatFileType.UnSet) {
				return ChildNameBinarySearch(lName, false, true, out index);
			}

			var retval = ChildNameBinarySearch(lName, true, true, out index);
			index = retval == 0 ? _childrenNameIndex[index] : -1;
			return retval;
		}

		private int ChildNameBinarySearch(DatBase lName, bool useIndex, bool findFirst, out int index) {
			var intBottom = 0;
			var intTop = _children.Count;
			var intMid = 0;
			var intRes = -1;

			//Binary chop to find the closest match
			while ((intBottom < intTop) && (intRes != 0)) {
				intMid = (intBottom + intTop) / 2;

				intRes = CompareName(lName, _children[useIndex ? _childrenNameIndex[intMid] : intMid]);
				if (intRes < 0) {
					intTop = intMid;
				} else if (intRes > 0) {
					intBottom = intMid + 1;
				}
			}
			index = intMid;

			if (intRes == 0) {
				if (findFirst) {
					// if match found check up the list to the first match
					var intRes1 = 0;
					while ((index > 0) && (intRes1 == 0)) {
						intRes1 = CompareName(lName, _children[useIndex ? _childrenNameIndex[index - 1] : index - 1]);
						if (intRes1 == 0) {
							index--;
						}
					}
				} else {
					// if match was found check down the list to point past the last match
					// this is used so that if a duplicate is found, we return the next record as the index
					// where the duplicate item should be added.
					var intRes1 = 0;
					while ((index < _children.Count - 1) && (intRes1 == 0)) {
						intRes1 = CompareName(lName, _children[useIndex ? _childrenNameIndex[index + 1] : index + 1]);
						if (intRes1 == 0) {
							index++;
						}
					}
					// if intRes1 is still 0, then we are at the very end of the list.
					// increase index to point past the end so that the item is added on the end of the list.
					if (intRes1 == 0) {
						index++;
					}
				}
			}
			// if the search is greater than the closest match move one down the list
			else if (intRes > 0) {
				index++;
			}

			return intRes;

		}

		private int CompareName(DatBase lName, DatBase dBase) {

			switch (DatFileType) {
				case DatFileType.UnSet: {
						var res = Math.Sign(string.Compare(lName.Name, dBase.Name, StringComparison.InvariantCulture));
						if (res != 0) {
							return res;
						}

						break;

					}

				case DatFileType.Dir:
				case DatFileType.DirRVZip:
				case DatFileType.DirTorrentZip: {
						var res = Math.Sign(DatSort.TrrntZipStringCompareCase(lName.Name, dBase.Name));
						if (res != 0) {
							return res;
						}

						break;
					}
				case DatFileType.Dir7Zip: {
						var res = Math.Sign(DatSort.Trrnt7ZipStringCompare(lName.Name, dBase.Name));
						if (res != 0) {
							return res;
						}

						break;
					}
				default:

					throw new InvalidOperationException("Invalid directory compare type " + DatFileType);

			}
			return lName.DatFileType.CompareTo(dBase.DatFileType);
		}
	}
}
