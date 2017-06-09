using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using SpyStudio.Dialogs.Compare;

namespace SpyStudio.Extensions
{
    public static class LinkedListExtensions
    {
        public static LinkedListNode<T> FindForwardStartingAt<T>(this LinkedList<T> aLinkedList, LinkedListNode<T> aNode, Func<T, bool> aCondition)
        {
            var currentNode = aNode ?? aLinkedList.First;

            while (currentNode != null)
            {
                if (aCondition(currentNode.Value))
                    return currentNode;

                currentNode = currentNode.Next;
            }

            return null;
        }

        public static LinkedListNode<T> FindBackwardsStartingAt<T>(this LinkedList<T> aLinkedList, LinkedListNode<T> aNode, Func<T, bool> aCondition)
        {
            var currentNode = aNode ?? aLinkedList.Last;

            while (currentNode != null)
            {
                if (aCondition(currentNode.Value))
                    return currentNode;

                currentNode = currentNode.Previous;
            }

            return null;
        }

        public static LinkedListNode<T> FindFirst<T>(this LinkedList<T> aLinkedList, Func<T, bool> aCondition)
        {
            return aLinkedList.FindForwardStartingAt(aLinkedList.First, aCondition);
        }

        public static LinkedListNode<T> FindLast<T>(this LinkedList<T> aLinkedList, Func<T, bool> aCondition)
        {
            return aLinkedList.FindBackwardsStartingAt(aLinkedList.Last, aCondition);
        }

        public static LinkedListNode<T> AddBetween<T>(this LinkedList<T> aLinkedList, T aValue, LinkedListNode<T> previousNode, LinkedListNode<T> nextNode)
        {
            if (previousNode != null)
                return aLinkedList.AddAfter(previousNode, aValue);

            if (nextNode != null)
                return aLinkedList.AddBefore(nextNode, aValue);

            return aLinkedList.AddFirst(aValue);
        }

        public static void InsertOrdered(this LinkedList<EventInfo> aList, EventInfo anEventInfo)
        {
            if (aList.Last == null || aList.Last.Value.Preceeds(anEventInfo))
            {
                aList.AddLast(anEventInfo);
                return;
            }

            var lastPreceedingNode = aList.FindLast(e => e.Preceeds(anEventInfo));

            if (lastPreceedingNode == null)
            {
                aList.AddFirst(anEventInfo);
                return;
            }

            aList.AddAfter(lastPreceedingNode, anEventInfo);
        }
    }
}
