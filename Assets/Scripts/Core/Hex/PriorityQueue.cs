using System;
using System.Collections.Generic;

namespace SparkAge.Core.Hex
{
    /// <summary>最小堆优先队列：Dequeue 永远返回优先级最小的元素。O(log n)</summary>
    public class PriorityQueue<TElement, TPriority> where TPriority : IComparable<TPriority>
    {
        readonly List<(TElement Element, TPriority Priority)> _items = new();

        public int Count => _items.Count;

        public void Enqueue(TElement element, TPriority priority)
        {
            _items.Add((element, priority));
            int i = _items.Count - 1;
            while (i > 0)                                  // 尾部上浮
            {
                int parent = (i - 1) / 2;
                if (_items[parent].Priority.CompareTo(_items[i].Priority) <= 0) break;
                (_items[parent], _items[i]) = (_items[i], _items[parent]);
                i = parent;
            }
        }

        public TElement Dequeue()
        {
            if (_items.Count == 0) throw new InvalidOperationException("队列为空");
            var root = _items[0];
            _items[0] = _items[^1];                        // 末位移到根
            _items.RemoveAt(_items.Count - 1);

            int i = 0;
            while (true)                                   // 根下沉，选较小的子节点
            {
                int left = i * 2 + 1, right = i * 2 + 2, smallest = i;
                if (left < _items.Count && _items[left].Priority.CompareTo(_items[smallest].Priority) < 0)
                    smallest = left;
                if (right < _items.Count && _items[right].Priority.CompareTo(_items[smallest].Priority) < 0)
                    smallest = right;
                if (smallest == i) break;
                (_items[i], _items[smallest]) = (_items[smallest], _items[i]);
                i = smallest;
            }
            return root.Element;
        }
    }
}